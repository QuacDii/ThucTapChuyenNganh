using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public class ChiTietScGn
    {
        public int MaCt { get; set; }
        public bool TrangThai { get; set; }
        public int? MaGhe { get; set; }
        public int MaSuat { get; set; }

        public virtual GheNgoi? MaGheNavigation { get; set; }
        public virtual SuatChieu MaSuatNavigation { get; set; } = null!;
    }
}
