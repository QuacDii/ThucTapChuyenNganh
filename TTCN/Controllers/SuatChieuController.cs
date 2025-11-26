using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class SuatChieuController : Controller
    {
        private readonly QLDVContext _context;
        public SuatChieuController(QLDVContext context)
        {
            this._context = context;
        }

        public IActionResult Index()
        {
            var dsSuat = _context.SuatChieus
                            .Include(s => s.MaPhimNavigation)
                            .Include(s => s.MaPhongNavigation)
                            .ToList();
            return View(dsSuat);
        }

        [HttpGet]
        public IActionResult them()
        {
            ViewBag.MaPhim = new SelectList(_context.Phims, "MaPhim", "TenPhim");
            ViewBag.MaPhong = new SelectList(_context.PhongChieus, "MaPhong", "TenPhong");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult them(SuatChieu sc)
        {
            ModelState.Remove("MaPhimNavigation");
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("DonDatVes");
            ModelState.Remove("ChiTietScGnVes ");

            if (sc.MaPhim != null)
            {
                var phim = _context.Phims.Find(sc.MaPhim);
                if (phim != null && sc.GioBatDau.HasValue && phim.NgayPhatHanh.HasValue && phim.NgayKetThuc.HasValue)
                {
                    if (sc.GioBatDau.Value.Date < phim.NgayPhatHanh.Value.Date ||
                        sc.GioBatDau.Value.Date > phim.NgayKetThuc.Value.Date)
                    {
                        ModelState.AddModelError("GioBatDau",
                            $"Ngày chiếu {sc.GioBatDau.Value:dd/MM/yyyy} không hợp lệ. Phim chỉ chiếu từ {phim.NgayPhatHanh:dd/MM/yyyy} đến {phim.NgayKetThuc:dd/MM/yyyy}");
                    }
                }
            }


            if (ModelState.IsValid)
            {
                int maxId = _context.SuatChieus.Any() ? _context.SuatChieus.Max(s => s.MaSuat) : 0;
                sc.MaSuat = maxId + 1;
                _context.Add(sc);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaPhim = new SelectList(_context.Phims, "MaPhim", "TenPhim", sc.MaPhim);
            ViewBag.MaPhong = new SelectList(_context.PhongChieus, "MaPhong", "TenPhong", sc.MaPhong);
            return View(sc);
        }

        [HttpGet]
        public IActionResult sua(int id)
        {
            var sc = _context.SuatChieus.Find(id);
            if (sc == null) return NotFound();

            ViewBag.MaPhim = new SelectList(_context.Phims, "MaPhim", "TenPhim", sc.MaPhim);
            ViewBag.MaPhong = new SelectList(_context.PhongChieus, "MaPhong", "TenPhong", sc.MaPhong);
            return View(sc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult sua(int id, SuatChieu sc)
        {
            if (id != sc.MaSuat) return NotFound();

            ModelState.Remove("MaPhimNavigation");
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("DonDatVes");
            ModelState.Remove("ChiTietScGnVes");

            if (sc.MaPhim != null)
            {
                var phim = _context.Phims.Find(sc.MaPhim);
                if (phim != null && sc.GioBatDau.HasValue && phim.NgayPhatHanh.HasValue && phim.NgayKetThuc.HasValue)
                {
                    if (sc.GioBatDau.Value.Date < phim.NgayPhatHanh.Value.Date ||
                        sc.GioBatDau.Value.Date > phim.NgayKetThuc.Value.Date)
                    {
                        ModelState.AddModelError("GioBatDau", $"Ngày chiếu không hợp lệ (Ngoài khoảng chiếu của phim).");
                    }
                }
            }

            if (sc.GioBatDau.HasValue && sc.GioKetThuc.HasValue && sc.GioKetThuc <= sc.GioBatDau)
            {
                ModelState.AddModelError("GioKetThuc", "Giờ kết thúc phải lớn hơn giờ bắt đầu.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(sc);
                    _context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!kiemTra(sc.MaSuat)) return NotFound();
                    else throw;
                }
                return RedirectToAction("Index");
            }

            ViewBag.MaPhim = new SelectList(_context.Phims, "MaPhim", "TenPhim", sc.MaPhim);
            ViewBag.MaPhong = new SelectList(_context.PhongChieus, "MaPhong", "TenPhong", sc.MaPhong);
            return View(sc);
        }

        [HttpGet]
        public IActionResult xoa(int? id)
        {
            if (id == null) return NotFound();

            var sc = _context.SuatChieus
               .Include(s => s.MaPhimNavigation)
               .Include(s => s.MaPhongNavigation)
               .FirstOrDefault(m => m.MaSuat == id);

            if (sc == null) return NotFound();

            return View(sc);
        }

        [HttpPost, ActionName("xoa")]
        [ValidateAntiForgeryToken]
        public IActionResult xoa_Post(int id)
        {
            var sc = _context.SuatChieus.Find(id);
            if (sc != null)
            {
                _context.SuatChieus.Remove(sc);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public JsonResult getPhim(int id)
        {
            var phim = _context.Phims.Find(id);
            if (phim == null)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                thoiLuong = phim.ThoiLuong,
                ngayPhatHanh = phim.NgayPhatHanh?.ToString("yyyy-MM-dd'T'HH:mm"),
                ngayKetThuc = phim.NgayKetThuc != null ? phim.NgayKetThuc.Value.ToString("yyyy-MM-dd'T'HH:mm") : null
            });
        }

        private bool kiemTra(int? id)
        {
            return _context.SuatChieus.Any(e => e.MaSuat == id);
        }
    }
}