using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public class ChiTietScGnVe
    {
        public ChiTietScGnVe()
        {
            Ves = new HashSet<Ve>();
        }

        public int MaCt { get; set; }
        public bool TrangThai { get; set; }
        public int? MaGhe { get; set; }
        public int MaSuat { get; set; }

        public virtual GheNgoi? MaGheNavigation { get; set; }
        public virtual SuatChieu MaSuatNavigation { get; set; } = null!;
        public virtual ICollection<Ve> Ves { get; set; }
    }
}
