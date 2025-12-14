using Microsoft.AspNetCore.Mvc.Rendering;

namespace TTCN.Models
{
    public class ThongKe
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public decimal TongDoanhThu { get; set; }
        public int SoVeBanRa { get; set; }
        public decimal DoanhThuCombo { get; set; }
        public int SoDonHang { get; set; }
        public string chonTP { get; set; }
        public int? chonMaRap { get; set; } 
        public SelectList thanhPho { get; set; } 
        public SelectList rapChieu { get; set; }

        public List<int> DataVeBanNgay { get; set; }       
        public List<decimal> DataComboNgay { get; set; }   
        public List<int> DataDonHangNgay { get; set; }
        public List<string> LabelsNgay { get; set; } 
        public List<decimal> DataDoanhThuNgay { get; set; }
        public List<string> RapLabels { get; set; }    
        public List<decimal> RapDataDoanhThu { get; set; }
        public List<int> RapDataSoVe { get; set; }  
        public List<int> RapDataSoDon { get; set; }

        public List<TopPhimVM> TopPhims { get; set; }

        public List<DoanhThuRapVM> DoanhThuTheoRap { get; set; }
        public class DoanhThuRapVM
        {
            public string TenRap { get; set; }
            public decimal DoanhThu { get; set; }
        }
        public class TopPhimVM
        {
            public string TenPhim { get; set; }
            public int SoVe { get; set; }
            public decimal DoanhThu { get; set; }
        }
    }
}
