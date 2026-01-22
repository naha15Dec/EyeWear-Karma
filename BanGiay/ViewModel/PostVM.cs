using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
namespace BanGiay.ViewModel
{
    public class PostVM
    {
        public string IDPost { set; get; }
        [Required(ErrorMessage = "Không được bỏ trống tên bài viết")]
        [StringLength(250,ErrorMessage =("Tên bài viết tối đa 250 ký tự"))]
        public string NamePost { set; get; }
        [Required(ErrorMessage = "Không được bỏ trống nội dung tóm tắt bài viết")]
        [StringLength(2000, ErrorMessage = ("Tên bài viết tối đa 2000 ký tự"))]
        public string SummaryContent { set; get; }
        [Required(ErrorMessage = "Không được bỏ trống nội dung bài viết")]
        [StringLength(4000, ErrorMessage = ("Tên bài viết tối đa 4000 ký tự"))]
        [AllowHtml]
        public string Content { set; get; }
        public string Image { set; get; }
        public DateTime? DatePost { set; get; }
        public int View { set; get; }
        public string Account { set; get; }
        public bool Enable { set; get; }
        
    }
}