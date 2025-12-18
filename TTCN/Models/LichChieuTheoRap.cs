using TTCN.Models;

public class LichChieuTheoRap
{
    public List<CumRap> DsCumRap { get; set; } = new();
    public int? MaCumRap { get; set; }
    public DateTime NgayChieu { get; set; }
    public DateTime MinDate { get; set; }
    public DateTime MaxDate { get; set; }

    public List<LichChieuPhim> Phims { get; set; } = new();
}
