using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class UsersPhim
    {
        public int MaPhim { get; set; }
        public int MaUsers { get; set; }
        public string? BinhLuan { get; set; }
        public int? Diem { get; set; }

        public virtual Phim MaPhimNavigation { get; set; } = null!;
        public virtual User MaUsersNavigation { get; set; } = null!;
    }
}
