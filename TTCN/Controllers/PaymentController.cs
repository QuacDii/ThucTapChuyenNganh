using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Text.Json;
using TTCN.Models;
using TTCN.Models.Momo;
using TTCN.Services;
using TTCN.Services.Momo;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TTCN.Controllers
{
    public class PaymentController : Controller
    {
        private readonly QLDVContext _db;
        private readonly IMomoService _momoService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IEmailService _emailService;

        public PaymentController(
            QLDVContext db,
            IMomoService momoService,
            ILogger<PaymentController> logger,
            IEmailService emailService)
        {
            _db = db;
            _momoService = momoService;
            _logger = logger;
            _emailService = emailService;
        }

        // =============== BƯỚC 1: NHẬN DỮ LIỆU TỪ TRANG CHỌN GHẾ ===============
        [HttpPost]
        public IActionResult Index(PaymentSelectionRequest request)
        {
            if (request == null) return RedirectToAction("Index", "Home");

            // Parse ghế
            List<CheckoutSeatModel> seats = new();
            if (!string.IsNullOrWhiteSpace(request.SelectedSeatsJson))
            {
                try
                {
                    seats = JsonSerializer.Deserialize<List<CheckoutSeatModel>>(
                        request.SelectedSeatsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                catch { }
            }
            // ========== PARSE COMBO ==========
            List<CheckoutComboModel> combos = new();

            if (!string.IsNullOrWhiteSpace(request.SelectedCombosJson))
            {
                try
                {
                    combos = JsonSerializer.Deserialize<List<CheckoutComboModel>>(
                        request.SelectedCombosJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                }
                catch
                {
                    // có thể log nếu cần
                }
            }

            // Lấy mã user đang đăng nhập
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            // Tạo ViewModel
            var checkout = new PaymentCheckoutViewModel
            {
                MovieName = request.MovieName,
                CinemaName = request.CinemaName,
                RoomName = request.RoomName,
                ShowDate = request.ShowDate,
                ShowTime = request.ShowTime,
                Seats = seats,
                ComboText = request.ComboText,
                ComboPrice = request.ComboPrice,
                TotalPrice = request.TotalPrice,
                MaSuat = request.MaSuat,
                MaUsers = userId
            };

            // ============= TẠO ĐƠN CHỜ THANH TOÁN ===============

            var don = new DonDatVe
            {
                NgayDat = DateTime.Now,
                TongTien = checkout.TotalPrice,
                TrangThai = "Chờ thanh toán",
                MaUsers = userId
            };

            _db.DonDatVes.Add(don);
            _db.SaveChanges();   // MaDon tự tăng tại đây
            Console.WriteLine(don.MaDon);

            // Lưu MaDon vào session
            HttpContext.Session.SetInt32("MaDon", don.MaDon);

            // Lưu luôn dữ liệu ghế + combo vào session
            HttpContext.Session.SetString("CheckoutData",
                JsonConvert.SerializeObject(checkout));

            // ===== Tạo OrderInfo gửi sang MoMo =====
            var order = new OrderInfoModel
            {
                FullName = "User",
                OrderId = "M" + DateTime.Now.ToString("yyyyMMddHHmmssfff"), // FIXED
                Amount = (long)checkout.TotalPrice,
                OrderInfo = $"Thanh toán vé xem phim: {checkout.MovieName}",
                ExtraData = don.MaDon.ToString()
            };
            ViewBag.OI = order;

            // ========== LƯU GHẾ VÀO CHITIETDONDAT ==========
            //foreach (var seat in seats)
            //{
            //    var getSuat = _db.SuatChieus.FirstOrDefault(s => s.MaSuat == checkout.MaSuat);
            //    var getGhe = _db.GheNgois.FirstOrDefault(g => g.TenGhe == seat.Name && g.MaPhong == getSuat.MaPhong);

            //    if (getGhe != null)
            //    {
            //        var ct = new ChiTietDonDat
            //        {
            //            MaDon = don.MaDon,
            //            MaGhe = getGhe.MaGhe,
            //            MaSuat = checkout.MaSuat,
            //            TrangThai = true   
            //        };
            //        _db.ChiTietDonDat.Add(ct);
            //    }
            //}
            foreach (var seat in seats)
            {
                foreach (var maGhe in seat.MaGhe)
                {
                    _db.ChiTietDonDat.Add(new ChiTietDonDat
                    {
                        MaDon = don.MaDon,
                        MaGhe = maGhe,
                        MaSuat = checkout.MaSuat,
                        TrangThai = true
                    });
                }
            }

            // ========== LƯU COMBO ĐỒ ĂN ==========
            foreach (var c in combos)
            {
                if (c.SoLuong <= 0) continue;

                var combo = _db.DoAns.FirstOrDefault(x => x.MaCombo == c.MaCombo);
                if (combo != null)
                {
                    _db.DonDatVeDoAns.Add(new DonDatVeDoAn
                    {
                        MaDon = don.MaDon,
                        MaCombo = combo.MaCombo,
                        SoLuong = c.SoLuong
                    });
                }
            }

            ViewBag.MaDon = don.MaDon;
            _db.SaveChanges();

            return View(checkout);
        }

        // =============== BƯỚC 2: TẠO URL THANH TOÁN MOMO ===============
        [HttpPost]
        [Route("CreatePaymentUrl")]
        public async Task<IActionResult> CreatePaymentMomo(OrderInfoModel model)
        {
            if (!int.TryParse(model.ExtraData, out int maDon))
                return Content("Dữ liệu không hợp lệ");

            var don = _db.DonDatVes.FirstOrDefault(x => x.MaDon == maDon);
            if (don == null)
                return RedirectToAction("Index", "Home");

            if (DateTime.Now > don.NgayDat.AddMinutes(5))
            {
                return RedirectToAction("Index", "Home");
            }

            var response = await _momoService.CreatePaymentMomo(model);
            return Redirect(response.PayUrl);
        }


        // =============== BƯỚC 3: MOMO CALLBACK ===============
        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            _logger.LogInformation("MOMO CALLBACK HIT");

            var response = _momoService.PaymentExecuteAsync(Request.Query);
            if (response == null)
                return Content("Không nhận được phản hồi từ MoMo");

            if (!int.TryParse(response.ExtraData, out int maDon))
                return Content("ExtraData không hợp lệ");

            var don = _db.DonDatVes.FirstOrDefault(x => x.MaDon == maDon);
            if (don == null)
                return RedirectToAction("Index", "Home");

            var chiTietList = _db.ChiTietDonDat
                    .Where(x => x.MaDon == don.MaDon)
                    .ToList();

            var chiTietDoAn = _db.DonDatVeDoAns.Where(x => x.MaDon == don.MaDon).ToList();

            if (don.TrangThai == "Hết hạn" && response.ResultCode == 0)
            {
                _logger.LogWarning($"[MOCK REFUND] Đơn {don.MaDon} quá hạn – hoàn tiền");

                don.TrangThai = "Đã hoàn tiền (Mock)";
                var ctGhe = _db.ChiTietDonDat.Where(x => x.MaDon == maDon).ToList();
                var ctCombo = _db.DonDatVeDoAns.Where(x => x.MaDon == maDon).ToList();

                _db.ChiTietDonDat.RemoveRange(ctGhe);
                _db.DonDatVeDoAns.RemoveRange(ctCombo);
                _db.SaveChanges();

                return RedirectToAction("Index", "Home");
            }

            // Thanh toán thành công
            if (response.ResultCode == 0)
            {
                don.TrangThai = "Đã thanh toán";
                _db.SaveChanges();

                // ================= AUTO LOGIN USER =================
                var user = _db.Users.FirstOrDefault(u => u.MaUsers == don.MaUsers);
                if (user != null)
                {
                    HttpContext.Session.SetInt32("UserId", user.MaUsers);
                    HttpContext.Session.SetString("UserEmail", user.Email);
                    HttpContext.Session.SetString("UserHoTen", user.HoTen);
                    HttpContext.Session.SetString("UserVaiTro", user.VaiTro);
                    HttpContext.Session.SetString("SessionStartTime", DateTime.UtcNow.ToString("o"));
                }
                // ===================================================

                // Gửi email xác nhận đặt vé
                _ = SendBookingConfirmationEmailAsync(maDon);

                return RedirectToAction(
                    "chiTietDon",
                    "DonDatVe",
                    new { id = don.MaDon }
                );

            }
            else // Thanh toán thất bại
            {
                _db.DonDatVes.Remove(don);
                
                foreach (var ct in chiTietList)
                {
                    _db.ChiTietDonDat.Remove(ct);
                }
                
                foreach(var da in chiTietDoAn)
                {
                    _db.DonDatVeDoAns.Remove(da);
                }

                _db.SaveChanges();

                return RedirectToAction("Index", "Home");
            }
        }

        private async Task SendBookingConfirmationEmailAsync(int maDon)
        {
            try
            {
                // Lấy thông tin đơn đặt vé với các navigation properties
                var don = _db.DonDatVes
                    .Include(d => d.MaUsersNavigation)
                    .Include(d => d.ChiTietDonDat)
                        .ThenInclude(ct => ct.MaGheNavigation)
                    .Include(d => d.ChiTietDonDat)
                        .ThenInclude(ct => ct.MaSuatNavigation)
                            .ThenInclude(s => s.MaPhimNavigation)
                    .Include(d => d.ChiTietDonDat)
                        .ThenInclude(ct => ct.MaSuatNavigation)
                            .ThenInclude(s => s.MaPhongNavigation)
                                .ThenInclude(p => p.MaCumRapNavigation)
                    .Include(d => d.DonDatVeDoAns)
                        .ThenInclude(da => da.MaComboNavigation)
                    .FirstOrDefault(x => x.MaDon == maDon);

                if (don == null || don.MaUsersNavigation == null)
                {
                    _logger.LogWarning($"Không tìm thấy đơn {maDon} hoặc thông tin user để gửi email");
                    return;
                }

                var user = don.MaUsersNavigation;
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogWarning($"User {user.MaUsers} không có email để gửi");
                    return;
                }

                // Lấy thông tin suất chiếu đầu tiên (tất cả ghế trong cùng một suất)
                var firstChiTiet = don.ChiTietDonDat.FirstOrDefault();
                if (firstChiTiet == null || firstChiTiet.MaSuatNavigation == null)
                {
                    _logger.LogWarning($"Đơn {maDon} không có chi tiết suất chiếu");
                    return;
                }

                var suatChieu = firstChiTiet.MaSuatNavigation;
                var phim = suatChieu.MaPhimNavigation;
                var phong = suatChieu.MaPhongNavigation;
                var cumRap = phong?.MaCumRapNavigation;
                var qrContent = $"MA-DON:{don.MaDon}";
                var qrBytes = GenerateQrBytes($"MA-DON:{don.MaDon}");

                var html = BuildEmailContent(
                    don, user, phim, cumRap, phong, suatChieu, ""
                );

                await _emailService.SendEmailWithInlineImageAsync(
                    user.Email,
                    $"Xác nhận đặt vé - #{don.MaDon}",
                    html,
                    qrBytes
                );
                _logger.LogInformation($"Đã gửi email xác nhận đặt vé đến {user.Email} cho đơn {maDon}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi gửi email xác nhận đặt vé cho đơn {maDon}");
            }
        }

        private string BuildEmailContent(
            DonDatVe don,
            User user,
            Phim phim,
            CumRap? cumRap,
            PhongChieu? phong,
            SuatChieu suatChieu,
            string qrBase64)
        {
            var sb = new StringBuilder();

            // Danh sách ghế đã chọn
            var gheList = don.ChiTietDonDat
                .Where(ct => ct.MaGheNavigation != null)
                .Select(ct => ct.MaGheNavigation.TenGhe)
                .OrderBy(g => g)
                .ToList();

            // Danh sách combo đã chọn
            var comboList = don.DonDatVeDoAns
                .Where(da => da.MaComboNavigation != null)
                .Select(da => new
                {
                    Ten = da.MaComboNavigation.MoTa,
                    SoLuong = da.SoLuong,
                    Gia = da.MaComboNavigation.Gia,
                    ThanhTien = da.MaComboNavigation.Gia * da.SoLuong
                })
                .ToList();

            var ngayGioChieu = suatChieu.GioBatDau?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
            var tenPhim = phim?.TenPhim ?? "N/A";
            var tenCumRap = cumRap?.TenCumRap ?? "N/A";
            var tenPhong = phong?.TenPhong ?? "N/A";
            var diaChi = cumRap?.DiaChi ?? "N/A";

            sb.AppendLine($@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            line-height: 1.6;
                            color: #333;
                            max-width: 600px;
                            margin: 0 auto;
                            padding: 20px;
                            background-color: #f4f4f4;
                        }}
                        .container {{
                            background-color: #ffffff;
                            border-radius: 10px;
                            padding: 30px;
                            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
                        }}
                        .header {{
                            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                            color: white;
                            padding: 20px;
                            border-radius: 10px 10px 0 0;
                            margin: -30px -30px 30px -30px;
                            text-align: center;
                        }}
                        .header h1 {{
                            margin: 0;
                            font-size: 24px;
                        }}
                        .success-icon {{
                            font-size: 48px;
                            color: #28a745;
                            text-align: center;
                            margin: 20px 0;
                        }}
                        .info-section {{
                            margin: 20px 0;
                            padding: 15px;
                            background-color: #f8f9fa;
                            border-radius: 5px;
                            border-left: 4px solid #667eea;
                        }}
                        .info-row {{
                            display: flex;
                            justify-content: space-between;
                            padding: 8px 0;
                            border-bottom: 1px solid #e0e0e0;
                        }}
                        .info-row:last-child {{
                            border-bottom: none;
                        }}
                        .info-label {{
                            font-weight: 600;
                            color: #555;
                        }}
                        .info-value {{
                            color: #333;
                        }}
                        .seats-section {{
                            margin: 20px 0;
                            padding: 15px;
                            background-color: #e8f5e9;
                            border-radius: 5px;
                        }}
                        .seats-list {{
                            display: flex;
                            flex-wrap: wrap;
                            gap: 8px;
                            margin-top: 10px;
                        }}
                        .seat-badge {{
                            background-color: #4caf50;
                            color: white;
                            padding: 5px 12px;
                            border-radius: 20px;
                            font-weight: 600;
                            font-size: 14px;
                        }}
                        .combo-section {{
                            margin: 20px 0;
                            padding: 15px;
                            background-color: #fff3e0;
                            border-radius: 5px;
                        }}
                        .combo-item {{
                            display: flex;
                            justify-content: space-between;
                            padding: 8px 0;
                            border-bottom: 1px solid #ffe0b2;
                        }}
                        .combo-item:last-child {{
                            border-bottom: none;
                        }}
                        .total-section {{
                            margin: 20px 0;
                            padding: 20px;
                            background-color: #667eea;
                            color: white;
                            border-radius: 5px;
                            text-align: center;
                        }}
                        .total-section .total-label {{
                            font-size: 18px;
                            margin-bottom: 10px;
                        }}
                        .total-section .total-amount {{
                            font-size: 32px;
                            font-weight: bold;
                        }}
                        .footer {{
                            margin-top: 30px;
                            padding-top: 20px;
                            border-top: 2px solid #e0e0e0;
                            text-align: center;
                            color: #666;
                            font-size: 14px;
                        }}
                        .note {{
                            background-color: #fff9c4;
                            padding: 15px;
                            border-radius: 5px;
                            margin: 20px 0;
                            border-left: 4px solid #fbc02d;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🎬 Xác nhận đặt vé thành công</h1>
                        </div>
        
                        <div class='success-icon'>✓</div>
        
                        <p>Xin chào <strong>{user.HoTen}</strong>,</p>
                        <p>Cảm ơn bạn đã đặt vé tại hệ thống của chúng tôi. Đơn đặt vé của bạn đã được xác nhận và thanh toán thành công.</p>
        
                        <div class='info-section'>
                            <h3 style='margin-top: 0; color: #667eea;'>📋 Thông tin đơn hàng</h3>
                            <div class='info-row'>
                                <span class='info-label'>Mã đơn:</span>
                                <span class='info-value'><strong>#{don.MaDon}</strong></span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Ngày đặt:</span>
                                <span class='info-value'>{don.NgayDat:dd/MM/yyyy HH:mm}</span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Trạng thái:</span>
                                <span class='info-value' style='color: #28a745; font-weight: 600;'>{don.TrangThai}</span>
                            </div>
                        </div>

                        <div class='info-section'>
                            <h3 style='margin-top: 0; color: #667eea;'>🎥 Thông tin suất chiếu</h3>
                            <div class='info-row'>
                                <span class='info-label'>Phim:</span>
                                <span class='info-value'><strong>{tenPhim}</strong></span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Cụm rạp:</span>
                                <span class='info-value'>{tenCumRap}</span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Địa chỉ:</span>
                                <span class='info-value'>{diaChi}</span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Phòng chiếu:</span>
                                <span class='info-value'>{tenPhong}</span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Ngày & Giờ chiếu:</span>
                                <span class='info-value'><strong>{ngayGioChieu}</strong></span>
                            </div>
                        </div>");

                            // Danh sách ghế
                            if (gheList.Any())
                            {
                                sb.AppendLine($@"
                        <div class='seats-section'>
                            <h3 style='margin-top: 0; color: #4caf50;'>🪑 Ghế đã chọn ({gheList.Count} ghế)</h3>
                            <div class='seats-list'>");
                                foreach (var ghe in gheList)
                                {
                                    sb.AppendLine($"                <span class='seat-badge'>{ghe}</span>");
                                }
                                sb.AppendLine($@"
                            </div>
                        </div>");
                            }

                            // Danh sách combo
                            if (comboList.Any())
                            {
                                sb.AppendLine($@"
                        <div class='combo-section'>
                            <h3 style='margin-top: 0; color: #f57c00;'>🍿 Combo đã chọn</h3>");
                                foreach (var combo in comboList)
                                {
                                    sb.AppendLine($@"
                            <div class='combo-item'>
                                <span>{combo.Ten} x{combo.SoLuong}</span>
                                <span><strong>{combo.ThanhTien:N0}đ</strong></span>
                            </div>");
                                }
                                sb.AppendLine($@"
                        </div>");
                            }

                            // Tổng tiền
                            sb.AppendLine($@"
                        <div class='total-section'>
                            <div class='total-label'>Tổng cộng</div>
                            <div class='total-amount'>{don.TongTien:N0}đ</div>
                        </div>
                        <div style=""text-align:center; margin-top:30px;"">
                            <p><strong>🎟 Mã QR vào rạp</strong></p>
                            <img src=""{{{{QR_IMAGE}}}}"" width=""180"" />
                            <p style=""font-size:13px;color:#666;"">
                                Quét mã QR này tại quầy để nhận vé
                            </p>
                        </div>

                        <div class='note'>
                            <strong>📌 Lưu ý:</strong>
                            <ul style='margin: 10px 0; padding-left: 20px;'>
                                <li>Vui lòng đến rạp trước 15 phút để làm thủ tục vào rạp</li>
                                <li>Vui lòng giữ gìn vé và không làm mất</li>
                                <li>Nếu có thắc mắc, vui lòng liên hệ hotline của rạp</li>
                            </ul>
                        </div>

                        <div class='footer'>
                            <p>Chúc bạn xem phim vui vẻ! 🎉</p>
                            <p>Trân trọng,<br><strong>Hệ thống đặt vé trực tuyến</strong></p>
                        </div>
                    </div>
                </body>
                </html>");

            return sb.ToString();
        }

        [HttpGet]
        public IActionResult CheckOrderStatus(int maDon)
        {
            var don = _db.DonDatVes.FirstOrDefault(x => x.MaDon == maDon);

            if (don == null)
                return Json(new { status = "NOT_FOUND" });

            return Json(new { status = don.TrangThai });
        }
        private byte[] GenerateQrBytes(string content)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrData);
            using Bitmap bitmap = qrCode.GetGraphic(10);

            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        [HttpGet]
        public IActionResult QrDon(int maDon)
        {
            var don = _db.DonDatVes.FirstOrDefault(x => x.MaDon == maDon);
            if (don == null)
                return NotFound();

            var qrContent = $"MA-DON:{don.MaDon}";

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new QRCode(qrData);
            using Bitmap bitmap = qrCode.GetGraphic(10);

            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);

            return File(ms.ToArray(), "image/png");
        }


    }
}
