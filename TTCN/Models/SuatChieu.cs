using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTCN.Models
{
    public partial class SuatChieu
    {
        public SuatChieu()
        {
            ChiTietScGn = new HashSet<ChiTietScGn>();
            DonDatVes = new HashSet<DonDatVe>();
        }

        [Key]
        public int MaSuat { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu!")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? GioBatDau { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? GioKetThuc { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giá suất chiếu!")]
        public decimal? Gia { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phim!")]
        public int? MaPhim { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng chiếu phim!")]
        public int? MaPhong { get; set; }

        public virtual Phim MaPhimNavigation { get; set; } = null!;
        public virtual PhongChieu MaPhongNavigation { get; set; } = null!;
        public virtual ICollection<ChiTietScGn> ChiTietScGn { get; set; }
        public virtual ICollection<DonDatVe> DonDatVes { get; set; }
    }
}
