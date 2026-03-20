using System.ComponentModel.DataAnnotations;

namespace MatKinh.ViewModel
{
    public class UpdateAccount
    {
        [Display(Name = "Mật khẩu mới")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự.")]
        public string PassWord { get; set; }

        [Display(Name = "Nhập lại mật khẩu mới")]
        [Compare("PassWord", ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        public string ComfirmPassword { get; set; }

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại phải gồm 10 số và bắt đầu bằng 0.")]
        public string Mobile { get; set; }

        [Display(Name = "Tên")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự.")]
        public string FirstName { get; set; }

        [Display(Name = "Họ")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự.")]
        public string LastName { get; set; }

        [Display(Name = "Giới tính")]
        public string Sex { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(250, ErrorMessage = "Địa chỉ không được quá 250 ký tự.")]
        public string Address { get; set; }
    }
}