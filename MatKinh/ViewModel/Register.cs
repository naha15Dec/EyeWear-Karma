using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MatKinh.ViewModel
{
    public class Register : IValidatableObject
    {
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9._]+$", ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái, số, dấu chấm và dấu gạch dưới.")]
        public string Username { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự.")]
        public string Password { get; set; }

        [Display(Name = "Nhập lại mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Mật khẩu nhập lại không khớp.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Họ")]
        [Required(ErrorMessage = "Vui lòng nhập họ.")]
        [StringLength(50, ErrorMessage = "Họ không được vượt quá 50 ký tự.")]
        [RegularExpression(@"^[\p{L}\s'.-]+$", ErrorMessage = "Họ chỉ nên chứa chữ cái và khoảng trắng.")]
        public string LastName { get; set; }

        [Display(Name = "Tên")]
        [Required(ErrorMessage = "Vui lòng nhập tên.")]
        [StringLength(50, ErrorMessage = "Tên không được vượt quá 50 ký tự.")]
        [RegularExpression(@"^[\p{L}\s'.-]+$", ErrorMessage = "Tên chỉ nên chứa chữ cái và khoảng trắng.")]
        public string FirstName { get; set; }

        [Display(Name = "Giới tính")]
        [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
        public string Sex { get; set; }

        [Display(Name = "Ngày sinh")]
        [Required(ErrorMessage = "Vui lòng chọn ngày sinh.")]
        [DataType(DataType.Date)]
        [BirthDateValidation(ErrorMessage = "Ngày sinh không hợp lệ. Người dùng phải từ 6 đến 100 tuổi.")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^(0|\+84)(3|5|7|8|9)[0-9]{8}$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ.")]
        public string Mobile { get; set; }

        [Display(Name = "Địa chỉ")]
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(250, MinimumLength = 5, ErrorMessage = "Địa chỉ phải từ 5 đến 250 ký tự.")]
        public string Address { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            string username = (Username ?? string.Empty).Trim();
            string password = Password ?? string.Empty;
            string firstName = (FirstName ?? string.Empty).Trim();
            string lastName = (LastName ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(username))
            {
                if (username.StartsWith(".") || username.StartsWith("_") ||
                    username.EndsWith(".") || username.EndsWith("_"))
                {
                    yield return new ValidationResult(
                        "Tên đăng nhập không nên bắt đầu hoặc kết thúc bằng dấu chấm/gạch dưới.",
                        new[] { "Username" });
                }

                if (username.Contains("..") || username.Contains("__"))
                {
                    yield return new ValidationResult(
                        "Tên đăng nhập không nên chứa dấu chấm hoặc gạch dưới liên tiếp.",
                        new[] { "Username" });
                }
            }

            if (!string.IsNullOrWhiteSpace(password))
            {
                bool hasLetter = Regex.IsMatch(password, @"[A-Za-z]");
                bool hasDigit = Regex.IsMatch(password, @"\d");

                if (!hasLetter || !hasDigit)
                {
                    yield return new ValidationResult(
                        "Mật khẩu nên có ít nhất 1 chữ cái và 1 chữ số.",
                        new[] { "Password" });
                }

                if (!string.IsNullOrWhiteSpace(username) &&
                    password.ToLower().Contains(username.ToLower()))
                {
                    yield return new ValidationResult(
                        "Mật khẩu không nên chứa tên đăng nhập.",
                        new[] { "Password" });
                }
            }

            if (!string.IsNullOrWhiteSpace(firstName) &&
                !string.IsNullOrWhiteSpace(lastName) &&
                (firstName + " " + lastName).Trim().Length < 2)
            {
                yield return new ValidationResult(
                    "Họ tên không hợp lệ.",
                    new[] { "FirstName" });
            }

            string sex = (Sex ?? string.Empty).Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(sex) &&
                sex != "nam" &&
                sex != "nữ" &&
                sex != "nu")
            {
                yield return new ValidationResult(
                    "Giới tính không hợp lệ.",
                    new[] { "Sex" });
            }
        }
    }

    public class BirthDateValidation : ValidationAttribute
    {
        private const int MinimumAge = 6;
        private const int MaximumAge = 100;

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return false;
            }

            DateTime dob;

            if (value is DateTime)
            {
                dob = (DateTime)value;
            }
            else
            {
                return false;
            }

            DateTime today = DateTime.Today;

            if (dob.Date > today)
            {
                return false;
            }

            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age))
            {
                age--;
            }

            return age >= MinimumAge && age <= MaximumAge;
        }
    }
}