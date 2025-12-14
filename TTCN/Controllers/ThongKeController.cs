using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TTCN.Models;
using static TTCN.Models.ThongKe;

namespace TTCN.Controllers
{
    public class ThongKeController : Controller
    {
        private readonly QLDVContext _context;

        public ThongKeController(QLDVContext context)
        {
            _context = context;
        }

        public IActionResult Index(DateTime? tuNgay, DateTime? denNgay, string chonTP, int? chonMaRap)
        {
            // 1. Xử lý ngày mặc định
            var fromDate = tuNgay ?? DateTime.Now.Date.AddDays(-30);
            var toDate = denNgay ?? DateTime.Now.Date.AddDays(1).AddTicks(-1);

            // 2. Query đơn hàng cơ bản
            var query = _context.DonDatVes
                .Where(d => d.NgayDat >= fromDate && d.NgayDat <= toDate)
                .Where(d => d.TrangThai.Contains("thanh toán") || d.TrangThai.Contains("Hoàn tất"));

            // 3. Lọc theo Thành phố & Rạp
            if (!string.IsNullOrEmpty(chonTP) || chonMaRap.HasValue)
            {
                query = query.Where(d => d.ChiTietDonDat.Any(ct =>
                    (string.IsNullOrEmpty(chonTP) || ct.MaSuatNavigation.MaPhongNavigation.MaCumRapNavigation.ThanhPho == chonTP) &&
                    (!chonMaRap.HasValue || ct.MaSuatNavigation.MaPhongNavigation.MaCumRapNavigation.MaCumRap == chonMaRap)
                ));
            }

            // --- 4. TÍNH TOÁN TỔNG QUAN ---
            var listDonHang = query
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaSuatNavigation)
                        .ThenInclude(s => s.MaPhongNavigation)
                            .ThenInclude(p => p.MaCumRapNavigation)
                .Include(d => d.DonDatVeDoAns)
                    .ThenInclude(da => da.MaComboNavigation)
                .ToList();

            decimal tongTienVe = listDonHang.Sum(d => d.ChiTietDonDat.Sum(ct => ct.MaSuatNavigation.Gia ?? 0));
            // Tính tiền combo: Ưu tiên lấy giá lịch sử, nếu không lấy giá hiện tại
            decimal tongTienCombo = listDonHang.Sum(d => d.DonDatVeDoAns.Sum(da => da.SoLuong * (da.Gia > 0 ? da.Gia : da.MaComboNavigation.Gia)));

            var model = new ThongKe
            {
                TuNgay = fromDate,
                DenNgay = toDate,
                chonTP = chonTP,
                chonMaRap = chonMaRap,

                TongDoanhThu = tongTienVe + tongTienCombo,
                DoanhThuCombo = tongTienCombo,
                SoVeBanRa = listDonHang.Sum(d => d.ChiTietDonDat.Count),
                SoDonHang = listDonHang.Count
            };

            ViewBag.DoanhThuVe = tongTienVe;


            // --- 5. DỮ LIỆU BIỂU ĐỒ CỘT (Theo ngày) ---
            var dataChart = listDonHang
                .GroupBy(x => x.NgayDat.Date)
                .Select(g => new
                {
                    Ngay = g.Key,
                    TienVe = g.Sum(d => d.ChiTietDonDat.Sum(ct => ct.MaSuatNavigation.Gia ?? 0)),
                    TienCombo = g.Sum(d => d.DonDatVeDoAns.Sum(da => da.SoLuong * (da.Gia > 0 ? da.Gia : da.MaComboNavigation.Gia))),
                    SoVe = g.Sum(d => d.ChiTietDonDat.Count),
                    SoDon = g.Count()
                })
                .OrderBy(x => x.Ngay)
                .ToList();

            model.LabelsNgay = dataChart.Select(x => x.Ngay.ToString("dd/MM")).ToList();
            model.DataDoanhThuNgay = dataChart.Select(x => x.TienVe + x.TienCombo).ToList();
            model.DataVeBanNgay = dataChart.Select(x => x.SoVe).ToList();
            model.DataComboNgay = dataChart.Select(x => x.TienCombo).ToList();
            model.DataDonHangNgay = dataChart.Select(x => x.SoDon).ToList();


            // --- 6. DỮ LIỆU BIỂU ĐỒ TRÒN (Theo Rạp) ---
            // Gom nhóm theo Rạp
            var groupedRap = listDonHang
                .GroupBy(d => d.ChiTietDonDat.FirstOrDefault()?.MaSuatNavigation.MaPhongNavigation.MaCumRapNavigation.TenCumRap ?? "Khác")
                .Select(g => new
                {
                    TenRap = g.Key,
                    RevenueVe = g.Sum(d => d.ChiTietDonDat.Sum(ct => ct.MaSuatNavigation.Gia ?? 0)),
                    RevenueCombo = g.Sum(d => d.DonDatVeDoAns.Sum(da => da.SoLuong * (da.Gia > 0 ? da.Gia : da.MaComboNavigation.Gia))),
                    SoVe = g.Sum(d => d.ChiTietDonDat.Count),
                    SoDon = g.Count()
                })
                .OrderByDescending(x => x.RevenueVe + x.RevenueCombo)
                .ToList();

            // Truyền các danh sách dữ liệu riêng biệt sang View để vẽ biểu đồ tròn động
            ViewBag.PieLabels = groupedRap.Select(x => x.TenRap).ToList();

            ViewBag.PieData_Tong = groupedRap.Select(x => x.RevenueVe + x.RevenueCombo).ToList(); // Tổng
            ViewBag.PieData_Ve = groupedRap.Select(x => x.RevenueVe).ToList();                   // Chỉ tiền vé
            ViewBag.PieData_Combo = groupedRap.Select(x => x.RevenueCombo).ToList();             // Chỉ tiền combo
            ViewBag.PieData_SoVe = groupedRap.Select(x => x.SoVe).ToList();                      // Số lượng vé
            ViewBag.PieData_SoDon = groupedRap.Select(x => x.SoDon).ToList();                    // Số đơn hàng


            // Top Phim (Giữ nguyên logic cũ)
            var topPhimData = _context.ChiTietDonDat
                .Where(ct => query.Any(d => d.MaDon == ct.MaDon))
                .GroupBy(ct => ct.MaSuatNavigation.MaPhimNavigation.TenPhim)
                .Select(g => new TopPhimVM
                {
                    TenPhim = g.Key,
                    SoVe = g.Count(),
                    DoanhThu = g.Sum(x => x.MaSuatNavigation.Gia) ?? 0
                })
                .OrderByDescending(x => x.SoVe)
                .Take(5)
                .ToList();
            model.TopPhims = topPhimData;

            // Dropdown
            var cities = _context.CumRaps.Select(c => c.ThanhPho).Distinct().ToList();
            model.thanhPho = new SelectList(cities, chonTP);

            var cinemasQuery = _context.CumRaps.AsQueryable();
            if (!string.IsNullOrEmpty(chonTP))
            {
                cinemasQuery = cinemasQuery.Where(c => c.ThanhPho == chonTP);
            }
            model.rapChieu = new SelectList(cinemasQuery.ToList(), "MaCumRap", "TenCumRap", chonMaRap);

            return View(model);
        }

        [HttpGet]
        public IActionResult GetCinemasByCity(string cityName)
        {
            var cinemas = _context.CumRaps.AsQueryable();
            if (!string.IsNullOrEmpty(cityName))
            {
                cinemas = cinemas.Where(c => c.ThanhPho == cityName);
            }
            var result = cinemas.Select(c => new { id = c.MaCumRap, name = c.TenCumRap }).ToList();
            return Json(result);
        }
    }
}