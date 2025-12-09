using Microsoft.AspNetCore.Mvc;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class CumRapController : Controller
    {
        private readonly QLDVContext _context;
        public CumRapController(QLDVContext context)
        {
            this._context = context;
        }

        private List<string> getTP()
        {
            return new List<string>
            {
                "An Giang", "Bà Rịa - Vũng Tàu", "Bạc Liêu", "Bắc Giang", "Bắc Kạn", "Bắc Ninh",
                "Bến Tre", "Bình Dương", "Bình Định", "Bình Phước", "Bình Thuận", "Cà Mau",
                "Cao Bằng", "Cần Thơ", "Đà Nẵng", "Đắk Lắk", "Đắk Nông", "Điện Biên", "Đồng Nai",
                "Đồng Tháp", "Gia Lai", "Hà Giang", "Hà Nam", "Hà Nội", "Hà Tĩnh", "Hải Dương",
                "Hải Phòng", "Hậu Giang", "Hòa Bình", "Hưng Yên", "Khánh Hòa", "Kiên Giang",
                "Kon Tum", "Lai Châu", "Lâm Đồng", "Lạng Sơn", "Lào Cai", "Long An", "Nam Định",
                "Nghệ An", "Ninh Bình", "Ninh Thuận", "Phú Thọ", "Phú Yên", "Quảng Bình",
                "Quảng Nam", "Quảng Ngãi", "Quảng Ninh", "Quảng Trị", "Sóc Trăng", "Sơn La",
                "Tây Ninh", "Thái Bình", "Thái Nguyên", "Thanh Hóa", "Thừa Thiên Huế", "Tiền Giang",
                "TP. Hồ Chí Minh", "Trà Vinh", "Tuyên Quang", "Vĩnh Long", "Vĩnh Phúc", "Yên Bái"
            };
        }
        public IActionResult Index(string search, string thanhPho)
        {
            var query = _context.CumRaps.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(x => x.TenCumRap.Contains(search));
            }
            if (!string.IsNullOrEmpty(thanhPho))
            {
                query = query.Where(x => x.ThanhPho == thanhPho);
            }
            ViewBag.DsThanhPho = getTP(); 
            ViewBag.CurrentName = search;
            ViewBag.CurrentCity = thanhPho;

            var result = query.OrderByDescending(x => x.MaCumRap).ToList();
            return View(result);
        }

        [HttpGet]
        public IActionResult them()
        {
            ViewBag.DsThanhPho = getTP();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult them(CumRap cum)
        {
            if (_context.CumRaps.Any(x => x.TenCumRap == cum.TenCumRap && x.ThanhPho==cum.ThanhPho))
            {
                ModelState.AddModelError("", "Cụm rạp này đã tồn tại! Vui lòng chọn tên khác.");
            }
            if (ModelState.IsValid)
            {
                int max = 0;
                if(_context.CumRaps.Any())
                {
                    max=_context.CumRaps.Max(c=>c.MaCumRap);
                }
                cum.MaCumRap = max + 1;
                _context.CumRaps.Add(cum);
                _context.SaveChanges();

                TempData["Success"] = "Thêm Cụm rạp thành công!";
                return RedirectToAction("Index");
                
            }
            ViewBag.DsThanhPho = getTP();
            return View(cum);
        }

        [HttpGet]
        public IActionResult sua(int id)
        {
            if (id == null) return NotFound();

            var cum = _context.CumRaps.Find(id);
            if (cum == null) return NotFound();

            ViewBag.DsThanhPho = getTP();
            return View(cum);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult sua(int id, CumRap cumRap)
        {
            if (id != cumRap.MaCumRap) return NotFound();
            if (_context.CumRaps.Any(x => x.TenCumRap == cumRap.TenCumRap && x.ThanhPho == cumRap.ThanhPho && x.MaCumRap != id))
            {
                ModelState.AddModelError("", "Cụm rạp này đã tồn tại! Vui lòng chọn tên khác.");
            }
            if (ModelState.IsValid)
            {
                _context.CumRaps.Update(cumRap);
                _context.SaveChanges();
                TempData["Success"] = "Cập nhật Cụm rạp thành công!";
                return RedirectToAction("Index");

            }
            ViewBag.DsThanhPho = getTP();
            return View(cumRap);
        }

        [HttpGet]
        public IActionResult xoa(int id)
        {
            if (id == null) return NotFound();
            var cumRap = _context.CumRaps.FirstOrDefault(m => m.MaCumRap == id);
            if (cumRap == null) return NotFound();
            return View(cumRap);
        }

        [HttpPost, ActionName("xoa")]
        [ValidateAntiForgeryToken]
        public IActionResult xoa_Post(int id)
        {
            var cumRap = _context.CumRaps.Find(id);
            if (cumRap != null)
            {
                var coPhongChieu = _context.PhongChieus.Any(pc => pc.MaCumRap == id);
                if (coPhongChieu)
                {
                    TempData["Error"] = "Không thể xóa vì rạp này đang có phòng chiếu!";
                    return View(cumRap);
                }

                _context.CumRaps.Remove(cumRap);
                _context.SaveChanges();
                TempData["Message"] = "Đã xóa cụm rạp!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
