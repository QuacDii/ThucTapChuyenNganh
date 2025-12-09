using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class CumRap
    {
        public CumRap()
        {
            PhongChieus = new HashSet<PhongChieu>();
        }

        public int MaCumRap { get; set; }
        public string TenCumRap { get; set; } = null!;
        public string DiaChi { get; set; } = null!;
        public string ThanhPho { get; set; } = null!;

        public virtual ICollection<PhongChieu> PhongChieus { get; set; }
    }
}
