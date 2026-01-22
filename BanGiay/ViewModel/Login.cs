using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace BanGiay.ViewModel
{
    public class Login
    {
        [Display(Name = "Tài khoản đăng nhập")]
        [Required(ErrorMessage = "Không được bỏ trống")]
        public string Username { set; get; }
        [Display(Name = "Mật khẩu đăng nhập")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Không được bỏ trống")]
        public string Password { set; get; }
        public string Roles { set; get; }
        public bool RememberMe { get; set; }
    }
}