using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTCN.Models
{
    public partial class SuatChieu
    {
        public SuatChieu()
        {
            ChiTietDonDat = new HashSet<ChiTietDonDat>();
        }

        [Key]
        [Display(Name = "Mã suất")]
        public int MaSuat { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ bắt đầu!")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        [Display(Name = "Bắt đầu")]
        public DateTime? GioBatDau { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        [Display(Name = "Kết thúc")]
        public DateTime? GioKetThuc { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giá cơ bản!")]
        [Range(1000, 10000000, ErrorMessage = "Giá phải từ 1.000đ trở lên.")]
        [Display(Name = "Giá ghế cơ bản")]
        public decimal? Gia { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phim!")]
        public int? MaPhim { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng chiếu phim!")]
        public int? MaPhong { get; set; }

        public virtual Phim MaPhimNavigation { get; set; } = null!;
        public virtual PhongChieu MaPhongNavigation { get; set; } = null!;
        public virtual ICollection<ChiTietDonDat> ChiTietDonDat { get; set; }
    }
}