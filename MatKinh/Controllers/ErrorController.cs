using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;

namespace MatKinh.Controllers
{
    [CustomAuthentication]
    public class ErrorController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        public ActionResult Index()
        {
            Response.StatusCode = 403;
            Response.TrySkipIisCustomErrors = true;

            TaiKhoan sessionAccount = Session["LoginInformation"] as TaiKhoan;
            string roleCode = string.Empty;

            if (sessionAccount != null)
            {
                var account = db.TaiKhoans
                    .AsNoTracking()
                    .Include(x => x.VaiTro)
                    .FirstOrDefault(x =>
                        x.TaiKhoanId == sessionAccount.TaiKhoanId &&
                        x.IsActive);

                if (account != null && account.VaiTro != null)
                {
                    roleCode = (account.VaiTro.MaVaiTro ?? string.Empty)
                        .Trim()
                        .ToUpperInvariant();
                }
            }

            bool isAdminLike =
                roleCode == RoleConstants.ADMIN ||
                roleCode == RoleConstants.STAFF ||
                roleCode == RoleConstants.SHIPPER;

            ViewBag.IsAdminLike = isAdminLike;

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}