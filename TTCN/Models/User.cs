using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTCN.Models
{
    public partial class User
    {
        public User()
        {
            DonDatVes = new HashSet<DonDatVe>();
        }

        [Key]
        public int MaUsers { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string HoTen { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập Email.")]
        [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ!")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        public string MatKhau { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Vui lòng nhập đúng định dạng số điện thoại")]
        public string SoDienThoai { get; set; } = null!;
        public string VaiTro { get; set; } = null!;
        public DateTime NgayTao { get; set; }

        public virtual ICollection<DonDatVe> DonDatVes { get; set; }
    }
}
