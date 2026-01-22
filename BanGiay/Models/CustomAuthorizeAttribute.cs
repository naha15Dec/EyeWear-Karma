using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
namespace BanGiay.Models
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Kiểm tra xem người dùng có thuộc ít nhất một trong các vai trò được chỉ định không
            var userSession = httpContext.Session["LoginInformation"] as taiKhoanThanhVien; // UserSession là đối tượng lưu trạng thái đăng nhập

            if (httpContext.Session["LoginInformation"] != null)
            {
                var rolesArray = Roles.Split(',').ToList();

                foreach (var role in rolesArray)
                {
                    if (userSession.maNhom == (maNhom(role.Trim(), httpContext)))
                        return true;
                }
            }
            HttpContext.Current.Response.Redirect("~/Error/Index");
            return false;
        }
        /// <summary>
        /// Hàm này dùng để lấy tên nhóm của tài khoản thành mã nhóm để kiểm tra quyền hạn truy cập
        /// Lý do: chúng ta kiểm tra bằng mã thì tránh những trường hợp lỗi về dữ liệu
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public int maNhom(string temp, HttpContextBase httpContext)
        {
            int result = 0;
            foreach (var item in db.nhomTaiKhoans.ToList())
            {
                if (temp.Equals(item.tenNhom))
                {
                    result = item.maNhom;
                }
            }
            return result;
        }
    }
}