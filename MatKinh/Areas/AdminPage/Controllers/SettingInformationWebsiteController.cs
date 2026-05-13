using System;
using System.Data.Entity;
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
        [ValidateInput(false)]
        public ActionResult ChangeInformation(AdminWebsiteSettingVm model)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            NormalizeWebsiteSetting(model);

            if (!ModelState.IsValid)
            {
                var baseModel = BuildViewModel();

                model.CurrentInfo = baseModel.CurrentInfo;
                model.History = baseModel.History;

                return View("Index", model);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var oldActiveRecords = db.ThongTinCuaHangs
                        .Where(x => x.IsActive)
                        .ToList();

                    foreach (var old in oldActiveRecords)
                    {
                        old.IsActive = false;
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
                        IsActive = true,
                        UpdatedById = currentUser.TaiKhoanId,
                        UpdatedAt = DateTime.Now
                    };

                    db.ThongTinCuaHangs.Add(entity);
                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Cập nhật thông tin website thành công.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    var baseModel = BuildViewModel();
                    model.CurrentInfo = baseModel.CurrentInfo;
                    model.History = baseModel.History;

                    ModelState.AddModelError("", "Cập nhật thông tin website thất bại: " + ex.Message);
                    return View("Index", model);
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteHistoryChange(int id)
        {
            var item = db.ThongTinCuaHangs.FirstOrDefault(x => x.ThongTinCuaHangId == id);
            if (item == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy lịch sử thay đổi.";
                return RedirectToAction("Index");
            }

            if (item.IsActive)
            {
                TempData["ErrorMessage"] = "Không thể xóa thông tin website đang được sử dụng.";
                return RedirectToAction("Index");
            }

            db.ThongTinCuaHangs.Remove(item);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã xóa lịch sử thay đổi.";
            return RedirectToAction("Index");
        }

        private AdminWebsiteSettingVm BuildViewModel()
        {
            var history = db.ThongTinCuaHangs
                .Include(x => x.TaiKhoan)
                .OrderByDescending(x => x.UpdatedAt)
                .ToList();

            var latest = history
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefault()
                ?? history.FirstOrDefault();

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

        private void NormalizeWebsiteSetting(AdminWebsiteSettingVm model)
        {
            if (model == null)
            {
                return;
            }

            model.TenCuaHang = string.IsNullOrWhiteSpace(model.TenCuaHang) ? string.Empty : model.TenCuaHang.Trim();
            model.Hotline = string.IsNullOrWhiteSpace(model.Hotline) ? null : model.Hotline.Trim();
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            model.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
            model.MoTaNgan = string.IsNullOrWhiteSpace(model.MoTaNgan) ? null : model.MoTaNgan.Trim();
            model.GioiThieu = string.IsNullOrWhiteSpace(model.GioiThieu) ? null : model.GioiThieu.Trim();
            model.Logo = string.IsNullOrWhiteSpace(model.Logo) ? null : model.Logo.Trim();
            model.Banner = string.IsNullOrWhiteSpace(model.Banner) ? null : model.Banner.Trim();
            model.FacebookUrl = string.IsNullOrWhiteSpace(model.FacebookUrl) ? null : model.FacebookUrl.Trim();
            model.InstagramUrl = string.IsNullOrWhiteSpace(model.InstagramUrl) ? null : model.InstagramUrl.Trim();
            model.ZaloUrl = string.IsNullOrWhiteSpace(model.ZaloUrl) ? null : model.ZaloUrl.Trim();
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