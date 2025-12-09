using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Eventing.Reader;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class GheNgoiController : Controller
    {
        private readonly QLDVContext _context;

        public GheNgoiController(QLDVContext context)
        {
            _context = context;
        }

        public IActionResult Index(int maCumRap, int maPhong, string search)
        {
            var query = _context.GheNgois
                                .Include(g => g.MaPhongNavigation)
                                .ThenInclude(p => p.MaCumRapNavigation)
                                .AsQueryable();

            // Lọc theo Cụm Rạp
            if (maCumRap > 0)
            {
                query = query.Where(g => g.MaPhongNavigation.MaCumRap == maCumRap);
            }

            // Lọc theo Phòng
            if (maPhong > 0)
            {
                query = query.Where(g => g.MaPhong == maPhong);
            }

            // Tìm kiếm tên ghế
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(g => g.TenGhe.Contains(search) || g.MaGhe.ToString()==search);
            }


            // 1. Danh sách Cụm Rạp 
            ViewBag.DsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap", maCumRap);

            // 2. Danh sách Phòng (Lọc theo Cụm rạp nếu đã chọn)
            var listPhong = _context.PhongChieus.AsQueryable();
            if (maCumRap > 0)
            {
                listPhong = listPhong.Where(p => p.MaCumRap == maCumRap);
            }
            ViewBag.DsPhong = new SelectList(listPhong, "MaPhong", "TenPhong", maPhong);

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentCumRap = maCumRap;

            var result = query.OrderBy(g => g.MaPhong).ThenBy(g => g.TenGhe).ToList();
            return View(result);
        }

        [HttpGet]
        public IActionResult GetPhongByCumRap(int cumRapId)
        {
            var phongs = _context.PhongChieus
                                 .Where(p => p.MaCumRap == cumRapId)
                                 .Select(p => new { id = p.MaPhong, name = p.TenPhong })
                                 .ToList();
            return Json(phongs);
        }
        private List<string> GetDanhSachHangGhe()
        {
            var list = new List<string>();
            for (char c = 'A'; c <= 'Z'; c++)
            {
                list.Add(c.ToString());
            }
            return list;
        }

        private List<string> GetDanhSachLoaiGhe()
        {
            return new List<string> { "Thường", "VIP", "Sweetbox" };
        }


        [HttpGet]
        public IActionResult GetThongTinGhe(int maPhong)
        {
            var phong = _context.PhongChieus.FirstOrDefault(p => p.MaPhong == maPhong);
            if (phong == null) return NotFound();

            // Đếm số ghế đang có trong database của phòng này
            int daCo = _context.GheNgois.Count(g => g.MaPhong == maPhong);

            // Tính số ghế còn lại
            int conLai = phong.TongGhe - daCo;

            return Json(new
            {
                success = true,
                tenPhong = phong.TenPhong,
                tongGhe = phong.TongGhe,
                daCo = daCo,
                conLai = conLai
            });
        }

        [HttpGet]
        public IActionResult CheckGheTrung(int maPhong, string tuHang, string denHang, int tuSo, int denSo)
        {
            if (string.IsNullOrEmpty(tuHang) || string.IsNullOrEmpty(denHang) || tuSo > denSo)
            {
                return Json(new { valid = false });
            }

            char startRow = char.Parse(tuHang);
            char endRow = char.Parse(denHang);

            int totalRequest = 0;
            int duplicateCount = 0;

            // Quét qua các ghế người dùng định tạo
            for (char r = startRow; r <= endRow; r++)
            {
                for (int i = tuSo; i <= denSo; i++)
                {
                    totalRequest++;
                    string tenGhe = r.ToString() + i;

                    // Kiểm tra trong DB
                    bool exists = _context.GheNgois.Any(g => g.MaPhong == maPhong && g.TenGhe == tenGhe);
                    if (exists) duplicateCount++;
                }
            }

            int newSeats = totalRequest - duplicateCount;

            return Json(new
            {
                valid = true,
                total = totalRequest,
                duplicate = duplicateCount,
                validToAdd = newSeats,
                message = duplicateCount > 0
                          ? $"Cảnh báo: Có {duplicateCount} ghế đã tồn tại. Hệ thống sẽ chỉ thêm {newSeats} ghế mới."
                          : "Hợp lệ: Tất cả ghế đều chưa tồn tại."
            });
        }

        [HttpGet]
        public IActionResult them()

        {
            ViewBag.MaPhong = new SelectList(_context.PhongChieus, "MaPhong", "TenPhong");
            ViewBag.HangGheList = new SelectList(GetDanhSachHangGhe());
            ViewBag.LoaiGheList = new SelectList(GetDanhSachLoaiGhe());
            ViewBag.DsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult them(GheNgoi gheNgoi)
        {
            ModelState.Remove("MaPhongNavigation");
            ModelState.Remove("ChiTietScGn");

            if (ModelState.IsValid)
            {
                var phong = _context.PhongChieus.FirstOrDefault(p => p.MaPhong == gheNgoi.MaPhong);

                // 2. Đếm số ghế hiện có trong phòng này
                int soGheHienTai = _context.GheNgois.Count(g => g.MaPhong == gheNgoi.MaPhong);

                // 3. KIỂM TRA: Nếu đã full ghế thì báo lỗi
                if (soGheHienTai >= phong.TongGhe)
                {
                    ModelState.AddModelError(string.Empty, $"Không thể thêm! Phòng {phong.TenPhong} đã đủ {phong.TongGhe} ghế.");
                }
                else if (_context.GheNgois.Any(g => g.MaPhong == gheNgoi.MaPhong && g.TenGhe == gheNgoi.TenGhe))
                {
                    // Kiểm tra trùng tên ghế
                    ModelState.AddModelError("TenGhe", $"Ghế {gheNgoi.TenGhe} đã tồn tại!");
                }
                else
                {
                    int maxId = _context.GheNgois.Any() ? _context.GheNgois.Max(s => s.MaGhe) : 0;
                    gheNgoi.MaGhe = maxId + 1;
                    _context.GheNgois.Add(gheNgoi);
                    _context.SaveChanges();
                    TempData["Success"] = "Thêm ghế thành công!";
                    return RedirectToAction("Index");
                }
            }
            // Load lại dropdown nếu lỗi
            ViewBag.DsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap");
            ViewBag.MaPhong = new SelectList(_context.PhongChieus, "MaPhong", "TenPhong", gheNgoi.MaPhong);
            ViewBag.HangGheList = new SelectList(GetDanhSachHangGhe(), gheNgoi.HangGhe);
            ViewBag.LoaiGheList = new SelectList(GetDanhSachLoaiGhe(), gheNgoi.LoaiGhe);
            return View(gheNgoi);
        }

        [HttpGet]
        public IActionResult themNhieu()
        {
            ViewBag.DsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap");
            ViewBag.MaPhong = new SelectList(_context.PhongChieus, "MaPhong", "TenPhong");
            ViewBag.HangGheList = new SelectList(GetDanhSachHangGhe());
            ViewBag.LoaiGheList = new SelectList(GetDanhSachLoaiGhe());
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult themNhieu(int MaPhong, string TuHang, string DenHang, int TuSo, int DenSo, string LoaiGhe)
        {
            // Validate Logic nhập liệu
            char startRow = char.Parse(TuHang);
            char endRow = char.Parse(DenHang);
            int soHang = endRow - startRow + 1;
            int soGheMoiHang = DenSo - TuSo + 1;
            int soGheMuonThem = soHang * soGheMoiHang;

            if (startRow > endRow)
                ModelState.AddModelError("", "Hàng bắt đầu phải nhỏ hơn hoặc bằng hàng kết thúc.");

            if (TuSo > DenSo)
                ModelState.AddModelError("", "Số ghế bắt đầu phải nhỏ hơn hoặc bằng số kết thúc.");



            if (ModelState.IsValid)
            {
                var phong = _context.PhongChieus.FirstOrDefault(p => p.MaPhong == MaPhong);
                int soGheHienTai = _context.GheNgois.Count(g => g.MaPhong == MaPhong);
                int soGheConLai = phong.TongGhe - soGheHienTai;
                if (soGheMuonThem > soGheConLai)
                {
                    ModelState.AddModelError(string.Empty, "Phòng không đủ chỗ trống để thêm");
                }
                else
                {
                    int successCount = 0;   // Đếm số ghế thêm thành công
                    int duplicateCount = 0; // Đếm số ghế bị trùng
                    // Vòng lặp Hàng (Ví dụ A -> C)
                    for (char r = startRow; r <= endRow; r++)
                    {
                        string currentHang = r.ToString();

                        // Vòng lặp Số (Ví dụ 1 -> 10)
                        for (int i = TuSo; i <= DenSo; i++)
                        {
                            string tenGhe = currentHang + i; // A1, A2...

                            // Kiểm tra chưa có mới thêm
                            if (!_context.GheNgois.Any(g => g.MaPhong == MaPhong && g.TenGhe == tenGhe))
                            {
                                int maxId = _context.GheNgois.Any() ? _context.GheNgois.Max(s => s.MaGhe) : 0;
                                var ghe = new GheNgoi
                                {
                                    MaGhe = maxId + 1,
                                    TenGhe = tenGhe,
                                    HangGhe = currentHang,
                                    MaPhong = MaPhong,
                                    LoaiGhe = LoaiGhe
                                };
                                _context.GheNgois.Add(ghe);
                                _context.SaveChanges();
                                successCount++;
                            }
                            else
                            {
                                duplicateCount++;
                            }
                        }
                    }

                    if (successCount > 0)
                    {
                        _context.SaveChanges();

                        string msg = $"Đã thêm thành công {successCount} ghế {LoaiGhe}.";
                        if (duplicateCount > 0)
                        {
                            msg += $" (Hệ thống đã tự động bỏ qua {duplicateCount} ghế bị trùng tên).";
                        }
                        TempData["Success"] = msg;
                    }
                    else
                    {
                        // Trường hợp không thêm được cái nào (Toàn bộ đều trùng)
                        TempData["Error"] = $"Không thêm được ghế nào! Tất cả {duplicateCount} ghế trong phạm vi chọn đều đã tồn tại.";
                    }
                    return RedirectToAction("Index");
                }
            }
            ViewBag.DsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap");
            ViewBag.MaPhong = new SelectList(_context.PhongChieus, "MaPhong", "TenPhong", MaPhong);
            ViewBag.HangGheList = new SelectList(GetDanhSachHangGhe());
            ViewBag.LoaiGheList = new SelectList(GetDanhSachLoaiGhe());
            return View();
        }

        [HttpGet]
        public IActionResult xoa(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var gheNgoi = _context.GheNgois
                .Include(g => g.MaPhongNavigation)
                .ThenInclude(p => p.MaCumRapNavigation)
                .FirstOrDefault(m => m.MaGhe == id);

            return View(gheNgoi);
        }

        [HttpPost, ActionName("xoa")]
        [ValidateAntiForgeryToken]
        public IActionResult xoa_Post(int id)
        {
            // Tìm ghế cần xóa
            var gheNgoi = _context.GheNgois.Find(id);

            if (gheNgoi != null)
            {
                // --- KIỂM TRA RÀNG BUỘC DỮ LIỆU ---
                // Kiểm tra xem ghế này có đang nằm trong bảng ChiTietScGn (Vé đã bán) không?
                bool daCoVe = _context.ChiTietScGns.Any(ct => ct.MaGhe == id);

                if (daCoVe)
                {
                    // Nếu có vé -> Không xóa, báo lỗi và quay về trang danh sách
                    TempData["Error"] = $"Không thể xóa ghế {gheNgoi.TenGhe} vì đã có dữ liệu vé bán!";
                    return RedirectToAction("Index");
                }

                // Nếu sạch sẽ -> Tiến hành xóa
                _context.GheNgois.Remove(gheNgoi);
                _context.SaveChanges();
                TempData["Success"] = "Đã xóa ghế thành công!";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult xoaNhieu()
        {
            // Chuẩn bị dữ liệu cho Dropdown
            ViewBag.DsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap");
            ViewBag.MaPhong = new SelectList(new List<PhongChieu>(), "MaPhong", "TenPhong"); // Để trống chờ chọn Rạp
            ViewBag.HangGheList = new SelectList(GetDanhSachHangGhe()); // List A-Z

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult xoaNhieu(int MaPhong, string TuHang, string DenHang, int TuSo, int DenSo)
        {
            // Validate logic nhập liệu
            char startRow = char.Parse(TuHang);
            char endRow = char.Parse(DenHang);

            if (startRow > endRow) ModelState.AddModelError("", "Hàng bắt đầu phải nhỏ hơn hoặc bằng hàng kết thúc.");
            if (TuSo > DenSo) ModelState.AddModelError("", "Số ghế bắt đầu phải nhỏ hơn hoặc bằng số kết thúc.");

            if (ModelState.IsValid)
            {
                int deletedCount = 0; // Số ghế xóa thành công
                int skipCount = 0;    // Số ghế không xóa được (do có vé hoặc không tồn tại)
                int notFoundCount = 0;// Số ghế không tìm thấy

                for (char r = startRow; r <= endRow; r++)
                {
                    string currentHang = r.ToString();
                    for (int i = TuSo; i <= DenSo; i++)
                    {
                        string tenGhe = currentHang + i;

                        // Tìm ghế trong DB
                        var ghe = _context.GheNgois.FirstOrDefault(g => g.MaPhong == MaPhong && g.TenGhe == tenGhe);

                        if (ghe != null)
                        {
                            // Kiểm tra ràng buộc
                            bool coVe = _context.ChiTietScGns.Any(ct => ct.MaGhe == ghe.MaGhe);

                            if (!coVe)
                            {
                                _context.GheNgois.Remove(ghe);
                                deletedCount++;
                            }
                            else
                            {
                                skipCount++; // Bỏ qua vì đã có vé
                            }
                        }
                        else
                        {
                            notFoundCount++;
                        }
                    }
                }

                if (deletedCount > 0)
                {
                    _context.SaveChanges();
                    string msg = $"Đã xóa thành công {deletedCount} ghế.";

                    if (skipCount > 0)
                        msg += $" (Bỏ qua {skipCount} ghế đang có dữ liệu đặt vé).";

                    TempData["Success"] = msg;
                    return RedirectToAction("Index");
                }
                else
                {
                    if (skipCount > 0)
                    {
                        TempData["Error"] = $"Không xóa được ghế nào! Có {skipCount} ghế trong phạm vi này đã bán vé.";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["Error"] = "Không tìm thấy ghế nào trong phạm vi đã chọn để xóa.";
                        return RedirectToAction("Index");
                    }
                }
            }

            // Load lại View nếu lỗi
            ViewBag.DsCumRap = new SelectList(_context.CumRaps, "MaCumRap", "TenCumRap");
            // Load lại phòng của rạp hiện tại
            var phong = _context.PhongChieus.Find(MaPhong);
            if (phong != null)
            {
                var listPhong = _context.PhongChieus.Where(p => p.MaCumRap == phong.MaCumRap);
                ViewBag.MaPhong = new SelectList(listPhong, "MaPhong", "TenPhong", MaPhong);
            }
            else
            {
                ViewBag.MaPhong = new SelectList(new List<PhongChieu>(), "MaPhong", "TenPhong");
            }

            ViewBag.HangGheList = new SelectList(GetDanhSachHangGhe());
            return View();
        }
    }
}
