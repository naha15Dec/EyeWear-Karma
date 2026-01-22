using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace BanGiay.ViewModel
{
    public class UpdateAccount
    {
        [Required(ErrorMessage = "Không được bỏ trống tài khoản")]
        [StringLength(50, ErrorMessage = "Tài khoản phải dưới 50 ký tự")]
        public string UserName { set; get; }
        [Required(ErrorMessage = "Không được bỏ trống mật khẩu")]
        public string PassWord { set; get; }
        [Required(ErrorMessage = "Không được bỏ trống mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
        public string ComfirmPassword { set; get; }
        [Required(ErrorMessage = "Không được bỏ trống Email")]
        [EmailAddress(ErrorMessage = "Vui lòng nhập đúng địa chỉ email")]
        public string Email { set; get; }
        [Required(ErrorMessage = "Không được bỏ trống điện thoại")]
        [RegularExpression(@"^\d{0,10}$", ErrorMessage = "Số điện thoại chỉ gồm số 0-10")]
        [StringLength(13, ErrorMessage = "Số điện thoại chỉ được dưới 13 số")]
        public string Mobile { set; get; }
        public int AmountOfMoney { set; get; }
        [Required(ErrorMessage = "Không được bỏ trống tên")]
        [StringLength(50)]
        public string FirstName { set; get; }
        [Required(ErrorMessage = "Không được bỏ trống họ")]
        [StringLength(50)]
        public string LastName { set; get; }
        public string Sex { set; get; }
        [StringLength(250,ErrorMessage = "Địa chỉ không được quá 250 ký tự")]
        public string Address { set; get; }
        public bool StatusAccount { set; get; }
        public DateTime DateCreateAccount { set; get; }

    }
}