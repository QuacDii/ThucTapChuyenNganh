using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTCN.Models
{
    public partial class Phim
    {
        public Phim()
        {
            SuatChieus = new HashSet<SuatChieu>();
            PhimTheLoais = new HashSet<PhimTheLoai>();
        }

        [Key]
        [Display(Name = "Mã phim")]
        public int MaPhim { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phim!")]
        [StringLength(100, ErrorMessage = "Tên phim không được quá 100 ký tự.")]
        [Display(Name = "Tên phim")]
        public string TenPhim { get; set; } = null!;

        [DataType(DataType.MultilineText)]
        [Display(Name = "Mô tả")]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời lượng phim!")]
        [Range(1, 500, ErrorMessage = "Thời lượng phim phải từ 1 đến 500 phút.")]
        [Display(Name = "Thời lượng")]
        public int ThoiLuong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày công chiếu!")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày công chiếu")]
        public DateTime? NgayPhatHanh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đạo diễn!")]
        [StringLength(50, ErrorMessage = "Tên đạo diễn không được quá 50 ký tự.")]
        [Display(Name = "Đạo diễn")]
        public string DaoDien { get; set; } = null!;
        [Display(Name = "Poster phim")]
        public string? PosterPhim { get; set; }
        [Display(Name = "Trailer phim")]
        public string? TrailerPhim { get; set; }
        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc!")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày kết thúc")]
        public DateTime? NgayKetThuc { get; set; }

        public virtual ICollection<SuatChieu> SuatChieus { get; set; }
        public virtual ICollection<PhimTheLoai> PhimTheLoais { get; set; }
    }
}