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
        public IActionResult them(DoAn doAn, IFormFile fDoan)
        {
            // Bỏ qua validate các trường không nhập trực tiếp
            ModelState.Remove("DonDatVeDoAns");
            ModelState.Remove("fDoan");
            ModelState.Remove("HinhAnh");

            if (ModelState.IsValid)
            {
                if (fDoan != null && fDoan.Length > 0)
                {
                    // 1. Tạo tên file độc nhất
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fDoan.FileName);

                    // 2. Xác định thư mục lưu: wwwroot/images/combo
                    string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "combo");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    // 3. Lưu file vật lý
                    string filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fDoan.CopyTo(stream);
                    }

                    // 4. Lưu đường dẫn vào DB (QUAN TRỌNG: Có dấu / ở đầu)
                    doAn.HinhAnh = "/images/combo/" + fileName;
                }

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
            // ID là số nguyên nên check id == 0 thay vì null
            if (id == 0) return NotFound();

            var doAn = _context.DoAns.Find(id);
            if (doAn == null) return NotFound();

            return View(doAn);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult sua(int id, DoAn doAn, IFormFile fDoan)
        {
            ModelState.Remove("DonDatVeDoAns");
            ModelState.Remove("fDoan");
            ModelState.Remove("HinhAnh");

            if (id != doAn.MaCombo) return NotFound();

            if (ModelState.IsValid)
            {
                if (fDoan != null && fDoan.Length > 0)
                {
                    if (!string.IsNullOrEmpty(doAn.HinhAnh))
                    {
                        string relativePath = doAn.HinhAnh.TrimStart('/');

                        // Ghép với đường dẫn gốc của ứng dụng
                        string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                        try
                        {
                            if (System.IO.File.Exists(oldPath))
                            {
                                System.IO.File.Delete(oldPath);
                            }
                        }
                        catch
                        {
                        }
                    }

                    // --- LƯU ẢNH MỚI ---
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fDoan.FileName);
                    string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "combo");

                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    string filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fDoan.CopyTo(stream);
                    }

                    doAn.HinhAnh = "/images/combo/" + fileName;
                }

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
