using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTCN.Models
{
    public partial class CumRap
    {
        public CumRap()
        {
            PhongChieus = new HashSet<PhongChieu>();
        }

        [Key]
        [Display(Name = "Mã cụm rạp")]
        public int MaCumRap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên cụm rạp.")]
        [Display(Name = "Tên cụm rạp")]
        public string TenCumRap { get; set; } = null!;

        [Display(Name = "Địa chỉ")]
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        public string DiaChi { get; set; } = null!;

        [Display(Name = "Thành phố")]
        [Required(ErrorMessage = "Vui lòng chọn thành phố.")]
        public string ThanhPho { get; set; } = null!;

        public virtual ICollection<PhongChieu> PhongChieus { get; set; }
    }
}
