using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
namespace BanGiay.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = "Quản trị")]
    public class ManagerBlogController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        static bool check;
        /// <summary>
        /// Hiển thị danh sách bài viết đã duyệt hoặc chưa duyệt
        /// </summary>
        /// <param name="activate"></param>
        /// <returns></returns>
        public ActionResult ManagerBlog(string activate)
        {
            check = (activate.Equals("activate"));
            ViewBag.header = (check == true ? "Bài viết đã duyệt" : "Bài viết chờ duyệt");
            UpdateInterface(check);
            return View();
        }
        /// <summary>
        /// Duyệt và ẩn bài viết
        /// </summary>
        /// <param name="idPost"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActivateBlog(string idPost)
        {
            baiViet post = db.baiViets.Where(m => m.maBV.Equals(idPost)).FirstOrDefault();

            if (check)
            {
                post.daDuyet = false;
            }
            else
            {
                post.daDuyet = true;
            }
           
            db.SaveChanges();
            UpdateInterface(check);
            return View("ManagerBlog");
        }
        /// <summary>
        /// Xóa 1 bài viết
        /// </summary>
        /// <param name="idPost"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string idPost)
        {
            baiViet post = db.baiViets.Where(m => m.maBV.Equals(idPost)).FirstOrDefault();
            db.baiViets.Remove(post);
            db.SaveChanges();
            UpdateInterface(check);
            return View("ManagerBlog");
        }
        /// <summary>
        /// Dùng để tìm kiếm bài viết theo mã bài viết
        /// </summary>
        /// <param name="idPost"></param>
        /// <returns></returns>
        public ActionResult FindPostByID(string idPost)
        {
            ViewData["ManagerListBlog"] = db.baiViets.Where(m => m.daDuyet == check && (string.IsNullOrEmpty(idPost) || m.maBV.Contains(idPost))).ToList();
            return View("ManagerBlog");
        }
        /// <summary>
        /// Cập nhật giao diện lại danh sách bài viết
        /// </summary>
        /// <param name="isCheck"></param>
        private void UpdateInterface(bool isCheck)
        {
            ViewData["ManagerListBlog"] = db.baiViets.Where(m => m.daDuyet == isCheck ).ToList();
        }
    }
}