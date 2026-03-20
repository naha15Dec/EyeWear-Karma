using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Controllers
{
    public class AccountController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        // ========================= LOGIN =========================

        public ActionResult LoginAccount()
        {
            var account = Session["LoginInformation"] as TaiKhoan;
            if (account == null)
            {
                return View();
            }

            return RedirectToHomeByRole(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LoginAccount(Login lvm)
        {
            if (!ModelState.IsValid)
            {
                return View(lvm);
            }

            string username = (lvm.Username ?? string.Empty).Trim();
            string password = lvm.Password ?? string.Empty;
            string passwordHash = HashPassword.SHA512HashPass(password);

            var account = db.TaiKhoans
                .Include("VaiTro")
                .FirstOrDefault(x =>
                    x.TenDangNhap == username &&
                    x.MatKhauHash == passwordHash &&
                    x.IsActive);

            if (account == null)
            {
                ModelState.Clear();

                lvm.Username = string.Empty;
                lvm.Password = string.Empty;
                lvm.RememberMe = false;

                ModelState.AddModelError("Password", "Sai tài khoản hoặc mật khẩu.");
                return View(lvm);
            }

            account.LastLoginAt = DateTime.Now;
            account.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            Session["ShoppingCart"] = null;
            Session["LoginInformation"] = account;

            return RedirectToHomeByRole(account);
        }

        // ========================= REGISTER =========================

        public ActionResult RegisterAccount()
        {
            return View(new Register());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterAccount(Register rvm)
        {
            NormalizeRegisterInput(rvm);

            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(BuildErrorResponse());
                }

                return View(rvm);
            }

            ValidateRegisterBusinessRules(rvm);

            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(BuildErrorResponse());
                }

                return View(rvm);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int customerRoleId = GetCustomerRoleId();

                    string fullName = BuildFullName(rvm.LastName, rvm.FirstName);
                    bool? gioiTinh = ParseGender(rvm.Sex);
                    DateTime? ngaySinh = rvm.DateOfBirth;

                    var customer = GetOrCreateCustomerForRegistration(rvm, fullName, gioiTinh, ngaySinh);

                    var account = new TaiKhoan
                    {
                        VaiTroId = customerRoleId,
                        TenDangNhap = rvm.Username,
                        MatKhauHash = HashPassword.SHA512HashPass(rvm.Password),
                        HoTen = fullName,
                        Email = rvm.Email,
                        SoDienThoai = rvm.Mobile,
                        GioiTinh = gioiTinh,
                        NgaySinh = ngaySinh,
                        DiaChi = string.IsNullOrWhiteSpace(rvm.Address) ? string.Empty : rvm.Address.Trim(),
                        AnhDaiDien = null,
                        IsActive = true,
                        LastLoginAt = null,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = null
                    };

                    db.TaiKhoans.Add(account);
                    db.SaveChanges();

                    transaction.Commit();

                    Session["LoginInformation"] = account;
                    Session["ShoppingCart"] = null;

                    if (Request.IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            redirectUrl = Url.Action("Index", "Home")
                        });
                    }

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Đăng ký tài khoản thất bại: " + ex.Message);

                    if (Request.IsAjaxRequest())
                    {
                        return Json(BuildErrorResponse());
                    }

                    return View(rvm);
                }
            }
        }

        // ========================= LOGOUT =========================

        public ActionResult LogoutAccount()
        {
            Session["LoginInformation"] = null;
            Session["ShoppingCart"] = null;
            Session["LastCreatedOrderId"] = null;
            Session["LastCreatedOrderCode"] = null;

            return RedirectToAction("Index", "Home");
        }

        // ========================= PRIVATE METHODS =========================

        private void NormalizeRegisterInput(Register rvm)
        {
            if (rvm == null) return;

            rvm.Username = (rvm.Username ?? string.Empty).Trim();
            rvm.FirstName = (rvm.FirstName ?? string.Empty).Trim();
            rvm.LastName = (rvm.LastName ?? string.Empty).Trim();
            rvm.Mobile = (rvm.Mobile ?? string.Empty).Trim();
            rvm.Email = (rvm.Email ?? string.Empty).Trim().ToLower();
            rvm.Sex = (rvm.Sex ?? string.Empty).Trim();
        }

        private void ValidateRegisterBusinessRules(Register rvm)
        {
            if (rvm == null)
            {
                ModelState.AddModelError("", "Dữ liệu đăng ký không hợp lệ.");
                return;
            }

            bool usernameExists = db.TaiKhoans.Any(x => x.TenDangNhap == rvm.Username);
            if (usernameExists)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
            }

            bool emailExists = db.TaiKhoans.Any(x => x.Email == rvm.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
            }

            bool phoneExists = db.TaiKhoans.Any(x => x.SoDienThoai == rvm.Mobile);
            if (phoneExists)
            {
                ModelState.AddModelError("Mobile", "Số điện thoại đã được sử dụng.");
            }

            if (rvm.DateOfBirth.HasValue && rvm.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError("DateOfBirth", "Ngày sinh không hợp lệ.");
            }
        }

        private KhachHang GetOrCreateCustomerForRegistration(Register rvm, string fullName, bool? gioiTinh, DateTime? ngaySinh)
        {
            var customer = db.KhachHangs.FirstOrDefault(x => x.SoDienThoai == rvm.Mobile);

            if (customer == null && !string.IsNullOrWhiteSpace(rvm.Email))
            {
                customer = db.KhachHangs.FirstOrDefault(x => x.Email == rvm.Email);
            }

            if (customer == null)
            {
                customer = new KhachHang
                {
                    MaKhachHang = GenerateCustomerCode(),
                    HoTen = fullName,
                    Email = string.IsNullOrWhiteSpace(rvm.Email) ? null : rvm.Email,
                    SoDienThoai = rvm.Mobile,
                    GioiTinh = gioiTinh,
                    NgaySinh = ngaySinh,
                    DiaChi = string.IsNullOrWhiteSpace(rvm.Address) ? string.Empty : rvm.Address.Trim(),
                    GhiChu = "Khách hàng được tạo khi đăng ký tài khoản website.",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };

                db.KhachHangs.Add(customer);
                db.SaveChanges();
            }
            else
            {
                customer.HoTen = fullName;
                customer.Email = string.IsNullOrWhiteSpace(rvm.Email) ? customer.Email : rvm.Email;
                customer.SoDienThoai = rvm.Mobile;
                customer.GioiTinh = gioiTinh;
                customer.NgaySinh = ngaySinh;
                customer.UpdatedAt = DateTime.Now;
                db.SaveChanges();
            }

            return customer;
        }

        private int GetCustomerRoleId()
        {
            var customerRole = db.VaiTroes.FirstOrDefault(x =>
                x.IsActive &&
                (
                    x.MaVaiTro == "USER" ||
                    x.MaVaiTro == "CUSTOMER" ||
                    x.MaVaiTro == "KHACHHANG"
                ));

            if (customerRole == null)
            {
                throw new InvalidOperationException(
                    "DB chưa có role dành cho khách hàng đăng ký website. Hãy seed thêm VaiTro với MaVaiTro = 'USER'.");
            }

            return customerRole.VaiTroId;
        }

        private string BuildFullName(string lastName, string firstName)
        {
            string fullName = string.Format("{0} {1}", lastName ?? string.Empty, firstName ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(fullName) ? "Khách hàng" : fullName;
        }

        private bool? ParseGender(string sex)
        {
            if (string.IsNullOrWhiteSpace(sex))
            {
                return null;
            }

            string normalized = sex.Trim().ToLower();

            if (normalized == "nam")
            {
                return true;
            }

            if (normalized == "nữ" || normalized == "nu")
            {
                return false;
            }

            return null;
        }

        private string GenerateCustomerCode()
        {
            string code;
            do
            {
                code = "KH" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
            while (db.KhachHangs.Any(x => x.MaKhachHang == code));

            return code;
        }

        private ActionResult RedirectToHomeByRole(TaiKhoan account)
        {
            var role = db.VaiTroes.FirstOrDefault(x => x.VaiTroId == account.VaiTroId);

            if (role == null)
            {
                return RedirectToAction("Index", "Home");
            }

            string maVaiTro = (role.MaVaiTro ?? string.Empty).Trim().ToUpperInvariant();

            if (maVaiTro == "ADMIN" || maVaiTro == "STAFF" || maVaiTro == "SHIPPER")
            {
                return RedirectToAction("Index", "Dashboard", new { area = "AdminPage" });
            }

            return RedirectToAction("Index", "Home");
        }

        private object BuildErrorResponse()
        {
            var fieldErrors = ModelState
                .Where(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.First().ErrorMessage
                );

            var globalErrors = ModelState.ContainsKey("")
                ? ModelState[""].Errors.Select(e => e.ErrorMessage).ToArray()
                : new string[0];

            return new
            {
                success = false,
                fieldErrors,
                globalErrors
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