using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;
using TTCN.Models;
using TTCN.Models.Momo;
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

        public PaymentController(
            QLDVContext db,
            IMomoService momoService,
            ILogger<PaymentController> logger)
        {
            _db = db;
            _momoService = momoService;
            _logger = logger;
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
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

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
            foreach (var seat in seats)
            {
                var ghe = _db.GheNgois.FirstOrDefault(g => g.TenGhe == seat.Name);
                if (ghe != null)
                {
                    var ct = new ChiTietDonDat
                    {
                        MaDon = don.MaDon,
                        MaGhe = ghe.MaGhe,
                        MaSuat = checkout.MaSuat,
                        TrangThai = true   // false = giữ chỗ, chưa xác nhận
                    };
                    _db.ChiTietDonDat.Add(ct);
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
            var response = await _momoService.CreatePaymentMomo(model);
            if (response == null || string.IsNullOrEmpty(response.PayUrl))
                return Content("❌ MoMo tạo lỗi:<br><pre>" + JsonConvert.SerializeObject(response, Formatting.Indented) + "</pre>");


            return Redirect(response.PayUrl);
        }

        // =============== BƯỚC 3: MOMO CALLBACK ===============
        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            _logger.LogInformation("MOMO CALLBACK HIT");

            // Lấy dữ liệu MoMo trả về
            var response = _momoService.PaymentExecuteAsync(Request.Query);
            if (response == null)
                return Content("Không nhận được phản hồi từ MoMo");

            // ExtraData bắt buộc phải là MaDon
            if (!int.TryParse(response.ExtraData, out int maDon))
                return Content("ExtraData không hợp lệ");

            // Tìm đơn trong DB
            var don = _db.DonDatVes.FirstOrDefault(x => x.MaDon == maDon);
            if (don == null)
                return Content("Không tìm thấy đơn trong DB");

            var chiTietList = _db.ChiTietDonDat
                    .Where(x => x.MaDon == don.MaDon)
                    .ToList();

            var chiTietDoAn = _db.DonDatVeDoAns.Where(x => x.MaDon == don.MaDon).ToList();

            //if (don.TrangThai == "Hết hạn")
            //{
            //    _logger.LogWarning($"Đơn {maDon} đã hết hạn – từ chối thanh toán MoMo");

            //    //_db.DonDatVes.Remove(don);
            //    //foreach (var ct in chiTietList)
            //    //{
            //    //    _db.ChiTietDonDat.Remove(ct);
            //    //}

            //    //foreach (var da in chiTietDoAn)
            //    //{
            //    //    _db.DonDatVeDoAns.Remove(da);
            //    //}
            //    //_db.SaveChanges();
            //    return RedirectToAction("Index", "Home");
            //}
            if (don.TrangThai == "Hết hạn" && response.ResultCode == 0)
            {
                _logger.LogWarning($"[MOCK REFUND] Đơn {don.MaDon} quá hạn – hoàn tiền");

                don.TrangThai = "Đã hoàn tiền (Mock)";
                _db.SaveChanges();

                return RedirectToAction("Index", "Home");
            }

            // Thanh toán thành công
            if (response.ResultCode == 0)
            {
                don.TrangThai = "Đã thanh toán";
                _db.SaveChanges();

                return RedirectToAction("ThanhToanThanhCong", "DatVe");
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

                return RedirectToAction("ThanhToanThatBai", "DatVe");
            }
        }
        [HttpGet]
        public IActionResult CheckOrderStatus(int maDon)
        {
            var don = _db.DonDatVes.FirstOrDefault(x => x.MaDon == maDon);

            if (don == null)
                return Json(new { status = "NOT_FOUND" });

            return Json(new { status = don.TrangThai });
        }

    }
}
