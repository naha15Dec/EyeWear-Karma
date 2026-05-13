using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Controllers
{
    public class AccountController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const string RememberCookieName = "KarmaRememberToken";
        private const int RememberDays = 7;

        // ========================= LOGIN =========================

        [HttpGet]
        public ActionResult LoginAccount()
        {
            TaiKhoan rememberedAccount = TryLoginByRememberCookie();

            if (rememberedAccount != null)
            {
                Session["LoginInformation"] = rememberedAccount;
                SetCustomerSession(rememberedAccount);

                TempData["LoginSuccess"] = "Đăng nhập tự động thành công.";
                return RedirectToHomeByRole(rememberedAccount);
            }

            return View(new Login());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LoginAccount(Login model)
        {
            NormalizeLoginInput(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string passwordHash = HashPassword.SHA512HashPass(model.Password ?? string.Empty);

            var account = db.TaiKhoans.FirstOrDefault(x =>
                x.TenDangNhap == model.Username &&
                x.MatKhauHash == passwordHash &&
                x.IsActive);

            if (account == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            var role = db.VaiTroes.FirstOrDefault(x =>
                x.VaiTroId == account.VaiTroId &&
                x.IsActive);

            if (role == null)
            {
                ModelState.AddModelError("", "Vai trò tài khoản hiện không khả dụng.");
                return View(model);
            }

            account.LastLoginAt = DateTime.Now;
            db.SaveChanges();

            Session["LoginInformation"] = account;
            SetCustomerSession(account);

            if (model.RememberMe)
            {
                CreateRememberToken(account);
            }
            else
            {
                ClearRememberTokenCookie();
            }

            TempData["LoginSuccess"] = "Đăng nhập thành công.";
            return RedirectToHomeByRole(account);
        }

        // ========================= REGISTER =========================

        [HttpGet]
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
                        DiaChi = rvm.Address,
                        AnhDaiDien = null,
                        IsActive = true,
                        LastLoginAt = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = null
                    };

                    db.TaiKhoans.Add(account);
                    db.SaveChanges();

                    transaction.Commit();

                    Session["LoginInformation"] = account;
                    Session["KhachHangId"] = customer.KhachHangId;

                    TempData["RegisterSuccess"] = "Đăng ký tài khoản thành công.";

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
            RevokeCurrentRememberToken();

            Session["LoginInformation"] = null;
            Session["KhachHangId"] = null;
            Session["LastCreatedOrderId"] = null;
            Session["LastCreatedOrderCode"] = null;

            Session["PendingVnPayOrderCode"] = null;
            Session["PendingVnPayPurchaseInformation"] = null;
            Session["PendingVnPayCart"] = null;
            Session["PendingVnPayAmount"] = null;
            Session["PendingVnPayKhachHangId"] = null;

            TempData["LogoutSuccess"] = "Bạn đã đăng xuất khỏi hệ thống.";

            return RedirectToAction("Index", "Home");
        }

        // ========================= PRIVATE: LOGIN HELPERS =========================

        private void NormalizeLoginInput(Login model)
        {
            if (model == null)
            {
                return;
            }

            model.Username = NormalizeUsername(model.Username);
        }

        private void SetCustomerSession(TaiKhoan account)
        {
            Session["KhachHangId"] = null;

            if (account == null)
            {
                return;
            }

            string email = string.IsNullOrWhiteSpace(account.Email) ? null : account.Email.Trim();
            string phone = string.IsNullOrWhiteSpace(account.SoDienThoai) ? null : account.SoDienThoai.Trim();

            KhachHang customer = null;

            if (!string.IsNullOrWhiteSpace(email))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.Email == email);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(phone))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.SoDienThoai == phone);
            }

            if (customer != null)
            {
                Session["KhachHangId"] = customer.KhachHangId;
            }
        }

        private ActionResult RedirectToHomeByRole(TaiKhoan account)
        {
            if (account == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            var role = db.VaiTroes.FirstOrDefault(x => x.VaiTroId == account.VaiTroId);

            if (role == null || string.IsNullOrWhiteSpace(role.MaVaiTro))
            {
                return RedirectToAction("Index", "Home");
            }

            string roleCode = role.MaVaiTro.Trim().ToUpperInvariant();

            if (roleCode == RoleConstants.ADMIN ||
                roleCode == RoleConstants.STAFF ||
                roleCode == RoleConstants.SHIPPER)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "AdminPage" });
            }

            return RedirectToAction("Index", "Home");
        }

        // ========================= PRIVATE: REMEMBER ME =========================

        private void CreateRememberToken(TaiKhoan account)
        {
            if (account == null)
            {
                return;
            }

            RevokeRememberTokens(account.TaiKhoanId);

            string rawToken = GenerateSecureToken();
            string tokenHash = HashRememberToken(rawToken);

            var rememberToken = new TaiKhoanRememberToken
            {
                TaiKhoanId = account.TaiKhoanId,
                TokenHash = tokenHash,
                ExpiresAt = DateTime.Now.AddDays(RememberDays),
                CreatedAt = DateTime.Now,
                RevokedAt = null,
                UserAgent = GetUserAgent(),
                IpAddress = GetIpAddress()
            };

            db.TaiKhoanRememberTokens.Add(rememberToken);
            db.SaveChanges();

            var cookie = new HttpCookie(RememberCookieName, rawToken)
            {
                HttpOnly = true,
                Secure = Request != null && Request.IsSecureConnection,
                Expires = DateTime.Now.AddDays(RememberDays),
                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Add(cookie);
        }

        private TaiKhoan TryLoginByRememberCookie()
        {
            HttpCookie cookie = Request.Cookies[RememberCookieName];

            if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value))
            {
                return null;
            }

            string tokenHash = HashRememberToken(cookie.Value);

            var token = db.TaiKhoanRememberTokens.FirstOrDefault(x =>
                x.TokenHash == tokenHash &&
                x.RevokedAt == null &&
                x.ExpiresAt > DateTime.Now);

            if (token == null)
            {
                ClearRememberTokenCookie();
                return null;
            }

            var account = db.TaiKhoans.FirstOrDefault(x =>
                x.TaiKhoanId == token.TaiKhoanId &&
                x.IsActive);

            if (account == null)
            {
                token.RevokedAt = DateTime.Now;
                db.SaveChanges();

                ClearRememberTokenCookie();
                return null;
            }

            var role = db.VaiTroes.FirstOrDefault(x =>
                x.VaiTroId == account.VaiTroId &&
                x.IsActive);

            if (role == null)
            {
                token.RevokedAt = DateTime.Now;
                db.SaveChanges();

                ClearRememberTokenCookie();
                return null;
            }

            account.LastLoginAt = DateTime.Now;
            db.SaveChanges();

            return account;
        }

        private void RevokeCurrentRememberToken()
        {
            HttpCookie cookie = Request.Cookies[RememberCookieName];

            if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value))
            {
                string tokenHash = HashRememberToken(cookie.Value);

                var token = db.TaiKhoanRememberTokens.FirstOrDefault(x =>
                    x.TokenHash == tokenHash &&
                    x.RevokedAt == null);

                if (token != null)
                {
                    token.RevokedAt = DateTime.Now;
                    db.SaveChanges();
                }
            }

            ClearRememberTokenCookie();
        }

        private void RevokeRememberTokens(int taiKhoanId)
        {
            var tokens = db.TaiKhoanRememberTokens
                .Where(x =>
                    x.TaiKhoanId == taiKhoanId &&
                    x.RevokedAt == null)
                .ToList();

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.Now;
            }

            if (tokens.Any())
            {
                db.SaveChanges();
            }
        }

        private void ClearRememberTokenCookie()
        {
            if (Response == null)
            {
                return;
            }

            var cookie = new HttpCookie(RememberCookieName)
            {
                Value = string.Empty,
                Expires = DateTime.Now.AddDays(-1),
                HttpOnly = true,
                Secure = Request != null && Request.IsSecureConnection,
                SameSite = SameSiteMode.Lax
            };

            Response.Cookies.Add(cookie);
        }

        private string GenerateSecureToken()
        {
            byte[] bytes = new byte[32];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes);
        }

        private string HashRememberToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(token);
                byte[] hashBytes = sha.ComputeHash(bytes);

                StringBuilder builder = new StringBuilder();

                foreach (byte b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private string GetUserAgent()
        {
            try
            {
                string userAgent = Request.UserAgent ?? string.Empty;

                if (userAgent.Length > 500)
                {
                    userAgent = userAgent.Substring(0, 500);
                }

                return userAgent;
            }
            catch
            {
                return null;
            }
        }

        private string GetIpAddress()
        {
            try
            {
                string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (!string.IsNullOrWhiteSpace(ip))
                {
                    ip = ip.Split(',')[0].Trim();
                }

                if (string.IsNullOrWhiteSpace(ip) || ip.ToLower() == "unknown")
                {
                    ip = Request.ServerVariables["REMOTE_ADDR"];
                }

                if (string.IsNullOrWhiteSpace(ip) || ip == "::1")
                {
                    ip = "127.0.0.1";
                }

                if (ip.Length > 45)
                {
                    ip = ip.Substring(0, 45);
                }

                return ip;
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        // ========================= PRIVATE: REGISTER HELPERS =========================

        private void NormalizeRegisterInput(Register rvm)
        {
            if (rvm == null)
            {
                return;
            }

            rvm.Username = NormalizeUsername(rvm.Username);
            rvm.FirstName = NormalizeText(rvm.FirstName);
            rvm.LastName = NormalizeText(rvm.LastName);
            rvm.Mobile = NormalizePhone(rvm.Mobile);
            rvm.Email = NormalizeEmail(rvm.Email);
            rvm.Sex = NormalizeText(rvm.Sex);
            rvm.Address = NormalizeText(rvm.Address);
        }

        private void ValidateRegisterBusinessRules(Register rvm)
        {
            if (rvm == null)
            {
                ModelState.AddModelError("", "Dữ liệu đăng ký không hợp lệ.");
                return;
            }

            if (db.TaiKhoans.Any(x => x.TenDangNhap == rvm.Username))
            {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
            }

            if (db.TaiKhoans.Any(x => x.Email == rvm.Email))
            {
                ModelState.AddModelError("Email", "Email đã được sử dụng.");
            }

            if (db.TaiKhoans.Any(x => x.SoDienThoai == rvm.Mobile))
            {
                ModelState.AddModelError("Mobile", "Số điện thoại đã được sử dụng.");
            }

            if (rvm.DateOfBirth.HasValue)
            {
                int age = CalculateAge(rvm.DateOfBirth.Value.Date);

                if (age < 6)
                {
                    ModelState.AddModelError("DateOfBirth", "Người dùng phải từ 6 tuổi trở lên.");
                }

                if (age > 100)
                {
                    ModelState.AddModelError("DateOfBirth", "Ngày sinh không hợp lệ.");
                }
            }

            KhachHang customerByPhone = db.KhachHangs.FirstOrDefault(x => x.SoDienThoai == rvm.Mobile);
            KhachHang customerByEmail = db.KhachHangs.FirstOrDefault(x => x.Email == rvm.Email);

            if (customerByPhone != null &&
                customerByEmail != null &&
                customerByPhone.KhachHangId != customerByEmail.KhachHangId)
            {
                ModelState.AddModelError("", "Email và số điện thoại đang thuộc hai hồ sơ khách hàng khác nhau. Vui lòng kiểm tra lại thông tin.");
            }
        }

        private KhachHang GetOrCreateCustomerForRegistration(Register rvm, string fullName, bool? gioiTinh, DateTime? ngaySinh)
        {
            KhachHang customer = db.KhachHangs.FirstOrDefault(x => x.SoDienThoai == rvm.Mobile);

            if (customer == null)
            {
                customer = db.KhachHangs.FirstOrDefault(x => x.Email == rvm.Email);
            }

            if (customer == null)
            {
                customer = new KhachHang
                {
                    MaKhachHang = GenerateCustomerCode(),
                    HoTen = fullName,
                    Email = rvm.Email,
                    SoDienThoai = rvm.Mobile,
                    GioiTinh = gioiTinh,
                    NgaySinh = ngaySinh,
                    DiaChi = rvm.Address,
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
                customer.Email = rvm.Email;
                customer.SoDienThoai = rvm.Mobile;
                customer.GioiTinh = gioiTinh;
                customer.NgaySinh = ngaySinh;
                customer.DiaChi = rvm.Address;
                customer.IsActive = true;
                customer.UpdatedAt = DateTime.Now;

                db.SaveChanges();
            }

            return customer;
        }

        private int GetCustomerRoleId()
        {
            var role = db.VaiTroes.FirstOrDefault(x =>
                x.MaVaiTro == RoleConstants.USER &&
                x.IsActive);

            if (role == null)
            {
                throw new InvalidOperationException("Không tìm thấy vai trò khách hàng.");
            }

            return role.VaiTroId;
        }

        private object BuildErrorResponse()
        {
            var errors = ModelState
                .Where(x => x.Value.Errors.Any())
                .Select(x => new
                {
                    key = x.Key,
                    messages = x.Value.Errors.Select(e => e.ErrorMessage).ToList()
                })
                .ToList();

            return new
            {
                success = false,
                errors = errors
            };
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

        private string BuildFullName(string lastName, string firstName)
        {
            string fullName = string.Format("{0} {1}",
                NormalizeText(lastName),
                NormalizeText(firstName)).Trim();

            return string.IsNullOrWhiteSpace(fullName) ? "Khách hàng" : fullName;
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

        // ========================= PRIVATE: COMMON NORMALIZE =========================

        private string NormalizeUsername(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private string NormalizeEmail(string value)
        {
            return (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        private string NormalizeText(string value)
        {
            value = (value ?? string.Empty).Trim();

            while (value.Contains("  "))
            {
                value = value.Replace("  ", " ");
            }

            return value;
        }

        private string NormalizePhone(string value)
        {
            value = (value ?? string.Empty).Trim();

            value = value.Replace(" ", "")
                         .Replace("-", "")
                         .Replace(".", "")
                         .Replace("(", "")
                         .Replace(")", "");

            if (value.StartsWith("+84"))
            {
                value = "0" + value.Substring(3);
            }

            return value;
        }

        private int CalculateAge(DateTime dob)
        {
            DateTime today = DateTime.Today;

            int age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
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