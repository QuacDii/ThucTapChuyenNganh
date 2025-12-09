using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class DonDatVe
    {
        public DonDatVe()
        {
            ChiTietDonDats = new HashSet<ChiTietDonDat>();
        }

        public int MaDon { get; set; }
        public DateTime NgayDat { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; } = null!;
        public int MaUsers { get; set; }

        public virtual User MaUsersNavigation { get; set; } = null!;
        public virtual ICollection<ChiTietDonDat> ChiTietDonDats { get; set; }
    }
}
