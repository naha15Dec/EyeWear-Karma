using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    public class AdminAccountIndexVm
    {
        public string Keyword { get; set; }
        public string RoleFilter { get; set; }
        public string HeaderTitle { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage
        {
            get { return CurrentPage > 1; }
        }

        public bool HasNextPage
        {
            get { return CurrentPage < TotalPages; }
        }

        public List<AdminAccountListItemVm> Accounts { get; set; } = new List<AdminAccountListItemVm>();
        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
    }

    public class AdminAccountListItemVm
    {
        public int TaiKhoanId { get; set; }
        public string TenDangNhap { get; set; }
        public string HoTen { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public string DiaChi { get; set; }

        public int VaiTroId { get; set; }
        public string MaVaiTro { get; set; }
        public string TenVaiTro { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AdminAccountDetailVm
    {
        public int TaiKhoanId { get; set; }
        public string TenDangNhap { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public string SoDienThoai { get; set; }
        public string DiaChi { get; set; }

        public int VaiTroId { get; set; }
        public string MaVaiTro { get; set; }
        public string TenVaiTro { get; set; }

        public bool IsActive { get; set; }

        public List<SelectListItem> Roles { get; set; } = new List<SelectListItem>();
    }

    public class AdminAccountUpdateVm
    {
        public int TaiKhoanId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        public string SoDienThoai { get; set; }
        public string DiaChi { get; set; }
    }

    public class AdminAccountChangePasswordVm
    {
        public int TaiKhoanId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [System.ComponentModel.DataAnnotations.Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        public string ConfirmPassword { get; set; }
    }

    public class AdminAccountUpdateRoleVm
    {
        public int TaiKhoanId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        public int VaiTroId { get; set; }
    }
}