using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    public class AdminBlogCreateVm
    {
        public int? BaiVietId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài viết")]
        public string TieuDe { get; set; }

        public string TomTat { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung bài viết")]
        [AllowHtml]
        public string NoiDung { get; set; }

        public string AnhDaiDien { get; set; }

        public DateTime? NgayDang { get; set; }

        public int TrangThai { get; set; }
    }
}