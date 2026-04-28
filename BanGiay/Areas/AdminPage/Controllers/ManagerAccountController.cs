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
    [CustomAuthorize(Roles = "Quản trị")]
    public class ManagerAccountController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        public ActionResult AccountManager()
        {
            UpdateInterface();
            return View();
        }
        /// <summary>
        /// Dùng để tìm kiếm sản phẩm bằng tên tài khoản
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public ActionResult FindAccountByUsername(string username)
        {
            ViewData["PermisstionList"] = db.nhomTaiKhoans.ToList();
            ViewData["AccountList"] = db.taiKhoanThanhViens.Where(m => !m.taiKhoan.Equals("admin") && (string.IsNullOrEmpty(username) || m.taiKhoan.Contains(username))).ToList();
            return View("AccountManager");
        }
        /// <summary>
        /// Dùng để vô hiệu hóa hoặc mở khóa tài khoản
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DisableAccount(string userName)
        {
            try
            {
                taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(userName)).FirstOrDefault();
                if (account != null)
                {
                    if(account.trangThai == true)
                    {
                        account.trangThai = false;
                    }
                    else
                    {
                        account.trangThai = true;
                    }
                    db.SaveChanges();
                }
            }
            catch{}
            UpdateInterface();
            return View("AccountManager");
        }
        /// <summary>
        /// Xóa 1 tài khoản
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteAccount(string userName)
        {
            try
            {
                taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(userName)).FirstOrDefault();
                if (account != null)
                {
                    db.taiKhoanThanhViens.Remove(account);
                    db.SaveChanges();
                }
            }
            catch { }
            UpdateInterface();
            return View("AccountManager");
        }
        /// <summary>
        /// Chi tiết 1 tài khoản
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public ActionResult DetailAccountUser(string userName)
        {

            if (userName != null)
            {
                taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(userName)).FirstOrDefault();
                UpdateInterface();
                return View(account);
            }
            else
            {
                UpdateInterface();
                return View("AccountManager");
            }
        }
        /// <summary>
        /// Cập nhật thông tin 1 tài khoản
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="uda"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInformationAccount(string userName, UpdateAccount uda)
        {
            taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(userName)).FirstOrDefault();
            if (account != null)
            {
                account.email = uda.Email;
                account.soDT = uda.Mobile;
                account.hoDem = uda.LastName;
                account.tenTV = uda.FirstName;
                account.diaChi = uda.Address;
                account.gioiTinh = (uda.Sex.Equals("Nam") ? true : false);
                db.SaveChanges();
            }
            UpdateInterface();
            return View("DetailAccountUser", account);
        }
        /// <summary>
        /// Thay đổi mật khẩu 1 tài khoản
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="passwdCurrent"></param>
        /// <param name="uda"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string userName, string passwdCurrent, UpdateAccount uda)
        {
            taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(userName)).FirstOrDefault();
            if(account != null && uda.PassWord !=null)
            {
                account.matKhau = HashPassword.SHA512HashPass(uda.PassWord);
            }
            db.SaveChanges();
            UpdateInterface();
            return View("DetailAccountUser",account);

        }
        /// <summary>
        /// Cập nhật nhóm tài khoản quyền hạn truy cập
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="permisstionID"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePermisstionAccount(string userName,string permisstionID)
        {
            int id = int.Parse(permisstionID);
            taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(userName)).FirstOrDefault();
            //taiKhoanThanhVien account11 = (Session["LoginInformation"] as taiKhoanThanhVien);
            if(account != null) 
            {
                nhomTaiKhoan accountGroup = db.nhomTaiKhoans.Where(m => m.maNhom == id).FirstOrDefault();
                account.nhomTaiKhoan = accountGroup; // Dùng để cập nhật ở database

                // Dùng để cập nhật và thực thi ngay lập tức trên phiên
                // Lý do nếu mà không có cái này ở đây thì khi thay đổi quyền nó vẫn chưa thực thi trên phiên hiện tại mặc dù nó đã thay đổi dưới database
                //account11.nhomTaiKhoan = accountGroup;
                db.SaveChanges();
            }
            UpdateInterface();
            return View("DetailAccountUser",account);
        }
      
        /// <summary>
        /// Cập nhật lại danh sách tài khoản và danh sách nhóm tài khoản
        /// </summary>
        private void UpdateInterface()
        {
            ViewData["PermisstionList"] = db.nhomTaiKhoans.ToList();
            ViewData["AccountList"] = db.taiKhoanThanhViens.Where(m => !m.taiKhoan.Equals("admin")).ToList();
        }
    }
}