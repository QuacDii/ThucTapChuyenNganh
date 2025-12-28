using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class LichChieuController : Controller
    {
        private readonly QLDVContext _context;

        public LichChieuController(QLDVContext context)
        {
            _context = context;
        }

        public IActionResult TheoRap(int? maCumRap, DateTime? ngay)
        {
            var today = DateTime.Today;

            var minDate = today;
            var maxDate = today.AddDays(14); // fallback

            // nếu đã chọn rạp → lấy ngày theo phim
            if (maCumRap != null)
            {
                minDate = _context.SuatChieus
                    .Where(s => s.MaPhongNavigation.MaCumRap == maCumRap)
                    .Min(s => s.GioBatDau)!.Value.Date;

                maxDate = _context.SuatChieus
                    .Where(s => s.MaPhongNavigation.MaCumRap == maCumRap)
                    .Max(s => s.GioBatDau)!.Value.Date;

                if (minDate < today) minDate = today;
            }

            var ngayChieu = ngay ?? minDate;

            var vm = new LichChieuTheoRap
            {
                DsCumRap = _context.CumRaps.OrderBy(x => x.TenCumRap).ToList(),
                MaCumRap = maCumRap,
                NgayChieu = ngayChieu,
                MinDate = minDate,
                MaxDate = maxDate
            };

            // Chưa chọn rạp → chỉ hiển thị UI
            if (maCumRap == null)
                return View(vm);

            // ===== 2. Lấy danh sách suất chiếu =====
            var now = DateTime.Now;

            var suatChieus = _context.SuatChieus
                .Include(s => s.MaPhimNavigation)
                    .ThenInclude(p => p.PhimTheLoais)
                        .ThenInclude(pt => pt.MaTheLoaiNavigation)
                .Include(s => s.MaPhongNavigation)
                    .ThenInclude(p => p.GheNgois)
                .Include(s => s.ChiTietDonDat)
                .Where(s =>
                    s.MaPhongNavigation.MaCumRap == maCumRap &&
                    s.GioBatDau.HasValue &&
                    s.GioBatDau.Value.Date == vm.NgayChieu.Date &&
                    (
                        vm.NgayChieu.Date > now.Date ||          // ngày tương lai → lấy hết
                        s.GioBatDau.Value >= now                 // hôm nay → chỉ suất chưa chiếu
                    )
                )
                .ToList();

            // ===== 3. Group theo phim + tính ghế trống =====
            vm.Phims = suatChieus
                .GroupBy(s => s.MaPhimNavigation)
                .Select(g => new LichChieuPhim
                {
                    MaPhim = g.Key.MaPhim,
                    TenPhim = g.Key.TenPhim,
                    PosterPhim = g.Key.PosterPhim,
                    ThoiLuong = g.Key.ThoiLuong,

                    TheLoai = string.Join(", ",
                        g.Key.PhimTheLoais
                            .Select(pt => pt.MaTheLoaiNavigation.TenTheLoai)
                    ),

                    SuatChieus = g.Select(s => new SuatChieuItem
                    {
                        MaSuat = s.MaSuat,
                        GioBatDau = s.GioBatDau!.Value,
                        TenPhong = s.MaPhongNavigation.TenPhong,

                        // ===== TÍNH GHẾ TRỐNG =====
                        SoGheTrong =
                            s.MaPhongNavigation.GheNgois.Count
                            - s.ChiTietDonDat.Count
                    })
                    .OrderBy(x => x.GioBatDau)
                    .ToList()
                })
                .OrderBy(x => x.TenPhim)
                .ToList();

            return View(vm);
        }
    }
}
