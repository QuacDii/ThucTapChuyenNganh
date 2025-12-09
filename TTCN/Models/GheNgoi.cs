using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTCN.Models
{
    public partial class GheNgoi
    {
        public GheNgoi()
        {
            ChiTietDonDat = new HashSet<ChiTietDonDat>();
        }

        [Key]
        [Display(Name = "Mã ghế")]
        public int MaGhe { get; set; }

        [Display(Name = "Tên ghế")]
        [Required(ErrorMessage = "Vui lòng nhập tên ghế.")]
        [StringLength(4, ErrorMessage = "Tên ghế không được quá 3 ký tự.")]
        public string TenGhe { get; set; }

        [Display(Name = "Hàng")]
        [Required(ErrorMessage = "Vui lòng chọn hàng ghế.")]
        public string HangGhe { get; set; } = null!;

        [Display(Name = "Loại ghế")]
        [Required(ErrorMessage = "Vui lòng chọn loại ghế.")]
        public string LoaiGhe { get; set; } = null!;

        [Display(Name = "Thuộc phòng")]
        [Required(ErrorMessage = "Vui lòng chọn phòng chiếu.")]
        public int MaPhong { get; set; }

        public virtual PhongChieu MaPhongNavigation { get; set; } = null!;
        public virtual ICollection<ChiTietDonDat> ChiTietDonDat { get; set; }
    }
}
