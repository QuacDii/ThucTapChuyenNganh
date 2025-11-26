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

        public IActionResult Index()
        {
            var ds = _context.TheLoais.ToList();
            return View(ds);
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
                ModelState.AddModelError("TenTheLoai", "Tên thể loại này đã tồn tại!");
            }

            if (ModelState.IsValid)
            {
                int max = _context.TheLoais.Any() ? _context.TheLoais.Max(t => t.MaTheLoai) : 0;
                tl.MaTheLoai = max + 1;

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

        //[HttpPost]
        //[ValidateAntiForgeryToken]
 
    }
}
