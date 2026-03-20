using System.Web;
using System.Web.Mvc;

namespace MatKinh.Models
{
    public class CustomAuthenticationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsUserAuthenticated())
            {
                filterContext.Result = new RedirectResult("/Account/LoginAccount");
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        private bool IsUserAuthenticated()
        {
            return HttpContext.Current.Session["LoginInformation"] != null;
        }
    }
}