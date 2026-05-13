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
    public class ManagerAccountController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();
        private const int PAGE_SIZE = 10;

        [HttpGet]
        public ActionResult AccountManager(string keyword = "", string role = "", int page = 1)
        {
            var model = BuildIndexViewModel(keyword, role, page);
            return View(model);
        }

        [HttpGet]
        public ActionResult FindAccountByUsername(string username, string role = "", int page = 1)
        {
            var model = BuildIndexViewModel(username, role, page);
            return View("AccountManager", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DisableAccount(int id, string keyword = "", string role = "", int page = 1)
        {
            var account = db.TaiKhoans
                .Include(x => x.VaiTro)
                .FirstOrDefault(x => x.TaiKhoanId == id);

            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("AccountManager", new { keyword, role, page });
            }

            var currentLogin = Session["LoginInformation"] as TaiKhoan;
            if (currentLogin != null && currentLogin.TaiKhoanId == account.TaiKhoanId)
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản đang đăng nhập.";
                return RedirectToAction("AccountManager", new { keyword, role, page });
            }

            if (account.IsActive && IsLastActiveAdmin(account))
            {
                TempData["ErrorMessage"] = "Không thể khóa tài khoản admin cuối cùng đang hoạt động.";
                return RedirectToAction("AccountManager", new { keyword, role, page });
            }

            account.IsActive = !account.IsActive;
            account.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = account.IsActive
                ? "Đã mở khóa tài khoản."
                : "Đã khóa tài khoản.";

            return RedirectToAction("AccountManager", new { keyword, role, page });
        }

        [HttpGet]
        public ActionResult DetailAccountUser(int id)
        {
            var account = db.TaiKhoans
                .Include(x => x.VaiTro)
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
                .Include(x => x.VaiTro)
                .FirstOrDefault(x => x.TaiKhoanId == model.TaiKhoanId);

            if (account == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("AccountManager");
            }

            NormalizeUpdateModel(model);
            ValidateUpdateInformation(model);

            if (!ModelState.IsValid)
            {
                var detailModel = BuildDetailViewModel(account);
                detailModel.HoTen = model.HoTen;
                detailModel.Email = model.Email;
                detailModel.SoDienThoai = model.SoDienThoai;
                detailModel.DiaChi = model.DiaChi;

                return View("DetailAccountUser", detailModel);
            }

            account.HoTen = model.HoTen;
            account.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email;
            account.SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai;
            account.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi;
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
                .Include(x => x.VaiTro)
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

            account.MatKhauHash = HashPassword.SHA512HashPass(model.NewPassword.Trim());
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
                .Include(x => x.VaiTro)
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

            bool isChangingAdminToOtherRole =
                IsAdminAccount(account) &&
                !string.Equals(role.MaVaiTro, RoleConstants.ADMIN, StringComparison.OrdinalIgnoreCase);

            if (isChangingAdminToOtherRole && IsLastActiveAdmin(account))
            {
                TempData["ErrorMessage"] = "Không thể hạ quyền tài khoản admin cuối cùng đang hoạt động.";
                return RedirectToAction("DetailAccountUser", new { id = model.TaiKhoanId });
            }

            account.VaiTroId = role.VaiTroId;
            account.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật phân quyền thành công.";
            return RedirectToAction("DetailAccountUser", new { id = model.TaiKhoanId });
        }

        private AdminAccountIndexVm BuildIndexViewModel(string keyword, string role, int page)
        {
            keyword = (keyword ?? string.Empty).Trim();
            role = (role ?? string.Empty).Trim().ToUpper();

            if (page <= 0)
            {
                page = 1;
            }

            var query = db.TaiKhoans
                .Include(x => x.VaiTro)
                .AsQueryable();

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

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);

            if (totalPages <= 0)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var accounts = query
                .OrderBy(x => x.VaiTro.MaVaiTro)
                .ThenBy(x => x.TenDangNhap)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
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
                .ToList();

            return new AdminAccountIndexVm
            {
                Keyword = keyword,
                RoleFilter = role,
                HeaderTitle = "Quản lý tài khoản",
                Accounts = accounts,
                Roles = db.VaiTroes
                    .OrderBy(x => x.VaiTroId)
                    .Select(x => new SelectListItem
                    {
                        Value = x.MaVaiTro,
                        Text = x.TenVaiTro
                    })
                    .ToList(),
                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
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

        private void NormalizeUpdateModel(AdminAccountUpdateVm model)
        {
            model.HoTen = string.IsNullOrWhiteSpace(model.HoTen) ? string.Empty : model.HoTen.Trim();
            model.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            model.SoDienThoai = string.IsNullOrWhiteSpace(model.SoDienThoai) ? null : model.SoDienThoai.Trim();
            model.DiaChi = string.IsNullOrWhiteSpace(model.DiaChi) ? null : model.DiaChi.Trim();
        }

        private void ValidateUpdateInformation(AdminAccountUpdateVm model)
        {
            if (string.IsNullOrWhiteSpace(model.HoTen))
            {
                ModelState.AddModelError("HoTen", "Vui lòng nhập họ tên.");
            }

            bool duplicatedEmail = !string.IsNullOrWhiteSpace(model.Email) &&
                db.TaiKhoans.Any(x =>
                    x.TaiKhoanId != model.TaiKhoanId &&
                    x.Email == model.Email);

            if (duplicatedEmail)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng bởi tài khoản khác.");
            }

            bool duplicatedPhone = !string.IsNullOrWhiteSpace(model.SoDienThoai) &&
                db.TaiKhoans.Any(x =>
                    x.TaiKhoanId != model.TaiKhoanId &&
                    x.SoDienThoai == model.SoDienThoai);

            if (duplicatedPhone)
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại đã được sử dụng bởi tài khoản khác.");
            }
        }

        private bool IsAdminAccount(TaiKhoan account)
        {
            return account != null &&
                   account.VaiTro != null &&
                   string.Equals(account.VaiTro.MaVaiTro, RoleConstants.ADMIN, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsLastActiveAdmin(TaiKhoan account)
        {
            if (!IsAdminAccount(account))
            {
                return false;
            }

            return db.TaiKhoans.Count(x =>
                x.IsActive &&
                x.VaiTro.MaVaiTro == RoleConstants.ADMIN) <= 1;
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