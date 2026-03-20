using System;
using System.ComponentModel.DataAnnotations;

namespace MatKinh.ViewModel
{
    public class AdminBlogCreateVm
    {
        public int? BaiVietId { get; set; }

        [Required]
        public string TieuDe { get; set; }

        public string TomTat { get; set; }

        [Required]
        public string NoiDung { get; set; }

        public string AnhDaiDien { get; set; }

        public DateTime? NgayDang { get; set; }
    }
}