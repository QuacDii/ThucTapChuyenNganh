namespace TTCN.Models.Momo
{
    public class MomoExecuteResponseModel
    {
        public int ResultCode { get; set; }
        public string OrderId { get; set; }
        public string Amount { get; set; }
        public string FullName { get; set; }
        public string OrderInfo { get; set; }
        public string ExtraData { get; set; }
    }
}
