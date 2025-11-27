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

        public IActionResult Index(string search, DateTime? ngay, string trangThai, decimal? min, decimal? max)
        {
            var query = _context.SuatChieus
                            .Include(s => s.MaPhimNavigation)
                            .Include(s => s.MaPhongNavigation)
                            .ThenInclude(p => p.MaCumRapNavigation)
                            .AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.MaPhimNavigation.TenPhim.Contains(search));
            }

            // 3. Lọc theo Ngày Chiếu
            if (ngay.HasValue)
            {
                query = query.Where(s => s.GioBatDau.HasValue && s.GioBatDau.Value.Date == ngay.Value.Date);
            }

            // 4. Lọc theo Khoảng Giá Vé
            if (min.HasValue)
            {
                query = query.Where(s => s.Gia >= min.Value);
            }
            if (max.HasValue)
            {
                query = query.Where(s => s.Gia <= max.Value);
            }

            // 5. Lọc theo Trạng Thái
            var now = DateTime.Now;
            if (!string.IsNullOrEmpty(trangThai))
            {
                switch (trangThai)
                {
                    case "sap_chieu": // Giờ bắt đầu > hiện tại
                        query = query.Where(s => s.GioBatDau > now);
                        break;
                    case "dang_chieu": // Bắt đầu <= hiện tại <= Kết thúc
                        query = query.Where(s => s.GioBatDau <= now && s.GioKetThuc >= now);
                        break;
                    case "da_chieu": // Kết thúc < hiện tại
                        query = query.Where(s => s.GioKetThuc < now);
                        break;
                }
            }

            // 6. Sắp xếp: Ưu tiên suất chiếu mới nhất lên đầu
            var result = query.OrderByDescending(s => s.GioBatDau).ToList();

            // 7. Lưu lại giá trị filter để hiển thị lại trên View
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = trangThai;
            ViewBag.CurrentDate = ngay?.ToString("yyyy-MM-dd"); 
            ViewBag.CurrentMinPrice = min;
            ViewBag.CurrentMaxPrice = max;
            return View(result);
        }

        [HttpGet]
        public IActionResult them()
        {
            ViewBag.MaPhim = new SelectList(_context.Phims, "MaPhim", "TenPhim");
            ViewBag.ListCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap");
            ViewBag.MaPhong = new SelectList(new List<PhongChieu>(), "MaPhong", "TenPhong");
            return View();
        }

        [HttpGet]
        public JsonResult GetPhongByCumRap(int id)
        {
            var listPhong = _context.PhongChieus
                                    .Where(p => p.MaCumRap == id)
                                    .Select(p => new {
                                        maPhong = p.MaPhong,
                                        tenPhong = p.TenPhong
                                    })
                                    .ToList();
            return Json(listPhong);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult them(SuatChieu sc)
        {
            ModelState.Remove("MaPhimNavigation");
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("DonDatVes");
            ModelState.Remove("ChiTietScGn");

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
            ViewBag.ListCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap"); 
            ViewBag.MaPhong = new SelectList(_context.PhongChieus, "MaPhong", "TenPhong", sc.MaPhong); 
            return View(sc);
        }

        [HttpGet]
        public IActionResult sua(int id)
        {
            var sc = _context.SuatChieus
                .Include(s => s.MaPhongNavigation)          // Lấy thông tin Phòng
                .ThenInclude(p => p.MaCumRapNavigation) // Lấy tiếp thông tin Cụm Rạp từ Phòng
                .FirstOrDefault(s => s.MaSuat == id);

            if (sc == null) return NotFound();

            ViewBag.MaPhim = new SelectList(_context.Phims, "MaPhim", "TenPhim", sc.MaPhim);
            var currentCumRapId = sc.MaPhongNavigation.MaCumRap;
            var listPhong = _context.PhongChieus
                                    .Where(p => p.MaCumRap == currentCumRapId)
                                    .ToList();

            ViewBag.MaPhong = new SelectList(listPhong, "MaPhong", "TenPhong", sc.MaPhong);

            var roomMap = listPhong.ToDictionary(
                k => k.MaPhong,
                v => v.MaCumRapNavigation?.TenCumRap ?? sc.MaPhongNavigation.MaCumRapNavigation.TenCumRap
            );
            ViewBag.RoomCinemaJson = System.Text.Json.JsonSerializer.Serialize(roomMap);

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
            ModelState.Remove("ChiTietScGn");

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

            bool ktr=_context.SuatChieus.Any(e=>e.MaPhim==sc.MaPhim
                                             && e.MaPhong==sc.MaPhong
                                             && e.GioBatDau==sc.GioBatDau
                                             && e.GioKetThuc==sc.GioKetThuc
                                             && e.MaSuat != id);

            if (ktr)
            {
                ModelState.AddModelError("","Suất chiếu này đã tồn tại trong hệ thống!");
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

            // Tìm lại phòng hiện tại để biết nó thuộc rạp nào
            var currentPhong = _context.PhongChieus.Include(p => p.MaCumRapNavigation).FirstOrDefault(p => p.MaPhong == sc.MaPhong);
            if (currentPhong != null)
            {
                var listPhong = _context.PhongChieus.Where(p => p.MaCumRap == currentPhong.MaCumRap).ToList();
                ViewBag.MaPhong = new SelectList(listPhong, "MaPhong", "TenPhong", sc.MaPhong);

                var roomMap = listPhong.ToDictionary(k => k.MaPhong, v => currentPhong.MaCumRapNavigation.TenCumRap);
                ViewBag.RoomCinemaJson = System.Text.Json.JsonSerializer.Serialize(roomMap);
            }

            return View(sc);
        }

        [HttpGet]
        public IActionResult xoa(int? id)
        {
            if (id == null) return NotFound();

            var sc = _context.SuatChieus
               .Include(s => s.MaPhimNavigation)
               .Include(s => s.MaPhongNavigation)
               .ThenInclude(p => p.MaCumRapNavigation)
               .FirstOrDefault(m => m.MaSuat == id);

            if (sc == null) return NotFound();

            return View(sc);
        }

        [HttpPost, ActionName("xoa")]
        [ValidateAntiForgeryToken]
        public IActionResult xoa_Post(int id)
        {
            bool coDonDat = _context.DonDatVes.Any(s => s.MaDon == id);

            if (coDonDat)
            {
                // Nếu có suất chiếu -> Báo lỗi qua TempData để hiển thị ở trang Index
                TempData["Error"] = "Không thể xóa Suất chiếu này vì đã có đơn đặt vé!";
                return RedirectToAction("Index");
            }

            var sc = _context.SuatChieus.Find(id);
            if (sc != null)
            {
                _context.SuatChieus.Remove(sc);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
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