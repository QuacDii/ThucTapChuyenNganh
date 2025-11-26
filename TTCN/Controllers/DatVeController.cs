using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class DatVeController : Controller
    {
        QLDVContext db = new QLDVContext();
        public IActionResult Index(int maPhim)
        {
            var dlPhim = (from tbPhim in db.Phims
                          join tbPhimTheLoai in db.PhimTheLoais on tbPhim.MaPhim equals tbPhimTheLoai.MaPhim
                          join tbTheLoai in db.TheLoais on tbPhimTheLoai.MaTheLoai equals tbTheLoai.MaTheLoai
                          join tbUsersPhim in db.UsersPhims on tbPhim.MaPhim equals tbUsersPhim.MaPhim
                          where tbPhim.MaPhim == maPhim
                          select new
                          {
                              tbPhim.MaPhim,
                              tbPhim.TenPhim,
                              tbPhim.PosterPhim,
                              tbPhim.ThoiLuong,
                              tbPhim.NgayPhatHanh,
                              tbPhim.NgayKetThuc,
                              tbTheLoai.TenTheLoai
                          }).ToList();
            var ngayPhim = dlPhim.Where(p => p.MaPhim == maPhim).Select(p => new
            {
                p.NgayPhatHanh,
                p.NgayKetThuc
            }).FirstOrDefault();

            DateTime ngayBatDau = (DateTime)(DateTime.Today > ngayPhim.NgayPhatHanh ? DateTime.Today : ngayPhim.NgayPhatHanh);

            int soNgay = (ngayPhim.NgayKetThuc.Value - ngayBatDau).Days;

            List<DateTime> listNgay = new List<DateTime>();
            for (int i = 0; i < soNgay; i++)
            {
                DateTime ngay = ngayBatDau.AddDays(i);
                if (ngay <= ngayPhim.NgayKetThuc)
                {
                    listNgay.Add(ngay);
                }
            }

            var nhomPhim = from p in dlPhim
                           group p by new
                           {
                               p.MaPhim,
                               p.TenPhim,
                               p.PosterPhim,
                               p.ThoiLuong
                           } into g
                           select new DuLieuDatVe
                           {
                               MaPhim = g.Key.MaPhim,
                               TenPhim = g.Key.TenPhim,
                               PosterPhim = g.Key.PosterPhim,
                               ThoiLuong = g.Key.ThoiLuong,
                               CacNgayChieu = listNgay,
                               TenTheLoai = g.Select(x => x.TenTheLoai).Distinct().ToList()
                           };
            ViewBag.dlPhimDaChon = nhomPhim;

            var dlSuatChieu = db.Phims.Where(p => p.MaPhim == maPhim)
                                      .Include(sc => sc.SuatChieus)
                                      .ThenInclude(pc => pc.MaPhongNavigation)
                                      .ThenInclude(cr => cr.MaCumRapNavigation)
                                      .FirstOrDefault();
            ViewBag.dlSuatChieu = dlSuatChieu;
            return View();
        }
        [HttpGet]
        public IActionResult GetGheBySuatChieu(int maSuat)
        {
            var suat = db.SuatChieus
                .Include(s => s.MaPhongNavigation)
                .ThenInclude(p => p.GheNgois)
                .FirstOrDefault(s => s.MaSuat == maSuat);

            if (suat == null) return NotFound();

            // Các ghế đã được đặt (TrangThai = true)
            var gheDaDat = db.ChiTietScGns
                .Where(ct => ct.MaSuat == maSuat && ct.TrangThai && ct.MaGhe != null)
                .Select(ct => ct.MaGhe.Value)
                .ToHashSet();

            // Map dữ liệu trả về JSON, sắp theo Hàng + Tên ghế
            var data = suat.MaPhongNavigation.GheNgois
                .OrderBy(g => g.HangGhe)
                .ThenBy(g => g.TenGhe)
                .Select(g => new
                {
                    g.MaGhe,
                    g.TenGhe,
                    g.HangGhe,
                    g.LoaiGhe,
                    GiaGhe = string.Equals(g.LoaiGhe, "VIP", StringComparison.OrdinalIgnoreCase) ? 120000 : 90000,
                    DaDat = gheDaDat.Contains(g.MaGhe)
                })
                .ToList();

            return Json(data);
        }

    }
}
