using Microsoft.AspNetCore.Mvc;
using System;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class DoAnController : Controller
    {
        private readonly QLDVContext _context;

        public DoAnController(QLDVContext context)
        {
            _context = context;
        }
        public IActionResult Index(string search, bool? trangThai, decimal? min, decimal? max)
        {
            var query = _context.DoAns.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => d.MoTa.Contains(search));
                ViewBag.CurrentSearch = search;
            }
            if (min.HasValue)
            {
                query = query.Where(s => s.Gia >= min.Value);
            }
            if (max.HasValue)
            {
                query = query.Where(s => s.Gia <= max.Value);
            }
            if (trangThai.HasValue)
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }


            var result = query.OrderByDescending(d => d.TrangThai).ToList();

            ViewBag.CurrentStatus = trangThai;
            ViewBag.CurrentMinPrice = min;
            ViewBag.CurrentMaxPrice = max;
            ViewBag.CurrentSearch = search;
            return View(result);
        }

        [HttpGet]
        public IActionResult them()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult them(DoAn doAn)
        {
            if (ModelState.IsValid)
            {
                int maxId = _context.DoAns.Any() ? _context.DoAns.Max(s => s.MaCombo) : 0;
                doAn.MaCombo = maxId + 1;
                _context.DoAns.Add(doAn);
                _context.SaveChanges();
                TempData["Success"] = "Thêm Combo thành công!";
                return RedirectToAction("Index");
            }
            return View(doAn);
        }

        [HttpGet]
        public IActionResult sua(int id)
        {
            if (id == null) return NotFound();

            var doAn = _context.DoAns.Find(id);
            if (doAn == null) return NotFound();

            return View(doAn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult sua(int id, DoAn doAn)
        {
            if (id != doAn.MaCombo) return NotFound();

            if (ModelState.IsValid)
            {
                _context.DoAns.Update(doAn);
                _context.SaveChanges();
                TempData["Success"] = "Cập nhật Combo thành công!";
                return RedirectToAction("Index");
            }
            return View(doAn);
        }

        [HttpGet]
        public IActionResult Restore(int id)
        {
            var doAn = _context.DoAns.Find(id);
            if (doAn != null)
            {
                doAn.TrangThai = true;
                _context.SaveChanges();
                TempData["Success"] = "Đã mở bán lại món này!";
            }
            return RedirectToAction("Index");
        }
    }
}
