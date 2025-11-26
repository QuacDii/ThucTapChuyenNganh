using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class PhimTheLoai
    {
        public int MaPhim { get; set; }
        public int MaTheLoai { get; set; }

        public virtual Phim MaPhimNavigation { get; set; } = null!;
        public virtual TheLoai MaTheLoaiNavigation { get; set; } = null!;
    }
}
