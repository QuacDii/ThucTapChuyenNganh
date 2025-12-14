using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TTCN.Models
{
    public class ChiTietDonDat
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaCt { get; set; }
        public bool TrangThai { get; set; }
        public int? MaGhe { get; set; }
        public int MaSuat { get; set; }
        public int MaDon { get; set; }
        public virtual DonDatVe MaDonNavigation { get; set; }
        public virtual GheNgoi? MaGheNavigation { get; set; }
        public virtual SuatChieu MaSuatNavigation { get; set; } = null!;
    }
}