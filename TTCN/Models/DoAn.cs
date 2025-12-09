using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TTCN.Models
{
    public partial class DoAn
    {

        [Key]
        [Display(Name = "Mã Combo")]
        public int MaCombo { get; set; }

        [Display(Name = "Tên Combo")]
        [Required(ErrorMessage = "Vui lòng nhập các món có trong Combo.")]
        public string MoTa { get; set; } = null!;

        [Display(Name = "Giá của Combo")]
        [Range(1000, 10000000, ErrorMessage = "Giá phải từ 1.000đ trở lên.")]
        [Required(ErrorMessage = "Vui lòng chọn giá của Combo")]
        public decimal Gia { get; set; }

        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;

        public virtual ICollection<DonDatVeDoAn> DonDatVeDoAns { get; set; }
    }
}