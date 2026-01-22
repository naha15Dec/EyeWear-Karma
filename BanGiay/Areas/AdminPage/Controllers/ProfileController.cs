using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
using BanGiay.ViewModel;
namespace BanGiay.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles ="Quản trị,Nhân viên")]
    public class ProfileController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        // GET: AdminPage/Profile
        public ActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// Cập nhật lại thông tin cho các tài khoản không phải là người dùng
        /// </summary>
        /// <param name="idAccount"></param>
        /// <param name="uda"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInformationAccount(string idAccount,UpdateAccount uda)
        {
            taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(idAccount)).FirstOrDefault();
            taiKhoanThanhVien account1 = (Session["LoginInformation"] as taiKhoanThanhVien);
            if (account != null)
            {
                account.hoDem = uda.LastName;
                account.tenTV = uda.FirstName;
                account.diaChi = uda.Address;
                account.soDT = uda.Mobile;
                account.soTien = uda.AmountOfMoney;
                account.gioiTinh = (uda.Sex.Equals("Nam") ? true : false);

                account1.hoDem = uda.LastName;
                account1.tenTV = uda.FirstName;
                account1.diaChi = uda.Address;
                account1.soDT = uda.Mobile;
                account1.soTien = uda.AmountOfMoney;
                account1.gioiTinh = (uda.Sex.Equals("Nam") ? true : false);
            }
            db.SaveChanges();
            return View("Index");
        }
        /// <summary>
        /// Thay đổi mật khẩu tài khoản cho các tài khoản không phải là người dùng
        /// </summary>
        /// <param name="idAccount"></param>
        /// <param name="passwdCurrent"></param>
        /// <param name="uda"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePasswordAccount(string idAccount,string passwdCurrent,UpdateAccount uda)
        {
            taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(idAccount)).FirstOrDefault();
            if (account != null)
            {
                if (account.matKhau.Equals(HashPassword.SHA512HashPass(passwdCurrent))){
                    account.matKhau = HashPassword.SHA512HashPass(uda.PassWord);
                }
            }
            db.SaveChanges();
            return View("Index");
        }
    }
}