using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public class ChiTietDonDat
    {
        public int MaCt { get; set; }
        public bool TrangThai { get; set; }
        public int? MaGhe { get; set; }
        public int MaSuat { get; set; }
        public int MaDon { get; set; }
        public virtual DonDatVe MaDonNavigation { get; set; }
        public virtual GheNgoi? MaGheNavigation { get; set; }
        public virtual SuatChieu MaSuatNavigation { get; set; } = null!;
    }
}
