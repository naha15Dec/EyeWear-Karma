using System;
using System.Linq;
using System.Web.Mvc;
using BanGiay.Models;
using BanGiay.ViewModel;

namespace BanGiay.Controllers
{
    public class AccountController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();

        // ========================= LOGIN =========================

        public ActionResult LoginAccount()
        {
            var account = Session["LoginInformation"] as taiKhoanThanhVien;

            if (account == null)
                return View();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LoginAccount(Login lvm)
        {
            string passhash = HashPassword.SHA512HashPass(lvm.Password);

            var account = db.taiKhoanThanhViens
                .FirstOrDefault(m =>
                    m.taiKhoan == lvm.Username &&
                    m.matKhau == passhash &&
                    m.trangThai == true
                );

            if (account != null)
            {
                Session["ShoppingCart"] = null;
                Session["LoginInformation"] = account;

                if (account.maNhom != 3)
                    return RedirectToAction("Index", "Dashboard", new { area = "AdminPage" });

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Sai tài khoản hoặc mật khẩu.");
            return View(lvm);
        }

        // ========================= REGISTER =========================
        // GET: /Account/RegisterAccount
        public ActionResult RegisterAccount()
        {
            // Luôn truyền model để View không bị Model = null
            return View(new Register());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterAccount(Register rvm)
        {
            // Chuẩn hóa input
            rvm.Username = rvm.Username?.Trim();
            rvm.Mobile = rvm.Mobile?.Trim();
            rvm.Email = rvm.Email?.Trim();

            // ==== 1. VALIDATE MODEL ==== 
            if (!ModelState.IsValid)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(BuildErrorResponse());
                }
                return View(rvm);
            }

            // ==== 2. KIỂM TRA TRÙNG TÀI KHOẢN / SĐT ====
            var existAccount = db.taiKhoanThanhViens
                .FirstOrDefault(m => m.taiKhoan == rvm.Username || m.soDT == rvm.Mobile);

            if (existAccount != null)
            {
                ModelState.AddModelError("", "Tài khoản hoặc số điện thoại đã được đăng ký.");

                if (Request.IsAjaxRequest())
                {
                    return Json(BuildErrorResponse());
                }
                return View(rvm);
            }

            // ==== 3. TẠO HOẶC LẤY KHÁCH HÀNG ====
            var getCustomer = db.khachHangs
                .FirstOrDefault(m => m.soDT == rvm.Mobile);

            string maKhachHang;

            if (getCustomer == null)
            {
                maKhachHang = string.Format("{0:MMddmmss}", DateTime.Now);

                var customer = new khachHang
                {
                    maKH = maKhachHang,
                    tenKH = rvm.LastName + " " + rvm.FirstName,
                    email = rvm.Email,
                    soDT = rvm.Mobile,
                    gioiTinh = (rvm.Sex == "Nam"),
                    ngaySinh = (DateTime)rvm.DateOfBirth,
                    diaChi = "",
                    ghiChu = ""
                };
                db.khachHangs.Add(customer);
            }
            else
            {
                maKhachHang = getCustomer.maKH;
            }

            // ==== 4. TẠO TÀI KHOẢN ====
            var account = new taiKhoanThanhVien
            {
                taiKhoan = rvm.Username,
                matKhau = HashPassword.SHA512HashPass(rvm.Password),
                maKH = maKhachHang,
                hoDem = rvm.LastName,
                tenTV = rvm.FirstName,
                email = rvm.Email,
                soDT = rvm.Mobile,
                gioiTinh = (rvm.Sex == "Nam"),
                ngaysinh = (DateTime)rvm.DateOfBirth,
                maNhom = 3,
                trangThai = true,
                soTien = 0,
                diaChi = "",
                ghiChu = ""
            };

            db.taiKhoanThanhViens.Add(account);
            db.SaveChanges();

            Session["LoginInformation"] = account;

            if (Request.IsAjaxRequest())
            {
                // JSON báo thành công, client JS sẽ redirect
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("LoginAccount", "Account")
                });

            }

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Build JSON lỗi từ ModelState cho Ajax
        /// </summary>
        private object BuildErrorResponse()
        {
            var fieldErrors = ModelState
                .Where(x => x.Key != "" && x.Value.Errors.Count > 0)
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


        // ========================= LOGOUT =========================

        public ActionResult LogoutAccount()
        {
            Session["LoginInformation"] = null;
            return RedirectToAction("Index", "Home");
        }
    }
}
