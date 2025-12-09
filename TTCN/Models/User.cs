using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class User
    {
        public User()
        {
            DonDatVes = new HashSet<DonDatVe>();
        }

        public int MaUsers { get; set; }
        public string HoTen { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string MatKhau { get; set; } = null!;
        public string SoDienThoai { get; set; } = null!;
        public string VaiTro { get; set; } = null!;
        public DateTime NgayTao { get; set; }

        public virtual ICollection<DonDatVe> DonDatVes { get; set; }
    }
}
