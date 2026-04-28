using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BanGiay.Models
{
    public class CustomAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //Kiểm tra người dùng đã đăng nhập chưa nếu chưa thì sẽ chuyển hướng về trang đăng nhập
            if (!IsUserAuthenticated())
            {
                filterContext.Result = new RedirectResult("/Account/LoginAccount");
            }
            base.OnActionExecuting(filterContext);
        }

        private bool IsUserAuthenticated()
        {
            // Thực hiện kiểm tra xem session có null hay không 
            // Nếu session khác null tức là đã có đăng nhập thì trả về true
            if (HttpContext.Current.Session["LoginInformation"] != null)
            {
                return true;
            }
            return false;
        }
    }

}