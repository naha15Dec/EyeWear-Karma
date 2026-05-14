using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        [HttpGet]
        public ActionResult BlogList(string keyword = "", int? status = null)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToLogin();
            }

            keyword = (keyword ?? string.Empty).Trim();

            var query = db.BaiViets
                .Include(x => x.TaiKhoan)
                .Where(x => x.CreatedById == user.TaiKhoanId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaBaiViet.Contains(keyword) ||
                    x.TieuDe.Contains(keyword));
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.TrangThai == status.Value);
            }

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.IsAdmin = IsAdmin(user);

            var list = query
                .OrderByDescending(x => x.UpdatedAt.HasValue ? x.UpdatedAt.Value : x.CreatedAt)
                .ToList();

            return View(list);
        }

        [HttpGet]
        public ActionResult AddBlog()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToLogin();
            }

            ViewBag.IsAdmin = IsAdmin(user);

            var model = new AdminBlogCreateVm
            {
                TrangThai = BlogStatusConstants.DRAFT
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBlog(AdminBlogCreateVm vm, HttpPostedFileBase imageAvatar)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToLogin();
            }

            bool isAdmin = IsAdmin(user);
            ViewBag.IsAdmin = isAdmin;

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                int status = BlogStatusConstants.DRAFT;

                if (isAdmin && vm.TrangThai == BlogStatusConstants.PUBLISHED)
                {
                    status = BlogStatusConstants.PUBLISHED;
                }

                var post = new BaiViet
                {
                    MaBaiViet = GenerateCode(),
                    TieuDe = (vm.TieuDe ?? string.Empty).Trim(),
                    TomTat = string.IsNullOrWhiteSpace(vm.TomTat) ? null : vm.TomTat.Trim(),
                    NoiDung = vm.NoiDung,
                    CreatedById = user.TaiKhoanId,
                    TrangThai = status,
                    NgayDang = status == BlogStatusConstants.PUBLISHED ? DateTime.Now : (DateTime?)null,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };

                SaveImage(imageAvatar, post);

                db.BaiViets.Add(post);
                db.SaveChanges();

                TempData["SuccessMessage"] = isAdmin && status == BlogStatusConstants.PUBLISHED
                    ? "Tạo và đăng bài viết thành công."
                    : "Tạo bài viết thành công. Bài đang chờ duyệt.";

                return RedirectToAction("BlogList");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Tạo bài viết thất bại: " + ex.Message);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id, AdminBlogCreateVm vm, HttpPostedFileBase imageAvatar)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToLogin();
            }

            var post = db.BaiViets.FirstOrDefault(x => x.BaiVietId == id);
            if (post == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết.";
                return RedirectToAction("BlogList");
            }

            if (post.CreatedById != user.TaiKhoanId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền sửa bài viết này.";
                return RedirectToAction("BlogList");
            }

            bool isAdmin = IsAdmin(user);

            if (!isAdmin && post.TrangThai == BlogStatusConstants.PUBLISHED)
            {
                TempData["ErrorMessage"] = "Bài viết đã đăng. Nhân viên không được sửa trực tiếp bài đã đăng.";
                return RedirectToAction("BlogList");
            }

            if (string.IsNullOrWhiteSpace(vm.TieuDe) || string.IsNullOrWhiteSpace(vm.NoiDung))
            {
                TempData["ErrorMessage"] = "Tiêu đề và nội dung bài viết không được để trống.";
                return RedirectToAction("BlogList");
            }

            try
            {
                post.TieuDe = vm.TieuDe.Trim();
                post.TomTat = string.IsNullOrWhiteSpace(vm.TomTat) ? null : vm.TomTat.Trim();
                post.NoiDung = vm.NoiDung;
                post.UpdatedAt = DateTime.Now;

                if (isAdmin)
                {
                    if (vm.TrangThai == BlogStatusConstants.PUBLISHED)
                    {
                        post.TrangThai = BlogStatusConstants.PUBLISHED;

                        if (!post.NgayDang.HasValue)
                        {
                            post.NgayDang = DateTime.Now;
                        }
                    }
                    else if (vm.TrangThai == BlogStatusConstants.DRAFT)
                    {
                        post.TrangThai = BlogStatusConstants.DRAFT;
                        post.NgayDang = null;
                    }
                    else if (vm.TrangThai == BlogStatusConstants.HIDDEN)
                    {
                        post.TrangThai = BlogStatusConstants.HIDDEN;
                    }
                }
                else
                {
                    post.TrangThai = BlogStatusConstants.DRAFT;
                    post.NgayDang = null;
                }

                SaveImage(imageAvatar, post);

                db.SaveChanges();

                TempData["SuccessMessage"] = isAdmin
                    ? "Cập nhật bài viết thành công."
                    : "Cập nhật bài viết thành công. Bài viết đã được gửi lại để chờ duyệt.";
                return RedirectToAction("BlogList");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Cập nhật bài viết thất bại: " + ex.Message;
                return RedirectToAction("BlogList");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                return RedirectToLogin();
            }

            var post = db.BaiViets.FirstOrDefault(x => x.BaiVietId == id);
            if (post == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết.";
                return RedirectToAction("BlogList");
            }

            if (post.CreatedById != user.TaiKhoanId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa bài viết này.";
                return RedirectToAction("BlogList");
            }

            bool isAdmin = IsAdmin(user);

            if (!isAdmin && post.TrangThai == BlogStatusConstants.PUBLISHED)
            {
                TempData["ErrorMessage"] = "Bài viết đã đăng. Nhân viên không được xóa/ẩn bài đã đăng.";
                return RedirectToAction("BlogList");
            }

            post.TrangThai = BlogStatusConstants.HIDDEN;
            post.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã chuyển bài viết sang trạng thái ẩn.";
            return RedirectToAction("BlogList");
        }

        [HttpGet]
        public ActionResult FindPostByName(string keyword)
        {
            return RedirectToAction("BlogList", new { keyword });
        }

        private TaiKhoan GetCurrentUser()
        {
            var session = Session["LoginInformation"] as TaiKhoan;
            if (session == null)
            {
                return null;
            }

            return db.TaiKhoans
                .Include(x => x.VaiTro)
                .FirstOrDefault(x => x.TaiKhoanId == session.TaiKhoanId && x.IsActive);
        }

        private bool IsAdmin(TaiKhoan user)
        {
            return user != null &&
                   user.VaiTro != null &&
                   string.Equals(user.VaiTro.MaVaiTro, RoleConstants.ADMIN, StringComparison.OrdinalIgnoreCase);
        }

        private ActionResult RedirectToLogin()
        {
            return RedirectToAction("LoginAccount", "Account", new { area = "" });
        }

        private string GenerateCode()
        {
            string code;

            do
            {
                code = "BV" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
            while (db.BaiViets.Any(x => x.MaBaiViet == code));

            return code;
        }

        private void SaveImage(HttpPostedFileBase file, BaiViet post)
        {
            if (file == null || file.ContentLength <= 0)
            {
                return;
            }

            const int maxSize = 3 * 1024 * 1024;
            if (file.ContentLength > maxSize)
            {
                throw new InvalidOperationException("Ảnh đại diện không được vượt quá 3MB.");
            }

            string extension = Path.GetExtension(file.FileName);
            string lowerExtension = (extension ?? string.Empty).ToLower();

            var allowedExtensions = new HashSet<string>
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (!allowedExtensions.Contains(lowerExtension))
            {
                throw new InvalidOperationException("Chỉ cho phép upload ảnh .jpg, .jpeg, .png hoặc .webp.");
            }

            string folder = "/Asset/SaveImgBlog/";
            string physicalFolder = Server.MapPath("~" + folder);

            if (!Directory.Exists(physicalFolder))
            {
                Directory.CreateDirectory(physicalFolder);
            }

            string fileName = Guid.NewGuid() + extension;
            string fullPath = Path.Combine(physicalFolder, fileName);

            file.SaveAs(fullPath);

            post.AnhDaiDien = folder + fileName;
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