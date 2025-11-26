using Microsoft.AspNetCore.Mvc;
using TTCN.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;     
using System.Collections.Generic;      
using System.Linq;

namespace TTCN.Controllers
{
    public class PhimController : Controller
    {
        private readonly QLDVContext _context;
        public PhimController(QLDVContext context)
        {
            this._context = context;
        }
        public IActionResult Index()
        {
            var dsPhim = _context.Phims
                            .Include(p => p.PhimTheLoais)
                            .ThenInclude(pt1 => pt1.MaTheLoaiNavigation)
                            .ToList();
            ViewBag.ph = dsPhim;
            return View();
        }

        [HttpGet]
        public ActionResult them()
        {
            var allTheLoais = _context.TheLoais.ToList();
            ViewBag.AllTheLoais = allTheLoais;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult them(Phim p, List<int> select)
        {
            ModelState.Remove("MaPhim");
            ModelState.Remove("TrangThai");
            ModelState.Remove("PhimTheLoais");
            ModelState.Remove("SuatChieus");

            if (p.NgayPhatHanh != default(DateTime) && p.NgayKetThuc != default(DateTime))
            {
                if (p.NgayKetThuc < p.NgayPhatHanh)
                    ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không được nhỏ hơn ngày phát hành!");
            }

            if (ModelState.IsValid)
            {
                int max = 0;
                if (_context.Phims.Any())
                {
                    max=_context.Phims.Max(p=>p.MaPhim);
                }

                p.MaPhim = max + 1;

                DateTime hnay=DateTime.Now;
                if (p.NgayPhatHanh > hnay)
                    p.TrangThai = "Sắp công chiếu";
                else if (p.NgayKetThuc < hnay)
                    p.TrangThai = "Đã chiếu";
                else p.TrangThai = "Đang công chiếu";

                _context.Phims.Add(p);
                _context.SaveChanges();

                int maPhimnew = p.MaPhim;

                if (select != null)
                {
                    foreach(var maTheLoai in select)
                    {
                        var theLoaiPhim = new PhimTheLoai
                        {
                            MaPhim = maPhimnew,
                            MaTheLoai = maTheLoai
                        };
                        _context.PhimTheLoais.Add(theLoaiPhim);
                    }
                    _context.SaveChanges();
                }
                return RedirectToAction("Index");
            }
            ViewBag.AllTheLoais = _context.TheLoais.ToList();
            return View(p);
        }

        [HttpGet]
        public ActionResult sua(int id)
        {
            Phim p = _context.Phims
                .Include(ph => ph.PhimTheLoais)
                .FirstOrDefault(ph => ph.MaPhim == id);

            if (p == null) return NotFound();

            ViewBag.AllTheLoais=_context.TheLoais.ToList();

            ViewBag.Select=p.PhimTheLoais.Select(p1 => p1.MaTheLoai).ToList();

            ViewBag.ph = p;
            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult sua(Phim ph, List<int> select)
        {
            ModelState.Remove("TrangThai");
            ModelState.Remove("PhimTheLoais");
            ModelState.Remove("SuatChieus");

            if (ph.NgayPhatHanh != default(DateTime) && ph.NgayKetThuc != default(DateTime))
            {
                if (ph.NgayKetThuc < ph.NgayPhatHanh)
                {
                    ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không được nhỏ hơn ngày phát hành!");
                }
            }
            if (ModelState.IsValid)
            {
                Phim p = _context.Phims
                    .Include(ph => ph.PhimTheLoais)
                    .FirstOrDefault(ph1 => ph1.MaPhim == ph.MaPhim);

                if(p == null) return NotFound();

                p.TenPhim = ph.TenPhim;
                p.MoTa = ph.MoTa;
                p.ThoiLuong = ph.ThoiLuong;
                p.NgayPhatHanh = ph.NgayPhatHanh;
                p.NgayKetThuc = ph.NgayKetThuc;
                p.DaoDien = ph.DaoDien;
                p.PosterPhim = ph.PosterPhim;
                p.TrailerPhim = ph.TrailerPhim;

                DateTime hnay = DateTime.Now;
                if (p.NgayKetThuc < hnay)
                {
                    p.TrangThai = "Đã chiếu";
                }
                else if (p.NgayPhatHanh > hnay)
                {
                    p.TrangThai = "Sắp công chiếu";
                }
                else
                {
                    p.TrangThai = "Đang công chiếu";
                }

                //Xóa thể loại cũ
                if (p.PhimTheLoais != null && p.PhimTheLoais.Any())
                {
                    _context.PhimTheLoais.RemoveRange(p.PhimTheLoais);
                }

                //Thêm thể loại mới
                if(select!= null)
                {
                    select = select.Distinct().ToList();
                    foreach (var maTheLoai in select)
                    {
                        _context.PhimTheLoais.Add(new PhimTheLoai
                        {
                            MaPhim=p.MaPhim,
                            MaTheLoai=maTheLoai
                        });
                    }
                }
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AllTheLoais = _context.TheLoais.ToList();
            ViewBag.Select = select ?? new List<int>(); // Hiển thị lại các checkbox đã check
            return View(ph);
        }

        [HttpGet]
        public ActionResult xoa(int id)
        {
            Phim p = _context.Phims
                  .Include(ph => ph.PhimTheLoais)
                  .ThenInclude(ptl => ptl.MaTheLoaiNavigation)
                  .FirstOrDefault(ph => ph.MaPhim == id);

            if (p == null) return NotFound();

            ViewBag.ph = p;
            return View(p);
        }

        [HttpPost, ActionName("xoa")]
        [ValidateAntiForgeryToken]
        public ActionResult xoa_Post(int id)
        {
            Phim ph = _context.Phims
                           .Include(p => p.PhimTheLoais)
                           .FirstOrDefault(p => p.MaPhim == id);

            if (ph != null)
            {
                _context.PhimTheLoais.RemoveRange(ph.PhimTheLoais);

                _context.Phims.Remove(ph);

                _context.SaveChanges(); 
            }
            return RedirectToAction("Index");
        }
    }
}

