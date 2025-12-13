using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class DonDatVeController : Controller
    {
        private readonly QLDVContext _context;

        public DonDatVeController(QLDVContext context)
        {
            _context = context;
        }
        public IActionResult Index(int? searchMa, decimal? min, decimal? max, string trangThai, DateTime? ngayDat)
        {
            // Cần Include để lấy thông tin Suất chiếu (Phim) và User đặt vé
            var query = _context.DonDatVes
                .Include(k=>k.ChiTietDonDat)
                    .ThenInclude(d => d.MaSuatNavigation)
                        .ThenInclude(s => s.MaPhimNavigation)// Lấy tên phim để hiển thị
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaSuatNavigation)
                        .ThenInclude(s => s.MaPhongNavigation)
                            .ThenInclude(p => p.MaCumRapNavigation)
                .Include(u=>u.MaUsersNavigation)// Lấy tên người đặt 
                .AsQueryable();

            // 2. Lọc theo Mã Đơn 
            if (searchMa.HasValue)
            {
                query = query.Where(d => d.MaDon == searchMa.Value);
            }

            // 3. Lọc theo Trạng Thái 
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai.Contains(trangThai));
            }

            // 4. Lọc theo Ngày Đặt
            if (ngayDat.HasValue)
            {
                query = query.Where(d => d.NgayDat.Date == ngayDat.Value.Date);
                ViewBag.CurrentDate = ngayDat.Value.ToString("yyyy-MM-dd");
            }

            // 5. Lọc theo Tổng Tiền (Khoảng giá)
            if (min.HasValue)
            {
                query = query.Where(d => d.TongTien >= min.Value);
            }
            if (max.HasValue)
            {
                query = query.Where(d => d.TongTien <= max.Value);
            }

            ViewBag.CurrentMa = searchMa;
            ViewBag.CurrentStatus = trangThai;

            var result = query.OrderByDescending(d => d.NgayDat).ToList();

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult huyDon(int id)
        {
            var donDatVe = _context.DonDatVes.Find(id);
            if (donDatVe == null)
            {
                return NotFound();
            }

            // 1. Kiểm tra điều kiện hủy
            var listChiTiet = _context.ChiTietDonDat
                              .Include(ct => ct.MaSuatNavigation)
                              .Where(ct => ct.MaDon == id)
                              .ToList();

            // Lấy thông tin suất chiếu từ vé đầu tiên (vì 1 đơn cùng 1 suất)
            var chiTietDau = listChiTiet.FirstOrDefault();

            if (chiTietDau != null && chiTietDau.MaSuatNavigation != null)
            {
                // Nếu giờ bắt đầu <= giờ hiện tại => Đã chiếu => Không cho hủy
                if (chiTietDau.MaSuatNavigation.GioBatDau <= DateTime.Now)
                {
                    TempData["Error"] = "Phim đang chiếu hoặc đã chiếu, không thể hủy đơn!";
                    return RedirectToAction("Index");
                }
            }

            donDatVe.TrangThai = "Đã hủy";
            donDatVe.TongTien = 0;

            // 5. Nhả ghế
            // Khi xóa đi, ghế đó sẽ không còn tồn tại trong bảng đặt ghế => Trạng thái ghế trở lại là trống.
            if (listChiTiet.Any())
            {
                _context.ChiTietDonDat.RemoveRange(listChiTiet);
            }

            _context.SaveChanges();
            TempData["Success"] = "Đã hủy đơn vé và nhả ghế thành công!";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult chiTiet(int id)
        {
            var donDatVe = _context.DonDatVes
        // 1. Thông tin khách hàng
        .Include(d => d.MaUsersNavigation)

        // 2. Thông tin Ghế và Phim 
        .Include(d => d.ChiTietDonDat)
            .ThenInclude(ct => ct.MaGheNavigation) // Lấy tên ghế (A1, A2...) và Loại ghế
        .Include(d => d.ChiTietDonDat)
            .ThenInclude(ct => ct.MaSuatNavigation)
                .ThenInclude(s => s.MaPhimNavigation)
        .Include(d => d.ChiTietDonDat)
            .ThenInclude(ct => ct.MaSuatNavigation)
                .ThenInclude(s => s.MaPhongNavigation)
                    .ThenInclude(p => p.MaCumRapNavigation)

        // 3. Lấy thông tin Combo (Bắp/Nước)
        .Include(d => d.DonDatVeDoAns)
            .ThenInclude(c => c.MaComboNavigation) // Lấy tên Combo và Giá tiền

        .FirstOrDefault(d => d.MaDon == id);

            if (donDatVe == null) return NotFound();

            return View(donDatVe);
        }

        [HttpGet]
        public IActionResult chiTietDon(int id)
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail)) return RedirectToAction("Index", "Login");

            var donVe = _context.DonDatVes
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaGheNavigation) // Lấy tên ghế
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaSuatNavigation)
                    .ThenInclude(s => s.MaPhimNavigation) // Lấy tên phim, poster
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaSuatNavigation)
                    .ThenInclude(s => s.MaPhongNavigation)
                    .ThenInclude(p => p.MaCumRapNavigation) // Lấy tên rạp
                .Include(d => d.DonDatVeDoAns)
                    .ThenInclude(da => da.MaComboNavigation) // Lấy combo bắp nước
                .FirstOrDefault(d => d.MaDon == id);

            if (donVe == null) return NotFound();

            return View(donVe);
        }

        [HttpGet]
        public IActionResult LichSu()
        {
            // 1. Lấy Email user từ Session
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login", new { area = "Account" }); // Điều hướng về trang login
            }

            var listVe = _context.DonDatVes
                .Include(d => d.ChiTietDonDat).ThenInclude(ct => ct.MaSuatNavigation).ThenInclude(s => s.MaPhimNavigation)
                .Include(d => d.ChiTietDonDat).ThenInclude(ct => ct.MaGheNavigation)
                .Where(d => d.MaUsersNavigation.Email == userEmail)
                .OrderByDescending(d => d.NgayDat)
                .ToList();

            return View(listVe);
        }
    }

}
