using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        [DisplayName("Mã Phòng")]
        public int MaPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phòng chiếu.")]
        [StringLength(50, ErrorMessage = "Tên phòng không được vượt quá 50 ký tự.")]
        [DisplayName("Tên Phòng")]
        public string TenPhong { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập tổng số ghế.")]
        [Range(1, 300, ErrorMessage = "Tổng số ghế phải lớn hơn 0.")]
        [DisplayName("Tổng Số Ghế")]
        public int TongGhe { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn cụm rạp.")]
        [DisplayName("Thuộc Cụm Rạp")]
        public int MaCumRap { get; set; }

        public virtual CumRap MaCumRapNavigation { get; set; } = null!;
        public virtual ICollection<GheNgoi> GheNgois { get; set; }
        public virtual ICollection<SuatChieu> SuatChieus { get; set; }
    }
}
