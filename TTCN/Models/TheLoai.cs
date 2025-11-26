using System;
using System.Collections.Generic;

namespace TTCN.Models
{
    public partial class TheLoai
    {
        public int MaTheLoai { get; set; }
        public string TenTheLoai { get; set; } = null!;

        public virtual ICollection<PhimTheLoai> PhimTheLoais { get; set; }
    }
}
