using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
using BanGiay.ViewModel;
namespace BanGiay.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = "Người dùng")]
    public class ProfileUserController : Controller
    {
        // GET: ProfileUser
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        public ActionResult Profile(int page=1)
        {
            taiKhoanThanhVien account = (Session["LoginInformation"] as taiKhoanThanhVien);
            List<donHang> Order= db.donHangs.Where(m=>m.taiKhoan.Equals(account.taiKhoan) ).ToList();
            PaginationProductPage(Order, page);
            return View(account);
        }
        /// <summary>
        /// Hàm này dùng để thay đổi thông tin của tài khoản
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="uda"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInformationAccount(string idAccount, UpdateAccount uda)
        {
            taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(idAccount)).FirstOrDefault();
            taiKhoanThanhVien account1 = (Session["LoginInformation"] as taiKhoanThanhVien);
            List<donHang> Order = db.donHangs.Where(m=>m.taiKhoan.Equals(account1.taiKhoan)).ToList();
            if (account != null)
            {
                account.hoDem = uda.LastName;
                account.tenTV = uda.FirstName;
                account.diaChi = uda.Address;
                account.soDT = uda.Mobile;
                account.gioiTinh = (uda.Sex.Equals("Nam") ? true : false);

                account1.hoDem = uda.LastName;
                account1.tenTV = uda.FirstName;
                account1.diaChi = uda.Address;
                account1.soDT = uda.Mobile;
                account1.gioiTinh = (uda.Sex.Equals("Nam") ? true : false);
            }
            db.SaveChanges();
            PaginationProductPage(Order,1);
            return View("Profile", account);
        }
        /// <summary>
        /// Hàm này dùng để thay đổi mật khẩu tài khoản
        /// </summary>
        /// <param name="idAccount"></param>
        /// <param name="passwdCurrent"></param>
        /// <param name="uda"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePasswordAccount(string idAccount, string passwdCurrent, UpdateAccount uda)
        {
            taiKhoanThanhVien account = db.taiKhoanThanhViens.Where(m => m.taiKhoan.Equals(idAccount)).FirstOrDefault();
            if (account != null)
            {
                if (account.matKhau.Equals(HashPassword.SHA512HashPass(passwdCurrent)))
                {
                    account.matKhau = HashPassword.SHA512HashPass(uda.PassWord);
                }
            }
            db.SaveChanges();
            return View("Profile",account);
        }
        /// <summary>
        /// Hàm này dùng để phân trang lịch sử mua hàng
        /// </summary>
        /// <param name="order"></param>
        /// <param name="page"></param>
        public void PaginationProductPage(List<donHang> order, int page)
        {
            int NoOfProductOnPage = 5;
            int NoOfPages = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(order.Count) / Convert.ToDouble(NoOfProductOnPage)));
            int SkipPageNumber = (page - 1) * NoOfProductOnPage;
            ViewBag.Page = page;

            // Chú thích ViewBag.NoOfPages
            // Khi mà người dùng nhấn từ trang số 5 trở đi thì nó sẽ hiển thị thêm tiếp 4 trang nữa tức là từ trang số 5 đến trang số 9 ...
            // Nhưng nếu trang đã được tính chỉ có 10 thì khi đến lớn hơn hoặc bằng 10 thì nó sẽ không cộng thêm 4 mà thay vào đó sẽ là cái số trang
            ViewBag.NoOfPages = ((page >= 5) ? ((page + 4 > NoOfPages) ? NoOfPages : (page + 4)) : (page >= 5 ? 5 : NoOfPages));
            // Chú thích: ViewBag.Virtual
            // Đoạn này có nghĩa nó sẽ cho mất đi hiển thị những trang mà đã bấm qua
            // Nếu như mà dưới trang 5 thì nó sẽ vẫn hiển thị 5 trang đầu nhưng bắt đầu từ trang số 5 trở đi thì nó chỉ hiển thị 5 trang kế tiếp tức là từ trang số 6 trở đi
            // Nếu mà nó đến trong khoảng cách từ (số trang của website  - 5) thì nó sẽ không mất đi số trang đã bấm qua nữa
            ViewBag.DisplayPage = (page < 5 ? 0 : (((page - 1) >= (NoOfPages - 5)) ? (NoOfPages - 5) : (page - 1)));
            order = order.Skip(SkipPageNumber).Take(NoOfProductOnPage).ToList();
            ViewData["listOrderUser"] = order;
        }
        public ActionResult DetailPurchaseOrder(string numberOfOrder)
        {
            donHang order = db.donHangs.FirstOrDefault(m=>m.soDH.Equals(numberOfOrder));
            taiKhoanThanhVien account = (Session["LoginInformation"] as taiKhoanThanhVien);
            if (order!=null) 
            {
                ViewData["listOfProductInOrder"] = db.chiTietDonHangs.Where(m => m.soDH.Equals(numberOfOrder)).ToList();
                return View(order);
            }
            else
            {
                return View("Profile");
            }
          
        }
    }
}