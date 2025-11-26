using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class Ve
    {
        public int MaVe { get; set; }
        public int MaCt { get; set; }
        public decimal GiaVe { get; set; }
        public virtual ChiTietScGnVe MaCtNavigation { get; set; } = null!;
    }
}
