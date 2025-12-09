using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TTCN.Models
{
    public partial class DonDatVe
    {

        [Key]
        [Display(Name ="Mã đơn đặt")]
        public int MaDon { get; set; }


        [Display(Name = "Ngày đặt vé")]
        public DateTime NgayDat { get; set; }

        [Display(Name = "Tổng tiền")]
        public decimal TongTien { get; set; }

        [Display(Name = "Trạng thái")]
        public string TrangThai { get; set; } = null!;

        [Column("maUsers")]
        [Display(Name = "Mã Khách Hàng")]
        public int? MaUsers { get; set; }


        [ForeignKey("MaUsers")]
        public virtual User MaUsersNavigation { get; set; } = null!;

        public virtual ICollection<ChiTietDonDat> ChiTietDonDat { get; set; }
    }
}
