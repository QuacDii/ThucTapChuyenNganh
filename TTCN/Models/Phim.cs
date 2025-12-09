using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class Phim
    {
        public Phim()
        {
            SuatChieus = new HashSet<SuatChieu>();
        }

        public int MaPhim { get; set; }
        public string TenPhim { get; set; } = null!;
        public string? MoTa { get; set; }
        public int ThoiLuong { get; set; }
        public DateTime? NgayPhatHanh { get; set; }
        public string DaoDien { get; set; } = null!;
        public string? PosterPhim { get; set; }
        public string? TrailerPhim { get; set; }
        public string TrangThai { get; set; } = null!;
        public DateTime? NgayKetThuc { get; set; }

        public virtual ICollection<SuatChieu> SuatChieus { get; set; }
    }
}
