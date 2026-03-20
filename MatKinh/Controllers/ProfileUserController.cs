using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.USER)]
    public class ProfileUserController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int PageSize = 5;

        public ActionResult Profile(int page = 1)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            if (page <= 0)
            {
                page = 1;
            }

            IQueryable<DonHang> orderQuery = db.DonHangs
                .AsNoTracking()
                .Where(x => x.CreatedById == currentAccount.TaiKhoanId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.DonHangId);

            BuildOrderPagination(orderQuery, page);

            return View(currentAccount);
        }

        /// <summary>
        /// Cập nhật thông tin tài khoản hiện tại
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInformationAccount(string idAccount, UpdateAccount uda)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            if (uda == null)
            {
                TempData["ProfileError"] = "Dữ liệu cập nhật không hợp lệ.";
                return RedirectToAction("Profile");
            }

            var account = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == currentAccount.TaiKhoanId);
            if (account == null)
            {
                TempData["ProfileError"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Profile");
            }

            string fullName = BuildFullName(uda.LastName, uda.FirstName);

            account.HoTen = fullName;
            account.DiaChi = string.IsNullOrWhiteSpace(uda.Address) ? account.DiaChi : uda.Address.Trim();
            account.SoDienThoai = string.IsNullOrWhiteSpace(uda.Mobile) ? account.SoDienThoai : uda.Mobile.Trim();
            account.GioiTinh = ParseGender(uda.Sex);
            account.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            Session["LoginInformation"] = account;
            TempData["ProfileSuccess"] = "Cập nhật thông tin tài khoản thành công.";

            return RedirectToAction("Profile");
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản hiện tại
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePasswordAccount(string idAccount, string passwdCurrent, UpdateAccount uda)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            var account = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == currentAccount.TaiKhoanId);
            if (account == null)
            {
                TempData["ProfileError"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Profile");
            }

            string currentPasswordHash = HashPassword.SHA512HashPass(passwdCurrent ?? string.Empty);
            if (!string.Equals(account.MatKhauHash, currentPasswordHash, StringComparison.Ordinal))
            {
                TempData["PasswordError"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("Profile");
            }

            if (uda == null || string.IsNullOrWhiteSpace(uda.PassWord))
            {
                TempData["PasswordError"] = "Mật khẩu mới không hợp lệ.";
                return RedirectToAction("Profile");
            }

            account.MatKhauHash = HashPassword.SHA512HashPass(uda.PassWord.Trim());
            account.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            Session["LoginInformation"] = account;
            TempData["PasswordSuccess"] = "Đổi mật khẩu thành công.";

            return RedirectToAction("Profile");
        }

        /// <summary>
        /// Chi tiết đơn hàng của user hiện tại
        /// </summary>
        public ActionResult DetailPurchaseOrder(string maDonHang)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            if (string.IsNullOrWhiteSpace(maDonHang))
            {
                return RedirectToAction("Profile");
            }

            maDonHang = maDonHang.Trim();

            DonHang order = db.DonHangs
                .Include(x => x.KhachHang)
                .FirstOrDefault(x => x.MaDonHang == maDonHang && x.CreatedById == currentAccount.TaiKhoanId);

            if (order == null)
            {
                TempData["ProfileError"] = "Không tìm thấy đơn hàng phù hợp.";
                return RedirectToAction("Profile");
            }

            ViewData["listOfProductInOrder"] = db.ChiTietDonHangs
                .Where(x => x.DonHangId == order.DonHangId)
                .OrderBy(x => x.ChiTietDonHangId)
                .ToList();

            return View(order);
        }

        private void BuildOrderPagination(IQueryable<DonHang> orderQuery, int page)
        {
            int totalCount = orderQuery.Count();
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);

            if (totalPages <= 0)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            List<DonHang> orders = orderQuery
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewData["listOrderUser"] = orders;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.NoOfPages = page >= 5
                ? ((page + 4 > totalPages) ? totalPages : (page + 4))
                : totalPages;

            ViewBag.DisplayPage = page < 5
                ? 0
                : (((page - 1) >= (totalPages - 5)) ? Math.Max(totalPages - 5, 0) : (page - 1));
        }

        private TaiKhoan GetCurrentAccount()
        {
            if (Session["LoginInformation"] == null)
            {
                return null;
            }

            var account = Session["LoginInformation"] as TaiKhoan;
            if (account == null)
            {
                return null;
            }

            return db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == account.TaiKhoanId);
        }

        private string BuildFullName(string lastName, string firstName)
        {
            string fullName = string.Format("{0} {1}",
                string.IsNullOrWhiteSpace(lastName) ? "" : lastName.Trim(),
                string.IsNullOrWhiteSpace(firstName) ? "" : firstName.Trim()).Trim();

            return string.IsNullOrWhiteSpace(fullName) ? "Người dùng" : fullName;
        }

        private bool? ParseGender(string sex)
        {
            if (string.IsNullOrWhiteSpace(sex))
            {
                return null;
            }

            string value = sex.Trim().ToLowerInvariant();

            if (value == "nam")
            {
                return true;
            }

            if (value == "nữ" || value == "nu")
            {
                return false;
            }

            return null;
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