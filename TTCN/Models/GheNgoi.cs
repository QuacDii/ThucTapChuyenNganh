using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        [DisplayName("Mã Ghế")]
        public int MaGhe { get; set; }

        [DisplayName("Tên Ghế")]
        public string TenGhe { get; set; } = null!;

        [DisplayName("Hàng Ghế")]
        public string HangGhe { get; set; } = null!;

        [DisplayName("Loại Ghế")]
        public string LoaiGhe { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn phòng chiếu.")]
        [DisplayName("Phòng Chiếu")]
        public int MaPhong { get; set; }

        [DisplayName("Phòng Chiếu")]
        public virtual PhongChieu MaPhongNavigation { get; set; } = null!;
        public virtual ICollection<ChiTietDonDat> ChiTietDonDat { get; set; }
    }
}
