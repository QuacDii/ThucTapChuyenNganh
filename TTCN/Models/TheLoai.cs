using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TTCN.Models
{
    public partial class TheLoai
    {
        public TheLoai()
        {
            PhimTheLoais = new HashSet<PhimTheLoai>();
        }

        [Key]
        public int MaTheLoai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thể loại!")]
        [StringLength(100, ErrorMessage = "Tên thể loại không được quá 100 ký tự.")]
        public string TenTheLoai { get; set; } = null!;

        public virtual ICollection<PhimTheLoai> PhimTheLoais { get; set; }
    }
}
