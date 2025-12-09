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
                .Include(d => d.MaSuatNavigation)
                    .ThenInclude(s => s.MaPhimNavigation)// Lấy tên phim để hiển thị
                    .Include(d => d.MaSuatNavigation)
                .Include(d => d.MaSuatNavigation)
                    .ThenInclude(s => s.MaPhongNavigation)
                    .ThenInclude(p => p.MaCumRapNavigation)
                .Include(d => d.MaUsersNavigation)       // Lấy tên người đặt 
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
    }
}
