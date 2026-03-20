using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN + "," + RoleConstants.STAFF)]
    public class ListBlogController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        // ================= LIST =================
        public ActionResult BlogList(string keyword = "")
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToLogin();

            var list = db.BaiViets
                .Where(x => x.CreatedById == user.TaiKhoanId &&
                           (string.IsNullOrEmpty(keyword) || x.TieuDe.Contains(keyword)))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(list);
        }

        // ================= CREATE =================
        [HttpGet]
        public ActionResult AddBlog()
        {
            return View(new AdminBlogCreateVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBlog(AdminBlogCreateVm vm, HttpPostedFileBase imageAvatar)
        {
            var user = GetCurrentUser();
            if (user == null) return RedirectToLogin();

            if (!ModelState.IsValid)
                return View(vm);

            var post = new BaiViet
            {
                MaBaiViet = GenerateCode(),
                TieuDe = vm.TieuDe,
                TomTat = vm.TomTat,
                NoiDung = vm.NoiDung,
                CreatedById = user.TaiKhoanId,
                TrangThai = BlogStatusConstants.DRAFT,
                CreatedAt = DateTime.Now
            };

            SaveImage(imageAvatar, post);

            db.BaiViets.Add(post);
            db.SaveChanges();

            return RedirectToAction("BlogList");
        }

        // ================= UPDATE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id, AdminBlogCreateVm vm, HttpPostedFileBase imageAvatar)
        {
            var post = db.BaiViets.FirstOrDefault(x => x.BaiVietId == id);
            if (post == null) return RedirectToAction("BlogList");

            post.TieuDe = vm.TieuDe;
            post.TomTat = vm.TomTat;
            post.NoiDung = vm.NoiDung;
            post.UpdatedAt = DateTime.Now;

            SaveImage(imageAvatar, post);

            db.SaveChanges();

            return RedirectToAction("BlogList");
        }

        // ================= DELETE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var post = db.BaiViets.FirstOrDefault(x => x.BaiVietId == id);
            if (post != null)
            {
                db.BaiViets.Remove(post);
                db.SaveChanges();
            }

            return RedirectToAction("BlogList");
        }

        // ================= SEARCH =================
        public ActionResult FindPostByName(string keyword)
        {
            return RedirectToAction("BlogList", new { keyword });
        }

        // ================= HELPER =================

        private TaiKhoan GetCurrentUser()
        {
            var session = Session["LoginInformation"] as TaiKhoan;
            if (session == null) return null;

            return db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == session.TaiKhoanId);
        }

        private ActionResult RedirectToLogin()
        {
            return RedirectToAction("LoginAccount", "Account", new { area = "" });
        }

        private string GenerateCode()
        {
            return DateTime.Now.ToString("MMddHHmmss");
        }

        private void SaveImage(HttpPostedFileBase file, BaiViet post)
        {
            if (file != null && file.ContentLength > 0)
            {
                string folder = "/Asset/SaveImgBlog/";
                string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

                string path = Server.MapPath(folder);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                string fullPath = Path.Combine(path, fileName);
                file.SaveAs(fullPath);

                post.AnhDaiDien = folder + fileName;
            }
        }
    }
}