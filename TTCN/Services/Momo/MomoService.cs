using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Crmf;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using TTCN.Models;
using TTCN.Models.Momo;

namespace TTCN.Services.Momo
{
    public class MomoService : IMomoService
    {
        private readonly IOptions<MomoOptionModel> _options;
        public MomoService(IOptions<MomoOptionModel> options)
        {
            _options = options;
        }
        //public async Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(OrderInfoModel model)
        //{
        //    model.OrderId = DateTime.UtcNow.Ticks.ToString();
        //    model.OrderInfo = "Khách hàng: " + model.FullName + ". Nội dung: " + model.OrderInfo;
        //    var rawData =
        //         $"accessKey={_options.Value.AccessKey}" +
        //         $"&amount={model.Amount}" +
        //         $"&extraData=" +
        //         $"&ipnUrl={_options.Value.NotifyUrl}" +
        //         $"&orderId={model.OrderId}" +
        //         $"&orderInfo={model.OrderInfo}" +
        //         $"&partnerCode={_options.Value.PartnerCode}" +
        //         $"&redirectUrl={_options.Value.ReturnUrl}" +
        //         $"&requestId={model.OrderId}" +
        //         $"&requestType={_options.Value.RequestType}";

        //    var signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

        //    var client = new RestClient(_options.Value.MomoApiUrl);
        //    var request = new RestRequest() { Method = Method.Post };
        //    request.AddHeader("Content-Type", "application/json; charset=UTF-8");

        //    // Create an object representing the request data
        //    var requestData = new
        //    {
        //        partnerCode = _options.Value.PartnerCode,
        //        partnerName = "MoMoPayment",
        //        storeId = "MoMoStore",
        //        requestId = model.OrderId,
        //        amount = model.Amount.ToString(),
        //        orderId = model.OrderId,
        //        orderInfo = model.OrderInfo,
        //        redirectUrl = _options.Value.ReturnUrl,
        //        ipnUrl = _options.Value.NotifyUrl,
        //        lang = "vi",
        //        autoCapture = true,
        //        requestType = _options.Value.RequestType,
        //        signature = signature
        //    };


        //    request.AddParameter("application/json", JsonConvert.SerializeObject(requestData), ParameterType.RequestBody);

        //    var response = await client.ExecuteAsync(request);
        //    var momoResponse = JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content);
        //    return momoResponse;

        //}
        public async Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(OrderInfoModel model)
        {
            // Chỉ tạo OrderId mới nếu chưa có
            if (string.IsNullOrWhiteSpace(model.OrderId))
            {
            model.OrderId = DateTime.UtcNow.Ticks.ToString();
            }
            model.OrderInfo = string.IsNullOrWhiteSpace(model.OrderInfo)
                                ? "Thanh toán vé xem phim"
                                : model.OrderInfo;

            // Sử dụng ExtraData từ model, nếu null thì dùng chuỗi rỗng
            string extraData = string.IsNullOrWhiteSpace(model.ExtraData) ? "" : model.ExtraData;

            string rawData =
                $"accessKey={_options.Value.AccessKey}" +
                $"&amount={model.Amount}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={_options.Value.NotifyUrl}" +
                $"&orderId={model.OrderId}" +
                $"&orderInfo={model.OrderInfo}" +
                $"&partnerCode={_options.Value.PartnerCode}" +
                $"&redirectUrl={_options.Value.ReturnUrl}" +
                $"&requestId={model.OrderId}" +
                $"&requestType={_options.Value.RequestType}";

            string signature = ComputeHmacSha256(rawData, _options.Value.SecretKey);

            var body = new
            {
                partnerCode = _options.Value.PartnerCode,
                partnerName = "MoMo",
                storeId = "Cinema",
                orderId = model.OrderId,
                amount = (int)model.Amount,
                orderInfo = model.OrderInfo,
                redirectUrl = _options.Value.ReturnUrl,
                ipnUrl = _options.Value.NotifyUrl,
                lang = "vi",
                requestType = _options.Value.RequestType,
                requestId = model.OrderId,
                extraData = extraData,
                signature = signature
            };

            var client = new RestClient(_options.Value.MomoApiUrl);
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(body);

            var response = await client.ExecuteAsync(request);
            var momoResponse = JsonConvert.DeserializeObject<MomoCreatePaymentResponseModel>(response.Content);

            return momoResponse;
        }

        public MomoExecuteResponseModel PaymentExecuteAsync(IQueryCollection collection)
        {
            var amountEntry = collection.FirstOrDefault(s => s.Key == "amount");
            var orderInfoEntry = collection.FirstOrDefault(s => s.Key == "orderInfo");
            var orderIdEntry = collection.FirstOrDefault(s => s.Key == "orderId");
            var resultCodeEntry = collection.FirstOrDefault(s => s.Key == "resultCode");
            var extraDataEntry = collection.FirstOrDefault(s => s.Key == "extraData");

            int resultCode = -1;
            if (resultCodeEntry.Key != null && resultCodeEntry.Value.Count > 0)
            {
                int.TryParse(resultCodeEntry.Value.ToString(), out resultCode);
            }

            return new MomoExecuteResponseModel()
            {
                Amount = amountEntry.Key != null && amountEntry.Value.Count > 0 ? amountEntry.Value.ToString() : string.Empty,
                OrderId = orderIdEntry.Key != null && orderIdEntry.Value.Count > 0 ? orderIdEntry.Value.ToString() : string.Empty,
                OrderInfo = orderInfoEntry.Key != null && orderInfoEntry.Value.Count > 0 ? orderInfoEntry.Value.ToString() : string.Empty,
                ResultCode = resultCode,
                ExtraData = extraDataEntry.Key != null && extraDataEntry.Value.Count > 0 ? extraDataEntry.Value.ToString() : string.Empty
            };
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            byte[] hashBytes;

            using (var hmac = new HMACSHA256(keyBytes))
            {
                hashBytes = hmac.ComputeHash(messageBytes);
            }

            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            return hashString;
        }
    }


}
