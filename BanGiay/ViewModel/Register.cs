using System;
using System.ComponentModel.DataAnnotations;

namespace BanGiay.ViewModel
{
    public class Register
    {
        [Required(ErrorMessage = "Không được bỏ trống tài khoản")]
        [Display(Name = "Tài khoản")]
        [StringLength(20, MinimumLength = 4, ErrorMessage = "Tài khoản phải từ 4 đến 20 ký tự")]
        public string Username { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Không được bỏ trống mật khẩu")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Mật khẩu phải ít nhất 6 ký tự")]
        public string Password { get; set; }

        [Display(Name = "Nhập lại mật khẩu")]
        [Required(ErrorMessage = "Không được bỏ trống nhập lại mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
        public string ComfirmPassword { get; set; }

        [Display(Name = "Ngày sinh")]
        [Required(ErrorMessage = "Không được bỏ trống ngày sinh")]
        [DataType(DataType.Date)]
        [BirthDateValidation(ErrorMessage = "Ngày sinh không hợp lệ")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        public string Sex { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Không được bỏ trống Email")]
        [EmailAddress(ErrorMessage = "Vui lòng nhập đúng địa chỉ email")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Không được bỏ trống điện thoại")]
        [RegularExpression(@"^(0[0-9]{9})$", ErrorMessage = "Số điện thoại phải gồm 10 số và bắt đầu bằng 0")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại phải đúng 10 số")]
        public string Mobile { get; set; }

        [Display(Name = "Địa chỉ")]
        [StringLength(250, ErrorMessage = "Địa chỉ không được vượt quá 250 ký tự")]
        public string Address { get; set; }

        [Display(Name = "Họ")]
        [Required(ErrorMessage = "Không được bỏ trống họ")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự")]
        public string LastName { get; set; }

        [Display(Name = "Tên")]
        [Required(ErrorMessage = "Không được bỏ trống tên")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự")]
        public string FirstName { get; set; }
    }

    /// <summary>
    /// Validate ngày sinh: không được lớn hơn ngày hiện tại, và tối thiểu 6 tuổi
    /// </summary>
    public class BirthDateValidation : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null) return false;

            DateTime dob;
            if (!DateTime.TryParse(value.ToString(), out dob))
                return false;

            // Không được sinh sau hôm nay
            if (dob > DateTime.Now) return false;

            // Ít nhất 6 tuổi (tuỳ bạn, có thể bỏ nếu không cần)
            var age = DateTime.Now.Year - dob.Year;
            if (dob > DateTime.Now.AddYears(-age)) age--;

            if (age < 6) return false;

            return true;
        }
    }
}
