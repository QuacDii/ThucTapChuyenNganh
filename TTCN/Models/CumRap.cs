using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        [DisplayName("Mã Cụm Rạp")]
        public int MaCumRap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên cụm rạp.")]
        [StringLength(100, ErrorMessage = "Tên cụm rạp không được quá 100 ký tự.")]
        [DisplayName("Tên Cụm Rạp")]
        public string TenCumRap { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        [StringLength(255, ErrorMessage = "Địa chỉ không được quá 255 ký tự.")]
        [DisplayName("Địa Chỉ")]
        public string DiaChi { get; set; } = null!;
        [Required(ErrorMessage = "Vui lòng chọn thành phố có trong danh sách.")]
        [DisplayName("Thành Phố")]
        public string ThanhPho { get; set; } = null!;

        public virtual ICollection<PhongChieu> PhongChieus { get; set; }
    }
}
