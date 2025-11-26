using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class DonDatVe
    {

        public int MaDon { get; set; }
        public int MaSuat { get; set; }
        public DateTime NgayDat { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; } = null!;

        public virtual SuatChieu MaSuatNavigation { get; set; } = null!;
        public virtual User MaUserstNavigation { get; set; } = null!;
    }
}
