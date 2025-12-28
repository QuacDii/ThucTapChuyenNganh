using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Text.Json;
using TTCN.Models;
using TTCN.Services;
using TTCN.Services.Momo;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TTCN.Controllers
{
    public class PaymentController : Controller
    {
        private readonly QLDVContext _db;
        private readonly IMomoService _momoService;
        private readonly ILogger<PaymentController> _logger;
        private readonly IEmailService _emailService;
        private readonly IHubContext<PaymentHub> _hub;
        private const int HOLD_MINUTES = 5;
        private readonly string _qrSecret;

        public PaymentController(
            QLDVContext db,
            IMomoService momoService,
            ILogger<PaymentController> logger,
            IEmailService emailService,
            IHubContext<PaymentHub> hub,
            IConfiguration config)
            {
                _db = db;
                _momoService = momoService;
                _logger = logger;
                _emailService = emailService;
                _hub = hub;
                _qrSecret = config["QrSecretKey"];
        }
        private string GenerateQrToken(int maDon, DateTime ngayDat)
        {
            var raw = $"{maDon}|{ngayDat:yyyyMMddHHmm}|{_qrSecret}";
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes);
        }

        [HttpPost]
        public async Task<IActionResult> Index(PaymentSelectionRequest request)
        {
            // Lấy mã user đang đăng nhập
            int userId = HttpContext.Session.GetInt32("UserId").Value;

            if (request == null) return RedirectToAction("Index", "Home");
            if (HttpContext.Session.GetInt32("MaDon") != null)
            {
                return RedirectToAction("Index", "Home");
            }


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

            var user = _db.Users.FirstOrDefault(u => u.MaUsers == userId);

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
                MaUsers = userId,
                HoTen = user?.HoTen,
                DienThoai = user?.SoDienThoai,
                Email = user?.Email
            };

            HttpContext.Session.SetInt32("MaPhim", request.MaPhim);

            if (combos.Any())
            {
                checkout.Combos = combos.Where(c => c.SoLuong > 0).Select(c =>
                    {
                        var comboDb = _db.DoAns.FirstOrDefault(x => x.MaCombo == c.MaCombo);
                        if (comboDb == null) return null;

                        return new CheckoutComboViewModel
                        {
                            MaCombo = comboDb.MaCombo,
                            TenCombo = comboDb.MoTa,
                            Gia = comboDb.Gia,
                            SoLuong = c.SoLuong
                        };
                    })
                    .Where(x => x != null)
                    .ToList();
            }
            // ================== KHÓA BACKEND – CHỐNG 2 NGƯỜI TRANH GHẾ ==================
            using var tran = await _db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable
            );

            try
            {
                // ===== 1. CHECK GHẾ TRÙNG (LOCK DB) =====
                var selectedSeatIds = seats
                    .SelectMany(s => s.MaGhe)
                    .ToList();

                bool hasConflict = await _db.ChiTietDonDat
                    .AnyAsync(ct =>
                        ct.MaSuat == request.MaSuat &&
                        selectedSeatIds.Contains(ct.MaGhe.Value)
                    );

                if (hasConflict)
                {
                    await tran.RollbackAsync();
                    return BadRequest("Một hoặc nhiều ghế đã được người khác chọn. Vui lòng chọn lại.");
                }

                // ===== 2. TẠO ĐƠN CHỜ THANH TOÁN =====
                var don = new DonDatVe
                {
                    NgayDat = DateTime.Now,
                    TongTien = checkout.TotalPrice,
                    TrangThai = "Chờ thanh toán",
                    MaUsers = userId
                };            

                _db.DonDatVes.Add(don);
                await _db.SaveChangesAsync(); // LẤY MaDon

                // ===== 3. LƯU GHẾ (CHÍNH THỨC GIỮ) =====
                foreach (var seat in seats)
                {
                    foreach (var maGhe in seat.MaGhe)
                    {
                        _db.ChiTietDonDat.Add(new ChiTietDonDat
                        {
                            MaDon = don.MaDon,
                            MaGhe = maGhe,
                            MaSuat = checkout.MaSuat,
                            TrangThai = false
                        });
                    }
                }

                // ===== 4. LƯU COMBO =====
                foreach (var c in combos)
                {
                    if (c.SoLuong <= 0) continue;

                    _db.DonDatVeDoAns.Add(new DonDatVeDoAn
                    {
                        MaDon = don.MaDon,
                        MaCombo = c.MaCombo,
                        SoLuong = c.SoLuong
                    });
                }

                await _db.SaveChangesAsync();

                // ===== 5. COMMIT – CHỈ 1 USER THẮNG =====
                await tran.CommitAsync();

                // ===== 6. LƯU SESSION =====
                HttpContext.Session.SetInt32("MaDon", don.MaDon);

                // ===== 7. LƯU CHECKOUT DATA (BACK TO SEAT) =====
                HttpContext.Session.SetString(
                    "CheckoutData",
                    JsonConvert.SerializeObject(new
                    {
                        MovieName = checkout.MovieName,
                        CinemaName = checkout.CinemaName,
                        RoomName = checkout.RoomName,
                        ShowDate = checkout.ShowDate,
                        ShowTime = checkout.ShowTime,
                        Seats = checkout.Seats,
                        SelectedCombos = combos,
                        MaSuat = checkout.MaSuat
                    })
                );
                var orderInfo = new OrderInfoModel
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Amount = checkout.TotalPrice,
                    OrderInfo = $"Thanh toán vé - Đơn #{don.MaDon}",
                    ExtraData = don.MaDon.ToString()
                };
                ViewBag.OI = orderInfo;

                ViewBag.MaDon = don.MaDon;
            }
            catch (Exception ex)
            {
                await tran.RollbackAsync();
                _logger.LogError(ex, "❌ Lỗi transaction giữ ghế");
                return StatusCode(500, "Có lỗi xảy ra, vui lòng thử lại");
            }

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

            if (DateTime.Now > don.NgayDat.AddMinutes(HOLD_MINUTES))
            {
                don.TrangThai = "Hết hạn";
                _db.SaveChanges();
                return RedirectToAction("Index", "Home");
            }

            var response = await _momoService.CreatePaymentMomo(model);
            return Redirect(response.PayUrl);
        }


        // =============== BƯỚC 3: MOMO CALLBACK ===============
        [HttpGet]
        public async Task<IActionResult> PaymentCallBackAsync()
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
            var expiredTime = don.NgayDat.AddMinutes(HOLD_MINUTES);
            var isExpired = DateTime.Now > expiredTime;

            var chiTietList = _db.ChiTietDonDat
                    .Where(x => x.MaDon == don.MaDon)
                    .ToList();

            var chiTietDoAn = _db.DonDatVeDoAns.Where(x => x.MaDon == don.MaDon).ToList();

            if (response.ResultCode == 0 && isExpired)
            {
                don.TrangThai = "Hết hạn";

                _db.ChiTietDonDat.RemoveRange(chiTietList);
                _db.DonDatVeDoAns.RemoveRange(chiTietDoAn);

                _db.SaveChanges();

                _logger.LogWarning($"Đơn {maDon} thanh toán trễ – bị từ chối");
                return RedirectToAction("Index", "Home");
            }

            // Thanh toán thành công
            if (response.ResultCode == 0)
            {
                don.TrangThai = "Đã thanh toán";
                foreach (var ct in chiTietList)
                {
                    ct.TrangThai = true;
                }
                _db.SaveChanges();

                foreach (var ct in chiTietList)
                {
                    await _hub.Clients
                        .Group($"SUAT_{ct.MaSuat}")
                        .SendAsync("SeatSold", new { maGhe = ct.MaGhe });
                }


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
                foreach (var ct in chiTietList)
                {
                    await _hub.Clients.Group($"SUAT_{ct.MaSuat}").SendAsync("SeatReleased", new { maGhe = ct.MaGhe });
                    _db.ChiTietDonDat.Remove(ct);
                }
                
                foreach(var da in chiTietDoAn)
                {
                    _db.DonDatVeDoAns.Remove(da);
                }
                don.TrangThai = "Thanh toán lỗi";
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
                var token = GenerateQrToken(don.MaDon, don.NgayDat);
                var qrUrl =
                    $"{Request.Scheme}://{Request.Host}/Payment/QrInfo" +
                    $"?maDon={don.MaDon}&token={token}";

                var qrBytes = GenerateQrBytes(qrUrl);



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

            var rawSeats = don.ChiTietDonDat
                .Where(ct => ct.MaGheNavigation != null)
                .Select(ct => new
                {
                    TenGhe = ct.MaGheNavigation.TenGhe.Trim(),
                    LoaiGhe = ct.MaGheNavigation.LoaiGhe.Trim(),
                    Row = System.Text.RegularExpressions.Regex.Match(ct.MaGheNavigation.TenGhe, @"^[A-Z]+").Value,
                    Number = int.Parse(System.Text.RegularExpressions.Regex.Match(ct.MaGheNavigation.TenGhe, @"\d+").Value)
                })
                .OrderBy(x => x.Row).ThenBy(x => x.Number)
                .ToList();

            // 2. Xử lý gộp ghế Sweetbox và tạo danh sách hiển thị cuối cùng
            var finalDisplayList = new List<dynamic>();
            var processedIndices = new HashSet<int>(); // Đánh dấu các ghế đã xử lý

            for (int i = 0; i < rawSeats.Count; i++)
            {
                if (processedIndices.Contains(i)) continue;

                var current = rawSeats[i];
                bool isSweetbox = current.LoaiGhe.ToUpper().Contains("SWEETBOX") || current.LoaiGhe.ToUpper().Contains("DOI");

                if (isSweetbox)
                {
                    if (i + 1 < rawSeats.Count)
                    {
                        var next = rawSeats[i + 1];
                        bool nextIsSweetbox = next.LoaiGhe.ToUpper().Contains("SWEETBOX") || next.LoaiGhe.ToUpper().Contains("DOI");

                        if (nextIsSweetbox && current.Row == next.Row && next.Number == current.Number + 1)
                        {
                            finalDisplayList.Add(new
                            {
                                Label = $"{current.TenGhe}-{next.TenGhe} (Sweetbox)",
                                Color = "#ff6b6b" // Màu hồng/đỏ
                            });

                            processedIndices.Add(i);     
                            processedIndices.Add(i + 1); 
                            continue;
                        }
                    }

                    finalDisplayList.Add(new
                    {
                        Label = $"{current.TenGhe} (Sweetbox)",
                        Color = "#ff6b6b"
                    });
                    processedIndices.Add(i);
                }
                else 
                {
                    string color = "#28a745"; 
                    string labelLoai = "Thường";

                    if (current.LoaiGhe.ToUpper().Contains("VIP"))
                    {
                        color = "#ffc107"; 
                        labelLoai = "VIP";
                    }

                    finalDisplayList.Add(new
                    {
                        Label = $"{current.TenGhe} ({labelLoai})",
                        Color = color
                    });
                    processedIndices.Add(i);
                }
            }

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
                body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f4f4f4; }}
                .container {{ background-color: #ffffff; border-radius: 10px; padding: 30px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
                .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 10px 10px 0 0; margin: -30px -30px 30px -30px; text-align: center; }}
                .header h1 {{ margin: 0; font-size: 24px; }}
                .success-icon {{ font-size: 48px; color: #28a745; text-align: center; margin: 20px 0; }}
                .info-section {{ margin: 20px 0; padding: 15px; background-color: #f8f9fa; border-radius: 5px; border-left: 4px solid #667eea; }}
                .info-row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #e0e0e0; }}
                .info-row:last-child {{ border-bottom: none; }}
                .info-label {{ font-weight: 600; color: #555; }}
                .info-value {{ color: #333; }}
                .seats-section {{ margin: 20px 0; padding: 15px; background-color: #e8f5e9; border-radius: 5px; }}
                .seats-list {{ display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px; }}
                
                .seat-badge {{
                    color: white;
                    padding: 5px 12px;
                    border-radius: 4px;
                    font-weight: 600;
                    font-size: 13px;
                    white-space: nowrap;
                    display: inline-block;
                    margin-bottom: 5px;
                    margin-right: 5px;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.2);
                }}

                .combo-section {{ margin: 20px 0; padding: 15px; background-color: #fff3e0; border-radius: 5px; }}
                .combo-item {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #ffe0b2; }}
                .combo-item:last-child {{ border-bottom: none; }}
                .total-section {{ margin: 20px 0; padding: 20px; background-color: #667eea; color: white; border-radius: 5px; text-align: center; }}
                .total-section .total-label {{ font-size: 18px; margin-bottom: 10px; }}
                .total-section .total-amount {{ font-size: 32px; font-weight: bold; }}
                .footer {{ margin-top: 30px; padding-top: 20px; border-top: 2px solid #e0e0e0; text-align: center; color: #666; font-size: 14px; }}
                .note {{ background-color: #fff9c4; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #fbc02d; }}
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
                        <span class='info-label'>Mã đơn: </span>
                        <span class='info-value'><strong>#{don.MaDon}</strong></span>
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Ngày đặt: </span>
                        <span class='info-value'> {don.NgayDat:dd/MM/yyyy HH:mm}</span>
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Trạng thái: </span>
                        <span class='info-value' style='color: #28a745; font-weight: 600;'> {don.TrangThai}</span>
                    </div>
                </div>

                <div class='info-section'>
                    <h3 style='margin-top: 0; color: #667eea;'>🎥 Thông tin suất chiếu</h3>
                    <div class='info-row'>
                        <span class='info-label'>Phim: </span>
                        <span class='info-value'><strong> {tenPhim}</strong></span>
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Cụm rạp: </span>
                        <span class='info-value'> {tenCumRap}</span>
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Địa chỉ: </span>
                        <span class='info-value'> {diaChi}</span>
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Phòng chiếu: </span>
                        <span class='info-value'> {tenPhong}</span>
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Ngày & Giờ chiếu: </span>
                        <span class='info-value'><strong> {ngayGioChieu}</strong></span>
                    </div>
                </div>");

            if (finalDisplayList.Any())
            {
                sb.AppendLine($@"
                <div class='seats-section'>
                    <h3 style='margin-top: 0; color: #4caf50;'>🪑 Ghế đã chọn ({finalDisplayList.Count} ghế)</h3>
                    <div class='seats-list'>");

                foreach (var item in finalDisplayList)
                {
                    sb.AppendLine($"<span class='seat-badge' style='background-color: {item.Color};'>{item.Label}</span>");
                }

                sb.AppendLine($@"</div>
                </div>");
            }

            if (comboList.Any())
            {
                sb.AppendLine($@"
                <div class='combo-section'>
                    <h3 style='margin-top: 0; color: #f57c00;'>🍿 Combo đã chọn</h3>");
                foreach (var combo in comboList)
                {
                    sb.AppendLine($@"
                    <div class='combo-item'>
                        <span>{combo.Ten} (x{combo.SoLuong})</span>
                        <span><strong> {combo.ThanhTien:N0}đ</strong></span>
                    </div>");
                }
                sb.AppendLine($@"
                </div>");
            }

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
        private byte[] GenerateQrBytes(string url)
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
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
            if (don == null) return NotFound();

            var token = GenerateQrToken(don.MaDon, don.NgayDat);

            var qrUrl =
                $"{Request.Scheme}://{Request.Host}/Payment/QrInfo" +
                $"?maDon={don.MaDon}&token={token}";

            var qrBytes = GenerateQrBytes(qrUrl);
            return File(qrBytes, "image/png");
        }

        [HttpPost]
        public async Task<IActionResult> BackToSeatAjaxAsync()
        {
            int? maPhim = HttpContext.Session.GetInt32("MaPhim");
            int? maDon = HttpContext.Session.GetInt32("MaDon");
            int? maSuat = null;


            var checkoutJson = HttpContext.Session.GetString("CheckoutData");
            if (!string.IsNullOrEmpty(checkoutJson))
            {
                var checkout = JsonConvert.DeserializeObject<PaymentCheckoutViewModel>(checkoutJson);
                maSuat = checkout?.MaSuat;
            }

            if (maPhim == null || maDon == null)
            {
                return Json(new { success = false });
            }


            var chiTietGhe = _db.ChiTietDonDat.Where(x => x.MaDon == maDon).ToList();

            foreach (var ct in chiTietGhe)
            {
                await _hub.Clients
                    .Group($"SUAT_{ct.MaSuat}")
                    .SendAsync("SeatReleased", new { maGhe = ct.MaGhe });
            }

            if (chiTietGhe.Any())
            {
                _db.ChiTietDonDat.RemoveRange(chiTietGhe);
            }

            var chiTietCombo = _db.DonDatVeDoAns
                .Where(x => x.MaDon == maDon)
                .ToList();

            if (chiTietCombo.Any())
            {
                _db.DonDatVeDoAns.RemoveRange(chiTietCombo);
            }


            var don = _db.DonDatVes.FirstOrDefault(x => x.MaDon == maDon);
            don.TrangThai = "Đã hủy";

            _db.SaveChanges();

            HttpContext.Session.Remove("CheckoutData");
            HttpContext.Session.Remove("MaDon");

            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("Index", "DatVe", new
                {
                    maPhim = maPhim,
                    maSuat = maSuat
                })
            });
        }
        [HttpPost]
        public async Task<IActionResult> CancelHoldAsync()
        {
            int? maDon = HttpContext.Session.GetInt32("MaDon");
            if (maDon == null) return Ok();

            var gheList = _db.ChiTietDonDat.Where(x => x.MaDon == maDon).ToList();

            foreach (var ct in gheList)
            {
                await _hub.Clients
                    .Group($"SUAT_{ct.MaSuat}")
                    .SendAsync("SeatReleased", new { maGhe = ct.MaGhe });
            }

            var don = _db.DonDatVes.FirstOrDefault(x => x.MaDon == maDon);
            if (don.TrangThai == "Đã thanh toán")
                return Ok();
            if (don != null && don.TrangThai == "Chờ thanh toán")
            {
                don.TrangThai = "Hết hạn";

                var ghe = _db.ChiTietDonDat.Where(x => x.MaDon == maDon);
                var combo = _db.DonDatVeDoAns.Where(x => x.MaDon == maDon);

                _db.ChiTietDonDat.RemoveRange(ghe);
                _db.DonDatVeDoAns.RemoveRange(combo);
                _db.SaveChanges();
            }

            HttpContext.Session.Remove("MaDon");
            HttpContext.Session.Remove("CheckoutData");

            return Ok();
        }
        [HttpGet]
        public IActionResult QrInfo(int maDon, string token)
        {
            var don = _db.DonDatVes
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaGheNavigation)
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaSuatNavigation)
                        .ThenInclude(s => s.MaPhongNavigation)
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaSuatNavigation)
                        .ThenInclude(s => s.MaPhimNavigation)
                .FirstOrDefault(d => d.MaDon == maDon);

            if (don == null)
                return Content("❌ Vé không tồn tại");

            if (don.TrangThai != "Đã thanh toán")
                return Content("❌ Vé chưa thanh toán");

            var expectedToken = GenerateQrToken(don.MaDon, don.NgayDat);

            if (!string.Equals(token, expectedToken, StringComparison.OrdinalIgnoreCase))
                return Content("❌ Vé giả hoặc đã bị chỉnh sửa");

            return View(don);
        }

        [HttpGet]
        public IActionResult GetRemainingTime(int maDon)
        {
            var don = _db.DonDatVes.FirstOrDefault(x => x.MaDon == maDon);

            if (don == null || don.TrangThai != "Chờ thanh toán")
                return Json(new { expired = true });

            var expireTime = don.NgayDat.AddMinutes(HOLD_MINUTES);
            var remain = (int)(expireTime - DateTime.Now).TotalSeconds;

            if (remain <= 0)
                return Json(new { expired = true });

            return Json(new { seconds = remain });
        }

    }
}
