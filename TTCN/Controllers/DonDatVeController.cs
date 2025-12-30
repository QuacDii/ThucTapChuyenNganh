using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TTCN.Models;

namespace TTCN.Controllers
{
    public class DonDatVeController : Controller
    {
        private readonly QLDVContext _context;

        public DonDatVeController(QLDVContext context)
        {
            _context = context;
        }
        public IActionResult Index(string search, decimal? min, decimal? max, string trangThai, DateTime? ngayDat)
        {

            var query = _context.DonDatVes
                .Include(k=>k.ChiTietDonDat)
                    .ThenInclude(d => d.MaSuatNavigation)
                        .ThenInclude(s => s.MaPhimNavigation)
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaSuatNavigation)
                        .ThenInclude(s => s.MaPhongNavigation)
                            .ThenInclude(p => p.MaCumRapNavigation)
                .Include(u=>u.MaUsersNavigation)
                .AsQueryable();


            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => d.MaUsersNavigation.HoTen.Contains(search));
            }


            if (!string.IsNullOrEmpty(trangThai))
            {
                query = query.Where(d => d.TrangThai.Contains(trangThai));
            }

            if (ngayDat.HasValue)
            {
                query = query.Where(d => d.NgayDat.Date == ngayDat.Value.Date);
                ViewBag.CurrentDate = ngayDat.Value.ToString("yyyy-MM-dd");
            }

            if (min.HasValue)
            {
                query = query.Where(d => d.TongTien >= min.Value);
            }
            if (max.HasValue)
            {
                query = query.Where(d => d.TongTien <= max.Value);
            }

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = trangThai;

            var result = query.OrderByDescending(d => d.NgayDat).ToList();

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult huyDon(int id)
        {
            var donDatVe = _context.DonDatVes.Find(id);
            if (donDatVe == null)
            {
                return NotFound();
            }


            var listChiTiet = _context.ChiTietDonDat
                              .Include(ct => ct.MaSuatNavigation)
                              .Where(ct => ct.MaDon == id)
                              .ToList();


            var chiTietDau = listChiTiet.FirstOrDefault();

            if (chiTietDau != null && chiTietDau.MaSuatNavigation != null)
            {

                if (chiTietDau.MaSuatNavigation.GioBatDau <= DateTime.Now)
                {
                    TempData["Error"] = "Phim đang chiếu hoặc đã chiếu, không thể hủy đơn!";
                    return RedirectToAction("Index");
                }
            }

            donDatVe.TrangThai = "Đã hủy";
            donDatVe.TongTien = 0;


            if (listChiTiet.Any())
            {
                _context.ChiTietDonDat.RemoveRange(listChiTiet);
            }

            _context.SaveChanges();
            TempData["Success"] = "Đã hủy đơn vé và nhả ghế thành công!";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult chiTiet(int id)
        {
            var donDatVe = _context.DonDatVes

        .Include(d => d.MaUsersNavigation)


        .Include(d => d.ChiTietDonDat)
            .ThenInclude(ct => ct.MaGheNavigation) 
        .Include(d => d.ChiTietDonDat)
            .ThenInclude(ct => ct.MaSuatNavigation)
                .ThenInclude(s => s.MaPhimNavigation)
        .Include(d => d.ChiTietDonDat)
            .ThenInclude(ct => ct.MaSuatNavigation)
                .ThenInclude(s => s.MaPhongNavigation)
                    .ThenInclude(p => p.MaCumRapNavigation)


        .Include(d => d.DonDatVeDoAns)
            .ThenInclude(c => c.MaComboNavigation)

        .FirstOrDefault(d => d.MaDon == id);

            if (donDatVe == null) return NotFound();

            return View(donDatVe);
        }

        [HttpGet]
        public IActionResult chiTietDon(int id)
        {

            var don = _context.DonDatVes
                .Include(d => d.MaUsersNavigation)
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaGheNavigation)
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaSuatNavigation)
                        .ThenInclude(s => s.MaPhimNavigation)
                .Include(d => d.ChiTietDonDat)
                    .ThenInclude(ct => ct.MaSuatNavigation)
                        .ThenInclude(s => s.MaPhongNavigation)
                            .ThenInclude(p => p.MaCumRapNavigation)
                .Include(d => d.DonDatVeDoAns)
                    .ThenInclude(d => d.MaComboNavigation)
                .FirstOrDefault(d => d.MaDon == id);

            if (don == null)
                return NotFound();
            if (don.TrangThai != "Đã thanh toán")
                return BadRequest();

            return View(don);
        }

        [HttpGet]
        public IActionResult LichSu()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login", new { area = "Account" }); 
            }

            var listVe = _context.DonDatVes
                .Include(d => d.ChiTietDonDat).ThenInclude(ct => ct.MaSuatNavigation).ThenInclude(s => s.MaPhimNavigation)
                .Include(d => d.ChiTietDonDat).ThenInclude(ct => ct.MaGheNavigation)
                .Where(d => d.MaUsersNavigation.Email == userEmail)
                .OrderByDescending(d => d.NgayDat)
                .ToList();

            return View(listVe);
        }
    }

}
