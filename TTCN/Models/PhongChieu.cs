using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class PhongChieu
    {
        public PhongChieu()
        {
            GheNgois = new HashSet<GheNgoi>();
            SuatChieus = new HashSet<SuatChieu>();
        }

        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = null!;
        public int TongGhe { get; set; }
        public int MaCumRap { get; set; }

        public virtual CumRap MaCumRapNavigation { get; set; } = null!;
        public virtual ICollection<GheNgoi> GheNgois { get; set; }
        public virtual ICollection<SuatChieu> SuatChieus { get; set; }
    }
}
