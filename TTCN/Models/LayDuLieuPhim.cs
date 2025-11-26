namespace TTCN.Models
{
    public class LayDuLieuPhim
    {
        public string TenPhim { get; set; } = null!;
        public string PosterPhim { get; set; } = null!;
        public int MaPhim { get; set; }
        public string TrailerPhim { get; set; } = null!;
        public int DiemTB { get; set; }
        public List<string> TenTheLoai { get; set; } = new List<string>();
    }
}
