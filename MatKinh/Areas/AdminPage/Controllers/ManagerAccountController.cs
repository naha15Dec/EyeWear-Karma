using System;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN)]
    public class ManagerAccountController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult AccountManager(string keyword = "", string role = "")
        {
            var model = BuildIndexViewModel(keyword, role);
            return View(model);
        }

        [HttpGet]
        public ActionResult FindAccountByUsername(string username, string role = "")
        {
            var model = BuildIndexViewModel(username, role);
            return View("AccountManager", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DisableAccount(int id, string keyword = "", string role = "")
        {
            var account = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == id);
            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("AccountManager", new { keyword, role });
            }

            var currentLogin = Session["LoginInformation"] as TaiKhoan;
            if (currentLogin != null && currentLogin.TaiKhoanId == account.TaiKhoanId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản đang đăng nhập.";
                return RedirectToAction("AccountManager", new { keyword, role });
            }

            account.IsActive = !account.IsActive;
            account.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = account.IsActive
                ? "Đã mở khóa tài khoản."
                : "Đã khóa tài khoản.";

            return RedirectToAction("AccountManager", new { keyword, role });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAccount(int id, string keyword = "", string role = "")
        {
            var account = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == id);
            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("AccountManager", new { keyword, role });
            }

            var currentLogin = Session["LoginInformation"] as TaiKhoan;
            if (currentLogin != null && currentLogin.TaiKhoanId == account.TaiKhoanId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự xóa tài khoản đang đăng nhập.";
                return RedirectToAction("AccountManager", new { keyword, role });
            }

            // Hệ thống vận hành nên ưu tiên khóa thay vì xóa cứng
            account.IsActive = false;
            account.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Tài khoản đã được vô hiệu hóa.";
            return RedirectToAction("AccountManager", new { keyword, role });
        }

        [HttpGet]
        public ActionResult DetailAccountUser(int id)
        {
            var account = db.TaiKhoans
                .Include("VaiTro")
                .FirstOrDefault(x => x.TaiKhoanId == id);

            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("AccountManager");
            }

            var model = BuildDetailViewModel(account);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInformationAccount(AdminAccountUpdateVm model)
        {
            var account = db.TaiKhoans
                .Include("VaiTro")
                .FirstOrDefault(x => x.TaiKhoanId == model.TaiKhoanId);

            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("AccountManager");
            }

            if (!ModelState.IsValid)
            {
                var detailModel = BuildDetailViewModel(account);
                return View("DetailAccountUser", detailModel);
            }

            account.HoTen = model.HoTen?.Trim();
            account.Email = model.Email?.Trim();
            account.SoDienThoai = model.SoDienThoai?.Trim();
            account.DiaChi = model.DiaChi?.Trim();
            account.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công.";
            return RedirectToAction("DetailAccountUser", new { id = model.TaiKhoanId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(AdminAccountChangePasswordVm model)
        {
            var account = db.TaiKhoans
                .Include("VaiTro")
                .FirstOrDefault(x => x.TaiKhoanId == model.TaiKhoanId);

            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("AccountManager");
            }

            if (!ModelState.IsValid)
            {
                var detailModel = BuildDetailViewModel(account);
                return View("DetailAccountUser", detailModel);
            }

            account.MatKhauHash = HashPassword.SHA512HashPass(model.NewPassword);
            account.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("DetailAccountUser", new { id = model.TaiKhoanId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePermisstionAccount(AdminAccountUpdateRoleVm model)
        {
            var account = db.TaiKhoans
                .Include("VaiTro")
                .FirstOrDefault(x => x.TaiKhoanId == model.TaiKhoanId);

            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("AccountManager");
            }

            var role = db.VaiTroes.FirstOrDefault(x => x.VaiTroId == model.VaiTroId);
            if (role == null)
            {
                TempData["ErrorMessage"] = "Vai trò không tồn tại.";
                return RedirectToAction("DetailAccountUser", new { id = model.TaiKhoanId });
            }

            var currentLogin = Session["LoginInformation"] as TaiKhoan;
            if (currentLogin != null && currentLogin.TaiKhoanId == account.TaiKhoanId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự đổi quyền của chính mình.";
                return RedirectToAction("DetailAccountUser", new { id = model.TaiKhoanId });
            }

            account.VaiTroId = role.VaiTroId;
            account.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật phân quyền thành công.";
            return RedirectToAction("DetailAccountUser", new { id = model.TaiKhoanId });
        }

        private AdminAccountIndexVm BuildIndexViewModel(string keyword, string role)
        {
            keyword = (keyword ?? string.Empty).Trim();
            role = (role ?? string.Empty).Trim().ToUpper();

            var query = db.TaiKhoans.Include("VaiTro").AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.TenDangNhap.Contains(keyword) ||
                    x.HoTen.Contains(keyword) ||
                    x.Email.Contains(keyword) ||
                    x.SoDienThoai.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(x => x.VaiTro.MaVaiTro == role);
            }

            var model = new AdminAccountIndexVm
            {
                Keyword = keyword,
                RoleFilter = role,
                HeaderTitle = "Quản lý tài khoản",
                Accounts = query
                    .OrderBy(x => x.VaiTro.MaVaiTro)
                    .ThenBy(x => x.TenDangNhap)
                    .Select(x => new AdminAccountListItemVm
                    {
                        TaiKhoanId = x.TaiKhoanId,
                        TenDangNhap = x.TenDangNhap,
                        HoTen = x.HoTen,
                        Email = x.Email,
                        SoDienThoai = x.SoDienThoai,
                        DiaChi = x.DiaChi,
                        VaiTroId = x.VaiTroId,
                        MaVaiTro = x.VaiTro.MaVaiTro,
                        TenVaiTro = x.VaiTro.TenVaiTro,
                        IsActive = x.IsActive,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt
                    })
                    .ToList(),
                Roles = db.VaiTroes
                    .OrderBy(x => x.VaiTroId)
                    .Select(x => new SelectListItem
                    {
                        Value = x.MaVaiTro,
                        Text = x.TenVaiTro
                    })
                    .ToList()
            };

            return model;
        }

        private AdminAccountDetailVm BuildDetailViewModel(TaiKhoan account)
        {
            return new AdminAccountDetailVm
            {
                TaiKhoanId = account.TaiKhoanId,
                TenDangNhap = account.TenDangNhap,
                HoTen = account.HoTen,
                Email = account.Email,
                SoDienThoai = account.SoDienThoai,
                DiaChi = account.DiaChi,
                VaiTroId = account.VaiTroId,
                MaVaiTro = account.VaiTro != null ? account.VaiTro.MaVaiTro : string.Empty,
                TenVaiTro = account.VaiTro != null ? account.VaiTro.TenVaiTro : string.Empty,
                IsActive = account.IsActive,
                Roles = db.VaiTroes
                    .OrderBy(x => x.VaiTroId)
                    .Select(x => new SelectListItem
                    {
                        Value = x.VaiTroId.ToString(),
                        Text = x.TenVaiTro
                    })
                    .ToList()
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