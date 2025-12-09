using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class SuatChieu
    {
        public SuatChieu()
        {
            ChiTietDonDats = new HashSet<ChiTietDonDat>();
        }

        public int MaSuat { get; set; }
        public DateTime GioBatDau { get; set; }
        public DateTime GioKetThuc { get; set; }
        public decimal Gia { get; set; }
        public int MaPhim { get; set; }
        public int MaPhong { get; set; }

        public virtual Phim MaPhimNavigation { get; set; } = null!;
        public virtual PhongChieu MaPhongNavigation { get; set; } = null!;
        public virtual ICollection<ChiTietDonDat> ChiTietDonDats { get; set; }
    }
}
