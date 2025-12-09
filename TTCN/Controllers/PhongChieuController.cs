using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class PhongChieuController : Controller
    {

        private readonly QLDVContext _context;
        public PhongChieuController(QLDVContext context)
        {
            _context = context;
        }
        public IActionResult Index(string search, int maCum)
        {
            var query = _context.PhongChieus.Include(p => p.MaCumRapNavigation).AsQueryable();

            // 1. Tìm theo tên
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.TenPhong.Contains(search));
                ViewBag.CurrentSearch = search;
            }

            // 2. Lọc theo cụm rạp
            if (maCum > 0)
            {
                query = query.Where(p => p.MaCumRap == maCum);
            }

            ViewBag.dsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap", maCum);

            var result = query.OrderByDescending(p => p.MaPhong).ToList();

            return View(result);
        }

        [HttpGet]
        public IActionResult them()
        {
            ViewBag.dsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult them(PhongChieu p)
        {
            ModelState.Remove("MaCumRapNavigation");
            ModelState.Remove("GheNgois");
            ModelState.Remove("SuatChieus");

            if (ModelState.IsValid)
            { 
                int maxId = _context.SuatChieus.Any() ? _context.PhongChieus.Max(s => s.MaPhong) : 0;
                p.MaPhong = maxId + 1;
                _context.PhongChieus.Add(p);
                _context.SaveChanges();
                TempData["Success"] = "Thêm phòng chiếu thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.dsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap", p.MaCumRap);
            return View(p);
        }

        [HttpGet]
        public IActionResult sua(int id)
        {
            if (id == 0) return NotFound();
            var pc = _context.PhongChieus.Find(id);
            if (pc == null) return NotFound();

            ViewBag.dsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap", pc.MaCumRap);
            return View(pc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult sua(int id, PhongChieu pc)
        {
            ModelState.Remove("MaCumRapNavigation");
            ModelState.Remove("GheNgois");
            ModelState.Remove("SuatChieus");

            bool ktr=_context.PhongChieus.Any(p=>p.TenPhong==pc.TenPhong
                                              && p.TongGhe==pc.TongGhe
                                              && p.MaCumRap==pc.MaCumRap
                                              && p.MaPhong != id);
            if (ktr)
            {
                ModelState.AddModelError("", "Phòng chiếu này đã tồn tại trong hệ thống!");
            }

            if (ModelState.IsValid) 
            {
                _context.PhongChieus.Update(pc);
                _context.SaveChanges();
                TempData["Success"] = "Cập nhật phòng chiếu thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.dsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap", pc.MaCumRap);
            return View(pc);
        }

        [HttpGet]
        public IActionResult xoa(int id)
        {
            if (id == null) return NotFound();

            var phongChieu = _context.PhongChieus
                .Include(p => p.MaCumRapNavigation)
                .FirstOrDefault(m => m.MaPhong == id);

            if (phongChieu == null) return NotFound();

            return View(phongChieu);
        }

        [HttpPost, ActionName("xoa")]
        [ValidateAntiForgeryToken]
        public IActionResult xoa_Post(int id)
        {
            var p = _context.PhongChieus.Find(id);

            if(p!=null)
            {
                bool coSuatChieu = _context.SuatChieus.Any(sc => sc.MaPhong == id);
                if (coSuatChieu)
                {
                    TempData["Error"] = "Không thể xóa phòng này vì đang có Lịch Chiếu hoặc Vé đã bán!";
                    return RedirectToAction("Index");
                }
                var danhSachGhe = _context.GheNgois.Where(g => g.MaPhong == id).ToList();

                if (danhSachGhe.Count > 0)
                {
                    _context.GheNgois.RemoveRange(danhSachGhe);
                }
                _context.PhongChieus.Remove(p);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa phòng chiếu và toàn bộ ghế bên trong!";
            }
            return RedirectToAction("Index");
        }
    }
}
