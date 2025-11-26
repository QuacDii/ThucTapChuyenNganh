using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TTCN.Models;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Microsoft.IdentityModel.Tokens;

namespace TTCN.Controllers
{
    public class HomeController : Controller
    {
        private readonly QLDVContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, QLDVContext context)
        {
            _logger = logger;
            _context = context;
        }
        QLDVContext db = new QLDVContext();
        // Action Index (Trang chủ) 
        public IActionResult Index()
        {
            List<LayDuLieuPhim> lsCongChieu = new List<LayDuLieuPhim>();
            List<LayDuLieuPhim> lsSapChieu = new List<LayDuLieuPhim>();
            List<LayDuLieuPhim> lsHot = new List<LayDuLieuPhim>();


            //---------------------------------------------
            // LẤY DANH SÁCH PHIM ĐANG CÔNG CHIẾU
            //---------------------------------------------

            var phimCongChieu = (from tbPhim in db.Phims
                                 join tbPhimTheLoai in db.PhimTheLoais on tbPhim.MaPhim equals tbPhimTheLoai.MaPhim
                                 join tbTheLoai in db.TheLoais on tbPhimTheLoai.MaTheLoai equals tbTheLoai.MaTheLoai
                                 join tbUsersPhim in db.UsersPhims on tbPhim.MaPhim equals tbUsersPhim.MaPhim
                                 where tbPhim.TrangThai == "Đang công chiếu"
                                 select new
                                 {
                                     tbPhim.MaPhim,
                                     tbPhim.TenPhim,
                                     tbPhim.PosterPhim,
                                     tbPhim.TrailerPhim,
                                     tbUsersPhim.Diem,
                                     tbTheLoai.TenTheLoai
                                 }).ToList();

            var diemTB = from p in phimCongChieu
                         group p.Diem by p.MaPhim into g
                         select new
                         {
                             MaPhim = g.Key,
                             DiemTB = g.Average()
                         };

            var nhomPhim = from p in phimCongChieu
                           group p by new
                           {
                               p.MaPhim,
                               p.TenPhim,
                               p.PosterPhim,
                               p.TrailerPhim
                           } into g
                           select new LayDuLieuPhim
                           {
                               MaPhim = g.Key.MaPhim,
                               TenPhim = g.Key.TenPhim,
                               PosterPhim = g.Key.PosterPhim,
                               TrailerPhim = g.Key.TrailerPhim,
                               DiemTB = (int)diemTB.Where(d => d.MaPhim == g.Key.MaPhim)
                                                   .Select(d => d.DiemTB)
                                                   .FirstOrDefault(),
                               TenTheLoai = g.Select(x => x.TenTheLoai).Distinct().ToList()
                           };

            lsCongChieu = nhomPhim.ToList();
            ViewBag.CongChieu = lsCongChieu;

            //---------------------------------------------
            // ⭐ LẤY TOP 3 PHIM HOT (ĐIỂM CAO NHẤT)
            //---------------------------------------------

            lsHot = lsCongChieu
                        .OrderByDescending(p => p.DiemTB)
                        .Take(3)
                        .ToList();

            ViewBag.PhimHot = lsHot;

            //---------------------------------------------
            //  SẮP CÔNG CHIẾU
            //---------------------------------------------

            var phimSapChieu = (from tbPhim in db.Phims
                                join tbPhimTheLoai in db.PhimTheLoais on tbPhim.MaPhim equals tbPhimTheLoai.MaPhim
                                join tbTheLoai in db.TheLoais on tbPhimTheLoai.MaTheLoai equals tbTheLoai.MaTheLoai
                                where tbPhim.TrangThai == "Sắp công chiếu"
                                select new
                                {
                                    tbPhim.MaPhim,
                                    tbPhim.TenPhim,
                                    tbPhim.PosterPhim,
                                    tbPhim.TrailerPhim,
                                    tbTheLoai.TenTheLoai
                                }).ToList();

            var nhomPhimSapChieu = from p in phimSapChieu
                                   group p by new
                                   {
                                       p.MaPhim,
                                       p.TenPhim,
                                       p.PosterPhim,
                                       p.TrailerPhim
                                   } into g
                                   select new LayDuLieuPhim
                                   {
                                       MaPhim = g.Key.MaPhim,
                                       TenPhim = g.Key.TenPhim,
                                       PosterPhim = g.Key.PosterPhim,
                                       TrailerPhim = g.Key.TrailerPhim,
                                       DiemTB = 0,
                                       TenTheLoai = g.Select(x => x.TenTheLoai).ToList()
                                   };

            lsSapChieu = nhomPhimSapChieu.ToList();
            ViewBag.SapChieu = lsSapChieu;

            return View();
        }

    }
}