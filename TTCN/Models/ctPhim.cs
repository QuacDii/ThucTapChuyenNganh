namespace TTCN.Models
{
    public class ctPhim
    {
        public Phim Phim { get; set; }
        public List<string> TenTheLoais { get; set; }
        public double DiemTrungBinh { get; set; }
        public List<UsersPhim> DanhSachDanhGia { get; set; }
    }
}
