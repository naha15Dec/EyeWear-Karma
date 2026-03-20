using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    public class AdminBlogIndexVm
    {
        public string HeaderTitle { get; set; }
        public string StatusFilter { get; set; }
        public string Keyword { get; set; }

        public List<AdminBlogListItemVm> Posts { get; set; } = new List<AdminBlogListItemVm>();
        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
    }

    public class AdminBlogListItemVm
    {
        public int BaiVietId { get; set; }
        public string MaBaiViet { get; set; }
        public string TieuDe { get; set; }
        public string TomTat { get; set; }
        public string AnhDaiDien { get; set; }

        public int TrangThai { get; set; }
        public string TrangThaiText { get; set; }

        public string NguoiTao { get; set; }
        public DateTime? NgayDang { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}