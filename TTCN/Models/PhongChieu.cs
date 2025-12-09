using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTCN.Models
{
    public partial class PhongChieu
    {
        public PhongChieu()
        {
            GheNgois = new HashSet<GheNgoi>();
            SuatChieus = new HashSet<SuatChieu>();
        }

        [Key]
        [Display(Name = "Mã phòng")]
        public int MaPhong { get; set; }

        [Display(Name = "Tên phòng chiếu")]
        [Required(ErrorMessage = "Vui lòng nhập tên phòng chiếu.")]
        [StringLength(50, ErrorMessage = "Tên phòng không được quá dài (tối đa 50 ký tự).")]
        public string TenPhong { get; set; } = null!;

        [Display(Name = "Tổng số ghế")]
        [Required(ErrorMessage = "Vui lòng nhập số lượng ghế.")]
        [Range(10, 1000, ErrorMessage = "Số ghế phải nằm trong khoảng từ 10 đến 1000 ghế.")]
        public int TongGhe { get; set; }

        [Display(Name = "Thuộc cụm rạp")]
        public int MaCumRap { get; set; }

        public virtual CumRap MaCumRapNavigation { get; set; } = null!;
        public virtual ICollection<GheNgoi> GheNgois { get; set; }
        public virtual ICollection<SuatChieu> SuatChieus { get; set; }
    }
}
