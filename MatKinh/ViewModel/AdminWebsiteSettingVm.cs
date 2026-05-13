using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using MatKinh.Models;

namespace MatKinh.ViewModel
{
    public class AdminWebsiteSettingVm
    {
        public ThongTinCuaHang CurrentInfo { get; set; }
        public List<ThongTinCuaHang> History { get; set; } = new List<ThongTinCuaHang>();

        [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng")]
        [StringLength(200, ErrorMessage = "Tên cửa hàng không được vượt quá 200 ký tự")]
        public string TenCuaHang { get; set; }

        [RegularExpression(@"^(0|\+84)(\d{9})$", ErrorMessage = "Hotline không hợp lệ. Ví dụ: 0900000000 hoặc +84900000000")]
        public string Hotline { get; set; }

        [EmailAddress(ErrorMessage = "Email liên hệ không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự")]
        public string DiaChi { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả ngắn không được vượt quá 500 ký tự")]
        public string MoTaNgan { get; set; }

        [AllowHtml]
        public string GioiThieu { get; set; }

        [StringLength(255, ErrorMessage = "Đường dẫn logo không được vượt quá 255 ký tự")]
        public string Logo { get; set; }

        [StringLength(255, ErrorMessage = "Đường dẫn banner không được vượt quá 255 ký tự")]
        public string Banner { get; set; }

        [Url(ErrorMessage = "Facebook URL không hợp lệ")]
        [StringLength(255, ErrorMessage = "Facebook URL không được vượt quá 255 ký tự")]
        public string FacebookUrl { get; set; }

        [Url(ErrorMessage = "Instagram URL không hợp lệ")]
        [StringLength(255, ErrorMessage = "Instagram URL không được vượt quá 255 ký tự")]
        public string InstagramUrl { get; set; }

        [StringLength(255, ErrorMessage = "Zalo URL không được vượt quá 255 ký tự")]
        public string ZaloUrl { get; set; }

        public bool IsActive { get; set; }
    }
}