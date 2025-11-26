using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class DonDatVeDoAn
    {
        public int MaCombo { get; set; }
        public int MaDon { get; set; }

        public virtual DoAn MaComboNavigation { get; set; } = null!;
        public virtual DonDatVe MaDonNavigation { get; set; } = null!;
    }
}
