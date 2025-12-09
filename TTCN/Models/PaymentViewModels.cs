using System.Collections.Generic;
using System.Linq;

namespace TTCN.Models
{
    public class PaymentSelectionRequest
    {
        public string MovieName { get; set; } = string.Empty;
        public string CinemaName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string ShowDate { get; set; } = string.Empty;
        public string ShowTime { get; set; } = string.Empty;
        public string SelectedSeatsJson { get; set; } = string.Empty;
        public string ComboText { get; set; } = string.Empty;
        public decimal ComboPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int MaSuat { get; set; }

    }

    public class CheckoutSeatModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class PaymentCheckoutViewModel
    {
        public string MovieName { get; set; } = string.Empty;
        public string CinemaName { get; set; } = "Chưa chọn";
        public string RoomName { get; set; } = "Chưa chọn";
        public string ShowDate { get; set; } = string.Empty;
        public string ShowTime { get; set; } = string.Empty;
        public List<CheckoutSeatModel> Seats { get; set; } = new();
        public string ComboText { get; set; } = string.Empty;
        public decimal ComboPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int SeatCount => Seats?.Sum(s => s.Quantity) ?? 0;
        public int MaSuat { get; set; }
        public int MaUsers { get; set; }

    }
}

