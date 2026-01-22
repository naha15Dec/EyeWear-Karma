using System;
using System.ComponentModel.DataAnnotations;

namespace BanGiay.ViewModel
{
    public class PurchaseInformation
    {
        public string IDCustomer { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống tên người đặt hàng")]
        [StringLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string NameCustomer { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống số điện thoại đặt hàng")]
        [RegularExpression(@"^(0[0-9]{9,10})$", ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string MobileCustomer { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống địa chỉ đặt hàng")]
        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string DeliveryAddress { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        // Vì flow hiện tại bắt buộc đăng nhập mới checkout,
        // Sex & BirthOfDate không bắt buộc nữa (để nullable cho an toàn)
        [Display(Name = "Giới tính")]
        public string Sex { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? BirthOfDate { get; set; }

        [Display(Name = "Ghi chú")]
        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string Note { get; set; }

        [Required(ErrorMessage = "Không được bỏ trống phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMenthods { get; set; }
    }
}
