using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TTCN.Models;
using TTCN.Services;
using static System.Net.WebRequestMethods;

namespace TTCN.Controllers
{
    public class AccountController : Controller
    {
        private readonly QLDVContext _context;
        private readonly IEmailService _emailService;

        public AccountController(QLDVContext context, IEmailService email)
        {
            _context = context;
            _emailService = email;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(); 
        }

        [HttpPost]
        public IActionResult Login(string email, string matKhau)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(matKhau))
            {
                return Json(new { success = false, message = "Vui lòng nhập email và mật khẩu." });
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.MatKhau == matKhau); 

            if (user == null)
            {
                return Json(new { success = false, message = "Email hoặc mật khẩu không chính xác." });
            }

            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserHoTen", user.HoTen);
            HttpContext.Session.SetString("UserVaiTro", user.VaiTro);
            HttpContext.Session.SetString("SessionStartTime", DateTime.UtcNow.ToString("o"));

            if (user.VaiTro == "Admin")
            {
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Users") });
            }
            else
            {
                return Json(new { success = true, redirectUrl = Url.Action("Index", "Home") });
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult QuenMatKhau()
        {
            return View();
        }

        [HttpPost]
        public IActionResult QuenMatKhau(string email)
        {
            var user=_context.Users.FirstOrDefault(u=>u.Email == email);
            if(user == null)
            {
                TempData["Error"] = "Email không tồn tại trong hệ thống";
                return View();
            }

            var opt = new Random().Next(1000, 9999).ToString();
            var subject = "Mã OTP khôi phục mật khẩu";
            var message = $"Mã OTP của bạn là: {opt}. Mã này sẽ hết hạn sau 10 phút.";

            // Dùng .GetAwaiter().GetResult() để gọi đồng bộ
            _emailService.SendEmailAsync(user.Email, subject, message).GetAwaiter().GetResult();

            HttpContext.Session.SetString("ResetOtpCode", opt);
            HttpContext.Session.SetString("ResetOtpEmail", email);
            HttpContext.Session.SetString("ResetOtpExpiry", DateTime.UtcNow.AddMinutes(10).ToString("o", CultureInfo.InvariantCulture));

            return RedirectToAction("ResetMatKhau");
        }

        [HttpGet]
        public IActionResult ResetMatKhau()
        {
            var email = HttpContext.Session.GetString("ResetOtpEmail");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("QuenMatKhau");
            }

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public IActionResult ResetMatKhau(string email, string otp, string newPassword, string confirmPassword)
        {
            var emailSession = HttpContext.Session.GetString("ResetOtpEmail");
            var otpSession = HttpContext.Session.GetString("ResetOtpCode");
            var expirySession = HttpContext.Session.GetString("ResetOtpExpiry");

            //THẤT BẠI
            if (string.IsNullOrEmpty(emailSession) || emailSession != email)
            {
                TempData["Error"] = "Mã đã hết hạn, vui lòng thử lại";
                return RedirectToAction("QuenMatKhau");
            }
            ViewBag.Email = email;

            if(newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu mới và mật khẩu xác nhận không khớp.";
                return View();
            }

            if (otpSession != otp)
            {
                TempData["Error"] = "Mã OTP không chính xác.";
                return View();
            }

            if (!DateTime.TryParseExact(expirySession, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiryTime) || expiryTime < DateTime.UtcNow)
            {
                TempData["Error"] = "Mã OTP đã hết hạn. Vui lòng thử lại.";
                return View();
            }

            //THÀNH CÔNG
            var user = _context.Users.FirstOrDefault(u => u.Email == email); 
            if (user == null)
            {
                TempData["Error"] = "Tài khoản không hợp lệ.";
                return RedirectToAction("QuenMatKhau");
            }

            user.MatKhau = newPassword;
            _context.SaveChanges();

            HttpContext.Session.Remove("ResetOtpCode");
            HttpContext.Session.Remove("ResetOtpEmail");
            HttpContext.Session.Remove("ResetOtpExpiry");

            TempData["Success"] = "Bạn đã đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult DangKy()
        {
            return View();
        }

        [HttpPost]
        public JsonResult SendOTP(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Vui lòng nhập email." });
            }

            var existUser = _context.Users.FirstOrDefault(u => u.Email == email);
            if (existUser != null)
            {
                return Json(new { success = false, message = "Email này đã được sử dụng." });
            }

            if (!new EmailAddressAttribute().IsValid(email))
            {
                return Json(new { success = false, message = "Định dạng Email không hợp lệ." });
            }

            var otp = new Random().Next(1000, 9999).ToString();
            var subject = "Mã OTP Xác thực tài khoản";
            var message = $"Mã OTP của bạn là: {otp}. Mã này có hiệu lực 10 phút.";

            _emailService.SendEmailAsync(email, subject, message).GetAwaiter().GetResult();
            HttpContext.Session.SetString("RegisterOtpCode", otp);
            HttpContext.Session.SetString("RegisterOtpEmail", email);
            HttpContext.Session.SetString("RegisterOtpExpiry", DateTime.UtcNow.AddMinutes(10).ToString("o", CultureInfo.InvariantCulture));

            return Json(new { success = true, message = "Đã gửi mã OTP đến email của bạn." });
        }

        [HttpPost]
        public IActionResult DangKy(User us, string otp)
        {
            var otpSession = HttpContext.Session.GetString("RegisterOtpCode");
            var emailSession = HttpContext.Session.GetString("RegisterOtpEmail");
            var expirySession = HttpContext.Session.GetString("RegisterOtpExpiry");

            Action<string> SetError = (msg) => {
                ViewBag.Error = msg;
                // Xóa OTP cũ để tránh dùng lại
                HttpContext.Session.Remove("RegisterOtpCode");
                HttpContext.Session.Remove("RegisterOtpEmail");
                HttpContext.Session.Remove("RegisterOtpExpiry");
            };

            if (!ModelState.IsValid)
            {
                return View(us);
            }

            if (string.IsNullOrEmpty(otpSession) || emailSession != us.Email)
            {
                SetError("Vui lòng nhấn 'Lấy mã OTP' và xác thực email.");
                return View(us);
            }
            if (otpSession != otp)
            {
                SetError("Mã OTP không chính xác.");
                return View(us);
            }
            if (!DateTime.TryParseExact(expirySession, "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiryTime) || expiryTime < DateTime.UtcNow)
            {
                SetError("Mã OTP đã hết hạn. Vui lòng nhấn 'Lấy mã OTP' lại.");
                return View(us);
            }

            var existPhone = _context.Users.FirstOrDefault(u => u.SoDienThoai == us.SoDienThoai);
            if (existPhone != null)
            {   
                SetError("Số điện thoại này đã được sử dụng.");
                return View(us);
            }

            var newUser = new User
            {
                MaUsers = (_context.Users.Select(u => (int?)u.MaUsers).Max() ?? 0) + 1,
                HoTen = us.HoTen,
                Email = us.Email,
                MatKhau = us.MatKhau,
                SoDienThoai = us.SoDienThoai,
                NgayTao = DateTime.Now,
                VaiTro = "User"
            };
                
            _context.Users.Add(newUser);
            _context.SaveChanges();

            // Xóa Session OTP
            HttpContext.Session.Remove("RegisterOtpCode");
            HttpContext.Session.Remove("RegisterOtpEmail");
            HttpContext.Session.Remove("RegisterOtpExpiry");

            // Tự động đăng nhập
            HttpContext.Session.SetString("UserEmail", newUser.Email);
            HttpContext.Session.SetString("UserHoTen", newUser.HoTen);
            HttpContext.Session.SetString("UserVaiTro", newUser.VaiTro);
            HttpContext.Session.SetString("SessionStartTime", DateTime.UtcNow.ToString("o"));

            return RedirectToAction("Index", "Home");
        }
    }
}