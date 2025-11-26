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
        public int MaPhim { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên phim!")]
        [StringLength(200, ErrorMessage = "Tên phim không được quá 200 ký tự.")]
        public string TenPhim { get; set; } = null!;

        [DataType(DataType.MultilineText)]
        public string? MoTa { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thời lượng phim!")]
        [Range(1, 500, ErrorMessage = "Thời lượng phim phải từ 1 đến 500 phút.")]
        public int ThoiLuong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày phát hành!")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? NgayPhatHanh { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên đạo diễn!")]
        [StringLength(100, ErrorMessage = "Tên đạo diễn không được quá 100 ký tự.")]
        public string DaoDien { get; set; } = null!;
        public string? PosterPhim { get; set; }
        public string? TrailerPhim { get; set; }
        public string TrangThai { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc!")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)]
        public DateTime? NgayKetThuc { get; set; }

        public virtual ICollection<SuatChieu> SuatChieus { get; set; }
        public virtual ICollection<PhimTheLoai> PhimTheLoais { get; set; }
    }
}
