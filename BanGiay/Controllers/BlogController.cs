using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
namespace BanGiay.Controllers
{
    public class BlogController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        public ActionResult Index(int page = 1)
        {
            List<baiViet> lb = db.baiViets.Where(m => m.daDuyet == true).ToList();
            PagninationBlogPage(lb, page);
            return View();
        }
        /// <summary>
        /// Dùng để xem chi tiết sản phẩm
        /// </summary>
        /// <param name="idBlog"></param>
        /// <returns></returns>
        public ActionResult DetailBlog(string idBlog)
        {
            if(idBlog != null)
            {
                baiViet bv = db.baiViets.Where(m => m.maBV == idBlog).FirstOrDefault();
                if (Session["LoginInformation"] != null)
                {
                    bv.luotXem++;
                    db.SaveChanges();
                }
                ViewData["listPostPopular"] = db.baiViets.OrderByDescending(m => m.luotXem).Where(m => m.daDuyet == true).Take(5).ToList();
                return View(bv);
            }
            else 
            {
                return RedirectToAction("Index","Home");
            }
        }
        /// <summary>
        /// Dùng để tìm kiếm bài viết theo tên
        /// </summary>
        /// <param name="namePost"></param>
        /// <returns></returns>
        public ActionResult FindBlogByName(string namePost,int page=1)
        {
            List<baiViet> lb = db.baiViets.Where(m=>m.tenBV.Contains(namePost)).ToList();
            PagninationBlogPage(lb, page);
            return View("Index");
        }
        /// <summary>
        /// Dùng để phân trang bài viết
        /// </summary>
        /// <param name="lb"></param>
        /// <param name="page"></param>
        public void PagninationBlogPage(List<baiViet> lb,int page)
        {
            ViewData["listPostPopular"] = db.baiViets.OrderByDescending(m => m.luotXem).Where(m => m.daDuyet == true).Take(5).ToList();
            int NumberOfProductOnPage = 3;
            int NoOfPages = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(lb.Count) / Convert.ToDouble(NumberOfProductOnPage)));
            int SkipPageNumber = (page - 1) * NumberOfProductOnPage;
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
            lb = lb.Skip(SkipPageNumber).Take(NumberOfProductOnPage).ToList();

            ViewData["listPost"] = lb;
        }
    }
}