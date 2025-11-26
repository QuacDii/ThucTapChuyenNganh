using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using TTCN.Models; 

namespace TTCN.Controllers
{
    public class UsersController : AdminBaseController
    {
        private readonly QLDVContext _context;

        public UsersController(QLDVContext context)
        {
            this._context = context;
        }

        public IActionResult Index(string search, string vaiTro)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.HoTen.Contains(search)
                                        || s.Email.Contains(search)
                                        || s.SoDienThoai.Contains(search));
            }

            if (!string.IsNullOrEmpty(vaiTro)) 
            {
                query=query.Where(u=>u.VaiTro==vaiTro);
            }

            ViewBag.CurrentFilter = search;
            ViewBag.CurrentRole = vaiTro;
            ViewBag.RoleList = _context.Users.Select(u => u.VaiTro).Distinct().ToList();

            ViewBag.p = query.ToList();
            return View();
        }

        [HttpGet]
        public ActionResult xoa(int id)
        {
            User us = _context.Users.Find(id);
            ViewBag.p = us;
            return View(us);
        }

        [HttpPost, ActionName("xoa")]
        public ActionResult xoa_Post(int id)
        {
            User us = _context.Users.Find(id);
            if (us != null)
            {
                _context.Users.Remove(us);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult them()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult them(User user)
        {
            if (_context.Users.Any(u => u.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email này đã tồn tại trong hệ thống!");
            }

            if (_context.Users.Any(u => u.SoDienThoai == user.SoDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại này đã được sử dụng!");
            }

            ModelState.Remove("MaUsers");
            ModelState.Remove("NgayTao");

            if (ModelState.IsValid)
            {
                user.NgayTao = DateTime.Now;
                int maxId = _context.Users.Any() ? _context.Users.Max(u => u.MaUsers) : 0;
                user.MaUsers = maxId + 1;

                _context.Users.Add(user);
                _context.SaveChanges();

                TempData["Success"] = $"Đã thêm tài khoản {user.HoTen} thành công!";
                return RedirectToAction("Index");
            }
            return View("them",user);
        }
    }
}
