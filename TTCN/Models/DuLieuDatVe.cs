namespace TTCN.Models
{
    public class DuLieuDatVe
    {
        public int MaPhim { get; set; }
        public string TenPhim { get; set; }
        public string PosterPhim { get; set; }
        public int ThoiLuong { get; set; }
        public List<string> TenTheLoai { get; set; } = new List<string>();
        public List<DateTime> CacNgayChieu { get; set; } = new List<DateTime>();
    }
}
