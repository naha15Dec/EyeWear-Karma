using System;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN)]
    public class SettingInformationWebsiteController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult Index()
        {
            var model = BuildViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeInformation(AdminWebsiteSettingVm model)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = BuildViewModel();
                return View("Index", invalidModel);
            }

            var entity = new ThongTinCuaHang
            {
                TenCuaHang = model.TenCuaHang,
                Hotline = model.Hotline,
                Email = model.Email,
                DiaChi = model.DiaChi,
                MoTaNgan = model.MoTaNgan,
                GioiThieu = model.GioiThieu,
                Logo = model.Logo,
                Banner = model.Banner,
                FacebookUrl = model.FacebookUrl,
                InstagramUrl = model.InstagramUrl,
                ZaloUrl = model.ZaloUrl,
                IsActive = model.IsActive,
                UpdatedById = currentUser.TaiKhoanId,
                UpdatedAt = DateTime.Now
            };

            db.ThongTinCuaHangs.Add(entity);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật thông tin website thành công.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteHistoryChange(int id)
        {
            var item = db.ThongTinCuaHangs.FirstOrDefault(x => x.ThongTinCuaHangId == id);
            if (item != null)
            {
                db.ThongTinCuaHangs.Remove(item);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        private AdminWebsiteSettingVm BuildViewModel()
        {
            var history = db.ThongTinCuaHangs
                .OrderByDescending(x => x.UpdatedAt)
                .ToList();

            var latest = history.FirstOrDefault();

            return new AdminWebsiteSettingVm
            {
                CurrentInfo = latest,
                History = history,

                TenCuaHang = latest != null ? latest.TenCuaHang : string.Empty,
                Hotline = latest != null ? latest.Hotline : string.Empty,
                Email = latest != null ? latest.Email : string.Empty,
                DiaChi = latest != null ? latest.DiaChi : string.Empty,
                MoTaNgan = latest != null ? latest.MoTaNgan : string.Empty,
                GioiThieu = latest != null ? latest.GioiThieu : string.Empty,
                Logo = latest != null ? latest.Logo : string.Empty,
                Banner = latest != null ? latest.Banner : string.Empty,
                FacebookUrl = latest != null ? latest.FacebookUrl : string.Empty,
                InstagramUrl = latest != null ? latest.InstagramUrl : string.Empty,
                ZaloUrl = latest != null ? latest.ZaloUrl : string.Empty,
                IsActive = latest != null && latest.IsActive
            };
        }

        private TaiKhoan GetCurrentAccount()
        {
            var sessionAccount = Session["LoginInformation"] as TaiKhoan;
            if (sessionAccount == null)
            {
                return null;
            }

            return db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == sessionAccount.TaiKhoanId && x.IsActive);
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