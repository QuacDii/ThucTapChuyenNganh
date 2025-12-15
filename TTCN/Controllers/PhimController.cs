using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;     
using System.Collections.Generic;      
using System.Linq;
using System.Security.Cryptography;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class PhimController : Controller
    {
        private readonly QLDVContext _context;
        public PhimController(QLDVContext context)
        {
            this._context = context;
        }
        public IActionResult Index(string search, string trangThai, List<int> maTheLoai, DateTime? ngay)
        {
            var query = _context.Phims
                    .Include(p => p.PhimTheLoais)
                    .ThenInclude(pt1 => pt1.MaTheLoaiNavigation)
                    .AsQueryable();

            // Lọc theo Tên phim 
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.TenPhim.Contains(search));
            }

            // Lọc theo Trạng thái
            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(p => p.TrangThai == trangThai);
            }

            //Lọc theo Ngày khởi chiếu
            if (ngay.HasValue)
            {
                query = query.Where(s => s.NgayPhatHanh == ngay.Value.Date);
            }

            // Lọc theo Thể loại 
            if (maTheLoai != null && maTheLoai.Count > 0)
            {
                query = query.Where(p => p.PhimTheLoais.Any(pt => maTheLoai.Contains(pt.MaTheLoai)));
            }

            var result = query.OrderByDescending(x => x.NgayPhatHanh).ToList();
            ViewBag.AllTheLoais = _context.TheLoais.ToList();

            // Gửi lại các giá trị đã tìm để giữ trên giao diện sau khi reload
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = trangThai;
            ViewBag.CurrentDate = ngay?.ToString("yyyy-MM-dd");
            ViewBag.CurrentGenre = maTheLoai;
            return View(result);
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
        public ActionResult them(Phim p, List<int> select, IFormFile fPoster, IFormFile fTrailer)
        {
            ModelState.Remove("MaPhim");
            ModelState.Remove("TrangThai");
            ModelState.Remove("PhimTheLoais");
            ModelState.Remove("SuatChieus");
            ModelState.Remove("fPoster");
            ModelState.Remove("fTrailer");
            ModelState.Remove("PosterPhim");
            ModelState.Remove("TrailerPhim");

            if (p.NgayPhatHanh != default(DateTime) && p.NgayKetThuc != default(DateTime))
            {
                if (p.NgayKetThuc < p.NgayPhatHanh)
                    ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không được nhỏ hơn ngày phát hành!");
            }
            if (_context.Phims.Any(x => x.TenPhim == p.TenPhim))
            {
                ModelState.AddModelError("TenPhim", "Phim này đã tồn tại! Vui lòng chọn tên khác.");
            }

            if (ModelState.IsValid)
            {
                DateTime hnay = DateTime.Now;
                if (p.NgayPhatHanh > hnay)
                    p.TrangThai = "Sắp công chiếu";
                else if (p.NgayKetThuc < hnay)
                    p.TrangThai = "Đã chiếu";
                else p.TrangThai = "Đang công chiếu";

                if (fPoster != null && fPoster.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fPoster.FileName);
                    string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
                    string filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fPoster.CopyTo(stream);
                    }

                    p.PosterPhim = "images/" + fileName;
                }
                else
                {
                    p.PosterPhim = "images/default.png";
                }
                if (fTrailer != null && fTrailer.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fTrailer.FileName);
                    string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "trailers");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);
                    string filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fTrailer.CopyTo(stream);
                    }
                    p.TrailerPhim = "trailers/" + fileName;
                }

                _context.Phims.Add(p);
                _context.SaveChanges();

                int maPhimnew = p.MaPhim;

                if (select != null)
                {
                    foreach (var maTheLoai in select)
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
                TempData["Success"] = "Thêm phim thành công!";
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

            ViewBag.AllTheLoais = _context.TheLoais.ToList();

            ViewBag.Select = p.PhimTheLoais.Select(p1 => p1.MaTheLoai).ToList();

            ViewBag.ph = p;
            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult sua(Phim ph, int id, List<int> select, IFormFile fPoster, IFormFile fTrailer)
        {
            ModelState.Remove("TrangThai");
            ModelState.Remove("PhimTheLoais");
            ModelState.Remove("SuatChieus");
            ModelState.Remove("fPoster");
            ModelState.Remove("fTrailer");
            ModelState.Remove("PosterPhim");
            ModelState.Remove("TrailerPhim");

            if (ph.NgayPhatHanh != default(DateTime) && ph.NgayKetThuc != default(DateTime))
            {
                if (ph.NgayKetThuc < ph.NgayPhatHanh)
                {
                    ModelState.AddModelError("NgayKetThuc", "Ngày kết thúc không được nhỏ hơn ngày phát hành!");
                }
            }
            if (_context.Phims.Any(x => x.TenPhim == ph.TenPhim && x.MaPhim != id))
            {

                ModelState.AddModelError("TenPhim", "Tên phim này đã tồn tại! Vui lòng chọn tên khác.");
            }
            if (ModelState.IsValid)
            {
                Phim p = _context.Phims
                    .Include(ph => ph.PhimTheLoais)
                    .FirstOrDefault(ph1 => ph1.MaPhim == ph.MaPhim);

                if (p == null) return NotFound();

                bool thayDoiThoiLuong = p.ThoiLuong != ph.ThoiLuong;

                p.TenPhim = ph.TenPhim;
                p.MoTa = ph.MoTa;
                p.ThoiLuong = ph.ThoiLuong;
                p.NgayPhatHanh = ph.NgayPhatHanh;
                p.NgayKetThuc = ph.NgayKetThuc;
                p.DaoDien = ph.DaoDien;
                p.DienVien = ph.DienVien;

                if (thayDoiThoiLuong)
                {
                    // Tìm tất cả các suất chiếu của phim này
                    var danhSachSuatChieu = _context.SuatChieus
                                                    .Where(s => s.MaPhim == p.MaPhim && s.GioBatDau != null)
                                                    .ToList();

                    foreach (var suat in danhSachSuatChieu)
                    {
                        // Tính lại Giờ Kết Thúc = Giờ Bắt Đầu + Thời Lượng Mới
                        suat.GioKetThuc = suat.GioBatDau.Value.AddMinutes(p.ThoiLuong);
                    }
                }

                if (fPoster != null && fPoster.Length > 0)
                {
                    if (!string.IsNullOrEmpty(p.PosterPhim))
                    {
                        string relativePath = p.PosterPhim.TrimStart('/');
                        string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                        try
                        {
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }
                        catch { /* Bỏ qua lỗi nếu file đang bị khóa */ }
                    }
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fPoster.FileName);
                    string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    string filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fPoster.CopyTo(stream);
                    }
                    p.PosterPhim = "images/" + fileName;
                }

                if (fTrailer != null && fTrailer.Length > 0)
                {
                    if (!string.IsNullOrEmpty(p.TrailerPhim))
                    {
                        string relativePath = p.TrailerPhim.TrimStart('/');
                        string oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                        try
                        {
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }
                        catch { }
                    }
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(fTrailer.FileName);
                    string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "trailers");
                    if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                    string filePath = Path.Combine(uploadDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        fTrailer.CopyTo(stream);
                    }

                    p.TrailerPhim = "trailers/" + fileName;
                }

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
                if (select != null)
                {
                    select = select.Distinct().ToList();
                    foreach (var maTheLoai in select)
                    {
                        _context.PhimTheLoais.Add(new PhimTheLoai
                        {
                            MaPhim = p.MaPhim,
                            MaTheLoai = maTheLoai
                        });
                    }
                }
                TempData["Success"] = "Cập nhật phim và đồng bộ lịch chiếu thành công!";
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
            bool coSuatChieu = _context.SuatChieus.Any(s => s.MaPhim == id);

            if (coSuatChieu)
            {
                // Nếu có suất chiếu -> Báo lỗi qua TempData để hiển thị ở trang Index
                TempData["Error"] = "Không thể xóa phim này vì đã có lịch chiếu!";
                return RedirectToAction("Index");
            }

            // 2. Nếu không có suất chiếu -> Tiến hành xóa
            Phim ph = _context.Phims
                           .Include(p => p.PhimTheLoais)
                           .FirstOrDefault(p => p.MaPhim == id);

            if (ph != null)
            {
                // Xóa các dòng trong bảng trung gian trước
                if (ph.PhimTheLoais != null)
                {
                    _context.PhimTheLoais.RemoveRange(ph.PhimTheLoais);
                }

                if (!string.IsNullOrEmpty(ph.PosterPhim))
                {
                    string relativePath = ph.PosterPhim.TrimStart('/');
                    string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);

                    if (System.IO.File.Exists(absolutePath))
                    {
                        try
                        {
                            System.IO.File.Delete(absolutePath);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                if (!string.IsNullOrEmpty(ph.TrailerPhim))
                {
                    string rP = ph.TrailerPhim.TrimStart('/');
                    string aP = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", rP);

                    if (System.IO.File.Exists(aP))
                    {
                        try
                        {
                            System.IO.File.Delete(aP);
                        }
                        catch (Exception)
                        {
                        }
                    }

                }
                _context.Phims.Remove(ph);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa phim!";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetSearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return Json(new object[] { }); // Trả về mảng rỗng nếu không có từ khóa
            }

            var ketQua = _context.Phims
                .Where(p => p.TenPhim.Contains(term))
                .Select(p => new
                {
                    id = p.MaPhim,
                    label = p.TenPhim,
                    image = p.PosterPhim
                })
                .Take(5) // Chỉ lấy 5 kết quả đầu tiên
                .ToList();

            return Json(ketQua);
        }

        [HttpGet]
        public IActionResult TimKiem(string tuKhoa)
        {
            // 1. Tạo query cơ bản lấy phim đang hoạt động
            var query = _context.Phims.AsQueryable();

            // 2. Nếu có từ khóa -> Lọc theo tên phim
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                query = query.Where(p => p.TenPhim.Contains(tuKhoa));
            }

            // 3. Lấy danh sách kết quả
            var ketQua = query.ToList();

            var dictDiem = _context.UsersPhims
                .Where(u => u.Diem != null && u.TrangThai == false) 
                .GroupBy(g => g.MaPhim)
                .Select(g => new {
                    MaPhim = g.Key,
                    DiemTB = (int)g.Average(u => u.Diem)
                })
                .ToDictionary(k => k.MaPhim, v => v.DiemTB);

            ViewBag.DiemDanhGia = dictDiem;
            ViewBag.TuKhoa = tuKhoa;

            return View(ketQua);
        }

        [HttpGet]
        public IActionResult chiTiet(int id)
        {
            var phim = _context.Phims
                .Include(p => p.PhimTheLoais)
                .ThenInclude(pt => pt.MaTheLoaiNavigation)
                .FirstOrDefault(p => p.MaPhim == id);

            if (phim == null) return NotFound();

            // 2. Lấy danh sách đánh giá
            var query = _context.UsersPhims
                .Include(up => up.MaUsersNavigation)
                .Where(up => up.MaPhim == id && (up.BinhLuan != null || up.Diem != null));

            // 3. Logic Phân Quyền: Nếu KHÔNG PHẢI ADMIN thì lọc bỏ comment bị ẩn
            var userRole = HttpContext.Session.GetString("UserVaiTro");
            if (userRole != "Admin")
            {
                query = query.Where(up => up.TrangThai == false);
            }

            var danhGias = query.ToList();

            // 4. Đổ dữ liệu vào Model 
            var model = new TTCN.Models.ctPhim
            {
                Phim = phim,
                TenTheLoais = phim.PhimTheLoais.Select(pt => pt.MaTheLoaiNavigation.TenTheLoai).ToList(),

                // Gán trực tiếp danh sách
                DanhSachDanhGia = danhGias
            };

            // 5. Tính điểm trung bình
            // Kể cả Admin đang xem thì điểm số vẫn phải tính trên cái mà khách hàng thấy
            var commentHopLe = _context.UsersPhims
                .Where(x => x.MaPhim == id && x.Diem != null && x.TrangThai == false)
                .ToList();

            if (commentHopLe.Any())
            {
                model.DiemTrungBinh = (int)commentHopLe.Average(x => x.Diem.Value);
            }
            else
            {
                model.DiemTrungBinh = 0;
            }
            return View(model);
        }

        [HttpPost]
        public IActionResult ThemDanhGia(int MaPhim, int SoSao, string NoiDung)
        {
            // 1. Kiểm tra đăng nhập (Lấy ID user từ Session)
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            // Lấy User ID từ Email
            var user = _context.Users.FirstOrDefault(u => u.Email == userEmail);
            if (user == null) return RedirectToAction("Index", "Home");

            var phim = _context.Phims.FirstOrDefault(p => p.MaPhim == MaPhim);

            if (phim == null || phim.TrangThai == "Sắp công chiếu")
            {
                TempData["Error"] = "Phim này chưa được phép đánh giá!";
                return RedirectToAction("chiTiet", new { id = MaPhim });
            }
            // 2. Lưu đánh giá vào Database
            var danhGiaCu = _context.UsersPhims
         .FirstOrDefault(x => x.MaPhim == MaPhim && x.MaUsers == user.MaUsers);

            if (danhGiaCu != null)
            {
                danhGiaCu.Diem = SoSao * 2;
                danhGiaCu.BinhLuan = NoiDung;
                danhGiaCu.NgayBL = DateTime.Now;
                // Mở lại bình luận nếu trước đó bị ẩn 
                danhGiaCu.TrangThai = false;

            }
            else
            {
                // TRƯỜNG HỢP CHƯA CÓ: Thêm mới hoàn toàn
                var danhGiaMoi = new UsersPhim
                {
                    MaPhim = MaPhim,
                    MaUsers = user.MaUsers,
                    Diem = SoSao * 2,
                    BinhLuan = NoiDung,
                    NgayBL = DateTime.Now,
                    TrangThai = false
                };
                _context.UsersPhims.Add(danhGiaMoi);
            }

            _context.SaveChanges(); // Lưu thay đổi (dù là Sửa hay Thêm)

            return RedirectToAction("chiTiet", new { id = MaPhim });
        }

        [HttpPost]
        public IActionResult anBL(int maUser, int maPhim)
        {
            // 1. Kiểm tra quyền Admin
            if (HttpContext.Session.GetString("UserVaiTro") != "Admin")
                return RedirectToAction("chiTiet", new { id = maPhim });

            //2.Tìm bình luận
            var comment = _context.UsersPhims.FirstOrDefault(x => x.MaPhim == maPhim && x.MaUsers == maUser);

            if(comment != null)
            {
                bool isHidden = comment.TrangThai;
                comment.TrangThai = !isHidden;

                _context.SaveChanges();
            }
            return RedirectToAction("chiTiet", "Phim", new { id = maPhim }, "reviewContainer");
        }
    }
}


