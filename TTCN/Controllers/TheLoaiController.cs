using Microsoft.AspNetCore.Mvc;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class TheLoaiController : Controller
    {
        private readonly QLDVContext _context;

        public TheLoaiController(QLDVContext context)
        {
            _context = context;
        }

        public IActionResult Index(string search)
        {
            var query = _context.TheLoais.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.TenTheLoai.Contains(search));
                ViewBag.CurrentFilter = search;
            }
            return View(query.ToList());
        }

        [HttpGet]
        public IActionResult them()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult them(TheLoai tl)
        {
            ModelState.Remove("MaTheLoai");
            ModelState.Remove("PhimTheLoais");

            if (_context.TheLoais.Any(t => t.TenTheLoai == tl.TenTheLoai))
            {
                ModelState.AddModelError("TenTheLoai", "Thể loại này đã tồn tại!");
                return View(tl);
            }

            if (ModelState.IsValid)
            {
                _context.TheLoais.Add(tl);
                _context.SaveChanges();
                TempData["Success"] = "Thêm thể loại thành công!";
                return RedirectToAction("Index");
            }
            return View(tl);
        }

        [HttpGet]
        public IActionResult sua(int id)
        {
            if (id == null) return NotFound();
            var tl = _context.TheLoais.Find(id);
            if (tl == null) return NotFound();
            return View(tl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult sua(int id, TheLoai theLoai)
        {
            if (id != theLoai.MaTheLoai) return NotFound();

            if (ModelState.IsValid)
            {
                if (_context.TheLoais.Any(x => x.TenTheLoai == theLoai.TenTheLoai && x.MaTheLoai != id))
                {
                    ModelState.AddModelError("TenTheLoai", "Thể loại này đã tồn tại!");
                    return View(theLoai);
                }

                _context.TheLoais.Update(theLoai);
                _context.SaveChanges(); 
                TempData["Success"] = "Cập nhật thể loại thành công!";
                return RedirectToAction("Index");
            }
            return View(theLoai);
        }

        [HttpGet]
        public IActionResult xoa(int id)
        {
            if (id == null) return NotFound();
            var theLoai = _context.TheLoais.Find(id);
            if (theLoai == null) return NotFound();
            return View(theLoai);
        }

        [HttpPost, ActionName("xoa")]
        [ValidateAntiForgeryToken]
        public IActionResult xoa_Post(int id)
        {
            var theLoai = _context.TheLoais.Find(id);
            if (theLoai != null)
            {
                // Kiểm tra ràng buộc: Nếu thể loại đang được dùng cho Phim thì không cho xóa
                if (_context.PhimTheLoais.Any(x => x.MaTheLoai == id))
                {
                    TempData["Error"] = "Không thể xóa! Thể loại này đang được gắn cho phim.";
                    return RedirectToAction("Index");
                }

                _context.TheLoais.Remove(theLoai);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa thể loại!";
            }
            return RedirectToAction("Index");
        }
    }
}
