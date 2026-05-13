using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN + "," + RoleConstants.STAFF + "," + RoleConstants.SHIPPER)]
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

            ViewBag.ActiveTab = "profileInfo";

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

            NormalizeProfileUpdateModel(model);
            ValidateProfileUpdate(model, currentUser.TaiKhoanId);

            if (!ModelState.IsValid)
            {
                ViewBag.ActiveTab = "profileInfo";

                var invalidModel = BuildProfileViewModel(currentUser);
                invalidModel.HoTen = model.HoTen;
                invalidModel.Email = model.Email;
                invalidModel.SoDienThoai = model.SoDienThoai;
                invalidModel.GioiTinh = model.GioiTinh;
                invalidModel.NgaySinh = model.NgaySinh;
                invalidModel.DiaChi = model.DiaChi;

                return View("Index", invalidModel);
            }

            currentUser.HoTen = model.HoTen;
            currentUser.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email;
            currentUser.SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai;
            currentUser.GioiTinh = model.GioiTinh;
            currentUser.NgaySinh = model.NgaySinh;
            currentUser.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi;
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
                ViewBag.ActiveTab = "changePassword";

                var invalidModel = BuildProfileViewModel(currentUser);
                return View("Index", invalidModel);
            }

            string currentPassword = (model.CurrentPassword ?? string.Empty).Trim();
            string newPassword = (model.NewPassword ?? string.Empty).Trim();

            string currentPasswordHash = HashPassword.SHA512HashPass(currentPassword);
            if (!string.Equals(currentUser.MatKhauHash, currentPasswordHash, StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.ActiveTab = "changePassword";

                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");

                var invalidModel = BuildProfileViewModel(currentUser);
                return View("Index", invalidModel);
            }

            currentUser.MatKhauHash = HashPassword.SHA512HashPass(newPassword);
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

        private void NormalizeProfileUpdateModel(AdminProfileUpdateVm model)
        {
            if (model == null)
            {
                return;
            }

            model.HoTen = string.IsNullOrWhiteSpace(model.HoTen) ? string.Empty : model.HoTen.Trim();
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            model.SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim();
            model.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
        }
        private void ValidateProfileUpdate(AdminProfileUpdateVm model, int currentAccountId)
        {
            if (model == null)
            {
                ModelState.AddModelError("", "Dữ liệu cập nhật không hợp lệ.");
                return;
            }

            if (string.IsNullOrWhiteSpace(model.HoTen))
            {
                ModelState.AddModelError("HoTen", "Vui lòng nhập họ tên.");
            }

            if (model.HoTen != null && model.HoTen.Trim().Length > 150)
            {
                ModelState.AddModelError("HoTen", "Họ tên không được vượt quá 150 ký tự.");
            }

            if (!string.IsNullOrWhiteSpace(model.Email))
            {
                string email = model.Email.Trim();

                if (email.Length > 100)
                {
                    ModelState.AddModelError("Email", "Email không được vượt quá 100 ký tự.");
                }

                bool duplicatedEmail = db.TaiKhoans.Any(x =>
                    x.TaiKhoanId != currentAccountId &&
                    x.Email == email);

                if (duplicatedEmail)
                {
                    ModelState.AddModelError("Email", "Email đã được sử dụng bởi tài khoản khác.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.SoDienThoai))
            {
                string phone = model.SoDienThoai.Trim();

                bool isValidPhone = System.Text.RegularExpressions.Regex.IsMatch(
                    phone,
                    @"^(0|\+84)(\d{9})$"
                );

                if (!isValidPhone)
                {
                    ModelState.AddModelError("SoDienThoai", "Số điện thoại không hợp lệ. Ví dụ: 0332080172 hoặc +84332080172.");
                }

                bool duplicatedPhone = db.TaiKhoans.Any(x =>
                    x.TaiKhoanId != currentAccountId &&
                    x.SoDienThoai == phone);

                if (duplicatedPhone)
                {
                    ModelState.AddModelError("SoDienThoai", "Số điện thoại đã được sử dụng bởi tài khoản khác.");
                }
            }

            if (model.NgaySinh.HasValue)
            {
                DateTime today = DateTime.Today;
                DateTime birthDate = model.NgaySinh.Value.Date;

                if (birthDate > today)
                {
                    ModelState.AddModelError("NgaySinh", "Ngày sinh không được lớn hơn ngày hiện tại.");
                }

                if (birthDate < today.AddYears(-120))
                {
                    ModelState.AddModelError("NgaySinh", "Ngày sinh không hợp lệ.");
                }
            }

            if (!string.IsNullOrWhiteSpace(model.DiaChi) && model.DiaChi.Trim().Length > 255)
            {
                ModelState.AddModelError("DiaChi", "Địa chỉ không được vượt quá 255 ký tự.");
            }
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