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
        public string SelectedCombosJson { get; set; }


    }
    public class CheckoutComboModel
    {
        public int MaCombo { get; set; }
        public int SoLuong { get; set; }
    }

    public class CheckoutSeatModel
    {
        public List<int> MaGhe { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity => MaGhe?.Count ?? 0;
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
        public int SeatCount => Seats?.Sum(s => s.MaGhe.Count) ?? 0;
        public int MaSuat { get; set; }
        public int MaUsers { get; set; }

    }
}


