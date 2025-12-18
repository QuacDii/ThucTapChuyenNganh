namespace TTCN.Models
{
    public class LichChieuPhim
    {
        public int MaPhim { get; set; }
        public string TenPhim { get; set; } = string.Empty;
        public string PosterPhim { get; set; } = string.Empty;
        public int ThoiLuong { get; set; }
        public string TheLoai { get; set; } = string.Empty;
        public List<SuatChieuItem> SuatChieus { get; set; } = new();
    }
}
