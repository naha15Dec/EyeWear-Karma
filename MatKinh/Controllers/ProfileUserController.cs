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
        /// Cập nhật thông tin tài khoản hiện tại.
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

            var account = db.TaiKhoans.FirstOrDefault(x =>
                x.TaiKhoanId == currentAccount.TaiKhoanId &&
                x.IsActive);

            if (account == null)
            {
                TempData["ProfileError"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Profile");
            }

            string fullName = BuildFullName(uda.LastName, uda.FirstName);
            string oldEmail = account.Email;
            string oldPhone = account.SoDienThoai;

            account.HoTen = fullName;

            if (!string.IsNullOrWhiteSpace(uda.Address))
            {
                account.DiaChi = uda.Address.Trim();
            }

            if (!string.IsNullOrWhiteSpace(uda.Mobile))
            {
                account.SoDienThoai = uda.Mobile.Trim();
            }

            account.GioiTinh = ParseGender(uda.Sex);
            account.UpdatedAt = DateTime.Now;

            SyncCustomerFromAccount(account, oldEmail, oldPhone);

            db.SaveChanges();

            Session["LoginInformation"] = account;
            TempData["ProfileSuccess"] = "Cập nhật thông tin tài khoản thành công.";

            return RedirectToAction("Profile");
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản hiện tại.
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

            var account = db.TaiKhoans.FirstOrDefault(x =>
                x.TaiKhoanId == currentAccount.TaiKhoanId &&
                x.IsActive);

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

            if (!string.IsNullOrWhiteSpace(uda.ComfirmPassword) &&
                !string.Equals(uda.PassWord.Trim(), uda.ComfirmPassword.Trim(), StringComparison.Ordinal))
            {
                TempData["PasswordError"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction("Profile");
            }

            if (uda.PassWord.Trim().Length < 6)
            {
                TempData["PasswordError"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
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
        /// Chi tiết đơn hàng của user hiện tại.
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
                .Include(x => x.LichSuTrangThaiDonHangs.Select(ls => ls.TaiKhoan))
                .FirstOrDefault(x =>
                    x.MaDonHang == maDonHang &&
                    x.CreatedById == currentAccount.TaiKhoanId);

            if (order == null)
            {
                TempData["ProfileError"] = "Không tìm thấy đơn hàng phù hợp.";
                return RedirectToAction("Profile");
            }

            ViewData["listOfProductInOrder"] = db.ChiTietDonHangs
                .Include(x => x.SanPham)
                .Where(x => x.DonHangId == order.DonHangId)
                .OrderBy(x => x.ChiTietDonHangId)
                .ToList();

            ViewData["orderHistories"] = db.LichSuTrangThaiDonHangs
                .Include(x => x.TaiKhoan)
                .Where(x => x.DonHangId == order.DonHangId)
                .OrderBy(x => x.CreatedAt)
                .ToList();

            ViewBag.CanCancelOrder = CanCustomerCancelOrder(order);

            return View(order);
        }

        /// <summary>
        /// Khách hàng hủy đơn hàng của chính mình.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelPurchaseOrder(string maDonHang, string lyDoHuy)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            if (string.IsNullOrWhiteSpace(maDonHang))
            {
                TempData["ProfileError"] = "Mã đơn hàng không hợp lệ.";
                return RedirectToAction("Profile");
            }

            maDonHang = maDonHang.Trim();

            DonHang order = db.DonHangs
                .Include(x => x.ChiTietDonHangs)
                .FirstOrDefault(x =>
                    x.MaDonHang == maDonHang &&
                    x.CreatedById == currentAccount.TaiKhoanId);

            if (order == null)
            {
                TempData["ProfileError"] = "Không tìm thấy đơn hàng phù hợp.";
                return RedirectToAction("Profile");
            }

            if (!CanCustomerCancelOrder(order))
            {
                TempData["ProfileError"] = "Đơn hàng này không thể hủy ở trạng thái hiện tại. Nếu cần hỗ trợ, vui lòng liên hệ cửa hàng.";
                return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = order.TrangThai;

                    bool isPaidVnPayOrder =
                        string.Equals(order.PhuongThucThanhToan, PaymentConstants.VNPAY, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(order.TrangThaiThanhToan, PaymentConstants.PAID, StringComparison.OrdinalIgnoreCase);

                    order.TrangThai = OrderStatusConstants.CANCELLED;
                    order.NgayHuy = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;

                    // Không tự động set Refunded vì hệ thống chưa xử lý hoàn tiền thật qua VNPAY.
                    // Nếu là đơn VNPAY đã thanh toán, vẫn giữ trạng thái Paid và ghi chú để shop xử lý hoàn tiền.
                    RestoreStockForOrder(order);

                    string cancelNote = string.IsNullOrWhiteSpace(lyDoHuy)
                        ? "Khách hàng hủy đơn hàng."
                        : "Khách hàng hủy đơn hàng. Lý do: " + lyDoHuy.Trim();

                    if (isPaidVnPayOrder)
                    {
                        cancelNote += " Đơn hàng đã thanh toán qua VNPay, cửa hàng sẽ kiểm tra và xử lý hoàn tiền trong 3–5 ngày làm việc.";
                    }

                    db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
                    {
                        DonHangId = order.DonHangId,
                        TrangThaiCu = oldStatus,
                        TrangThaiMoi = OrderStatusConstants.CANCELLED,
                        ThayDoiBoiId = currentAccount.TaiKhoanId,
                        GhiChu = cancelNote,
                        CreatedAt = DateTime.Now
                    });

                    db.SaveChanges();
                    transaction.Commit();

                    if (isPaidVnPayOrder)
                    {
                        TempData["ProfileSuccess"] = "Đã hủy đơn hàng thành công. Đơn hàng đã thanh toán qua VNPay, cửa hàng sẽ xử lý hoàn tiền trong 3–5 ngày làm việc.";
                    }
                    else
                    {
                        TempData["ProfileSuccess"] = "Đã hủy đơn hàng thành công.";
                    }

                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ProfileError"] = "Hủy đơn hàng thất bại: " + ex.Message;
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }
            }
        }

        private bool CanCustomerCancelOrder(DonHang order)
        {
            if (order == null)
            {
                return false;
            }

            // Khách chỉ được tự hủy khi đơn còn chờ xác nhận.
            // Các trạng thái đã xác nhận/chuẩn bị/giao hàng nên để shop hỗ trợ.
            return order.TrangThai == OrderStatusConstants.PENDING;
        }

        private void RestoreStockForOrder(DonHang order)
        {
            if (order == null || order.ChiTietDonHangs == null)
            {
                return;
            }

            foreach (var item in order.ChiTietDonHangs)
            {
                SanPham product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == item.SanPhamId);
                if (product != null)
                {
                    product.SoLuongTon += item.SoLuong;
                    product.UpdatedAt = DateTime.Now;
                }
            }
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

            return db.TaiKhoans.FirstOrDefault(x =>
                x.TaiKhoanId == account.TaiKhoanId &&
                x.IsActive);
        }

        private void SyncCustomerFromAccount(TaiKhoan account, string oldEmail, string oldPhone)
        {
            if (account == null)
            {
                return;
            }

            KhachHang customer = null;

            if (!string.IsNullOrWhiteSpace(oldEmail))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.Email == oldEmail);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(oldPhone))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.SoDienThoai == oldPhone);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(account.Email))
            {
                string email = account.Email.Trim();

                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.Email == email);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(account.SoDienThoai))
            {
                string phone = account.SoDienThoai.Trim();

                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.SoDienThoai == phone);
            }

            if (customer == null)
            {
                return;
            }

            customer.HoTen = account.HoTen;
            customer.Email = account.Email;
            customer.SoDienThoai = account.SoDienThoai;
            customer.GioiTinh = account.GioiTinh;
            customer.NgaySinh = account.NgaySinh;
            customer.DiaChi = account.DiaChi;
            customer.UpdatedAt = DateTime.Now;

            Session["KhachHangId"] = customer.KhachHangId;
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