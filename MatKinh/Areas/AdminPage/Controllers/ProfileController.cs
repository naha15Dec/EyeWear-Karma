using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN + "," + RoleConstants.STAFF)]
    public class ProfileController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult Index()
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var model = BuildProfileViewModel(currentUser);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInformationAccount(AdminProfileUpdateVm model)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = BuildProfileViewModel(currentUser);
                return View("Index", invalidModel);
            }

            currentUser.HoTen = (model.HoTen ?? string.Empty).Trim();
            currentUser.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            currentUser.SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim();
            currentUser.GioiTinh = model.GioiTinh;
            currentUser.NgaySinh = model.NgaySinh;
            currentUser.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
            currentUser.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            UpdateLoginSession(currentUser);

            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePasswordAccount(AdminProfileChangePasswordVm model)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = BuildProfileViewModel(currentUser);
                return View("Index", invalidModel);
            }

            string currentPasswordHash = HashPassword.SHA512HashPass(model.CurrentPassword);
            if (!string.Equals(currentUser.MatKhauHash, currentPasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("Index");
            }

            currentUser.MatKhauHash = HashPassword.SHA512HashPass(model.NewPassword);
            currentUser.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            UpdateLoginSession(currentUser);

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Index");
        }

        private TaiKhoan GetCurrentAccount()
        {
            var sessionAccount = Session["LoginInformation"] as TaiKhoan;
            if (sessionAccount == null)
            {
                return null;
            }

            return db.TaiKhoans
                .Include(x => x.VaiTro)
                .FirstOrDefault(x => x.TaiKhoanId == sessionAccount.TaiKhoanId && x.IsActive);
        }

        private AdminProfileVm BuildProfileViewModel(TaiKhoan account)
        {
            return new AdminProfileVm
            {
                TaiKhoanId = account.TaiKhoanId,
                TenDangNhap = account.TenDangNhap,
                HoTen = account.HoTen,
                Email = account.Email,
                SoDienThoai = account.SoDienThoai,
                GioiTinh = account.GioiTinh,
                NgaySinh = account.NgaySinh,
                DiaChi = account.DiaChi,
                AnhDaiDien = account.AnhDaiDien,
                TenVaiTro = account.VaiTro != null ? account.VaiTro.TenVaiTro : string.Empty,
                IsActive = account.IsActive
            };
        }

        private void UpdateLoginSession(TaiKhoan account)
        {
            Session["LoginInformation"] = new TaiKhoan
            {
                TaiKhoanId = account.TaiKhoanId,
                VaiTroId = account.VaiTroId,
                TenDangNhap = account.TenDangNhap,
                MatKhauHash = account.MatKhauHash,
                HoTen = account.HoTen,
                Email = account.Email,
                SoDienThoai = account.SoDienThoai,
                GioiTinh = account.GioiTinh,
                NgaySinh = account.NgaySinh,
                DiaChi = account.DiaChi,
                AnhDaiDien = account.AnhDaiDien,
                IsActive = account.IsActive,
                LastLoginAt = account.LastLoginAt,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt,
                VaiTro = account.VaiTro
            };
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