using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class GheNgoi
    {
        public GheNgoi()
        {
            ChiTietScGn = new HashSet<ChiTietScGn>();
        }

        public int MaGhe { get; set; }
        public string TenGhe { get; set; }
        public string HangGhe { get; set; } = null!;
        public string LoaiGhe { get; set; } = null!;
        public int MaPhong { get; set; }

        public virtual PhongChieu MaPhongNavigation { get; set; } = null!;
        public virtual ICollection<ChiTietScGn> ChiTietScGn { get; set; }
    }
}
