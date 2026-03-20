using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MatKinh.Models
{
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext == null)
            {
                return false;
            }

            var sessionAccount = httpContext.Session["LoginInformation"] as TaiKhoan;
            if (sessionAccount == null)
            {
                return false;
            }

            using (var db = new BanMatKinhEntities())
            {
                var account = db.TaiKhoans
                    .Include("VaiTro")
                    .FirstOrDefault(x => x.TaiKhoanId == sessionAccount.TaiKhoanId && x.IsActive);

                if (account == null || account.VaiTro == null)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Roles))
                {
                    return true;
                }

                var allowedRoles = Roles
                    .Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                string currentRoleCode = (account.VaiTro.MaVaiTro ?? string.Empty).Trim();
                string currentRoleName = (account.VaiTro.TenVaiTro ?? string.Empty).Trim();

                return allowedRoles.Any(role =>
                    string.Equals(role, currentRoleCode, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(role, currentRoleName, StringComparison.OrdinalIgnoreCase));
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Session["LoginInformation"] == null)
            {
                filterContext.Result = new RedirectResult("~/Account/LoginAccount");
                return;
            }

            filterContext.Result = new RedirectResult("~/Error/Index");
        }
    }
}