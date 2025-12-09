using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.X9;
using System.Text;
using System.Text.Json;
using TTCN.Models;
using TTCN.Models.Momo;
using TTCN.Services.Momo;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace TTCN.Controllers
{   
    public class PaymentController : Controller
    {
        QLDVContext db=new QLDVContext();
        private IMomoService _momoService;
        //private readonly IVnPayService _vnPayService;
        public PaymentController(IMomoService momoService)
        {
            _momoService = momoService;

        }
        [HttpGet]
        public IActionResult Index()
        {
            return View(new PaymentCheckoutViewModel());
        }

        [HttpPost]
        public IActionResult Index(PaymentSelectionRequest request)
        {
            if (request == null)
            {
                return RedirectToAction("Index", "Home");
            }

            List<CheckoutSeatModel> seats = new();
            if (!string.IsNullOrWhiteSpace(request.SelectedSeatsJson))
            {
                try
                {
                        seats = JsonSerializer.Deserialize<List<CheckoutSeatModel>>(
                        request.SelectedSeatsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );

                }
                catch (JsonException)
                {
                    seats = new List<CheckoutSeatModel>();
                }
            }

            // Lấy mã người dùng đang đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            var viewModel = new PaymentCheckoutViewModel
            {
                MovieName = request.MovieName,
                CinemaName = string.IsNullOrWhiteSpace(request.CinemaName) ? "Chưa chọn" : request.CinemaName,
                RoomName = string.IsNullOrWhiteSpace(request.RoomName) ? "Chưa chọn" : request.RoomName,
                ShowDate = request.ShowDate,
                ShowTime = request.ShowTime,
                Seats = seats,
                ComboText = request.ComboText,
                ComboPrice = request.ComboPrice,
                TotalPrice = request.TotalPrice,
                MaSuat = request.MaSuat,
                MaUsers = userId

            };
            var order = new OrderInfoModel
            {
                FullName = "User",
                OrderId = new Random().Next(10000, 99999).ToString(),
                Amount = (long)viewModel.TotalPrice,
                OrderInfo = $"Thanh toán vé xem phim: {viewModel.MovieName}"
            };

            order.ExtraData = Convert.ToBase64String(
    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModel))
);


            ViewBag.OI = order;
            return View(viewModel);
        }
        [HttpPost]
        [Route("CreatePaymentUrl")]
        public async Task<IActionResult> CreatePaymentMomo(OrderInfoModel model)
        {
            ViewBag.OI = model;
            model.OrderInfo = $"Thanh toán vé xem phim: {model.OrderInfo}";

            var response = await _momoService.CreatePaymentMomo(model);

            if (response == null || string.IsNullOrEmpty(response.PayUrl))
            {
                return BadRequest("MoMo API error: " + JsonConvert.SerializeObject(response));
            }

            return Redirect(response.PayUrl);

        }
        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);
            return View(response);
        }
        
        [HttpPost]
        public IActionResult MomoNotify()
        {
            return Ok();
        }

    }
}
