using System.ComponentModel.DataAnnotations;

namespace MatKinh.ViewModel
{
    public class PurchaseInformation
    {
        [Required(ErrorMessage = "Không được bỏ trống tên người nhận")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoTenNguoiNhan { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống số điện thoại")]
        [RegularExpression(@"^(0[0-9]{9,10})$", ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoaiNguoiNhan { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống địa chỉ giao hàng")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string DiaChiNhanHang { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public string PhuongThucThanhToan { get; set; }
    }
}