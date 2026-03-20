using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN + "," + RoleConstants.STAFF)]
    public class ManagerBlogController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult ManagerBlog(string status = "published", string keyword = "")
        {
            var model = BuildIndexViewModel(status, keyword);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActivateBlog(int id, string status = "published", string keyword = "")
        {
            var post = db.BaiViets.FirstOrDefault(x => x.BaiVietId == id);
            if (post == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết.";
                return RedirectToAction("ManagerBlog", new { status, keyword });
            }

            switch ((status ?? string.Empty).Trim().ToLower())
            {
                case "draft":
                    post.TrangThai = BlogStatusConstants.PUBLISHED;
                    post.NgayDang = DateTime.Now;
                    break;

                case "published":
                    post.TrangThai = BlogStatusConstants.HIDDEN;
                    break;

                case "hidden":
                    post.TrangThai = BlogStatusConstants.PUBLISHED;
                    if (!post.NgayDang.HasValue)
                    {
                        post.NgayDang = DateTime.Now;
                    }
                    break;

                default:
                    post.TrangThai = BlogStatusConstants.PUBLISHED;
                    if (!post.NgayDang.HasValue)
                    {
                        post.NgayDang = DateTime.Now;
                    }
                    break;
            }

            post.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật trạng thái bài viết thành công.";
            return RedirectToAction("ManagerBlog", new { status, keyword });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string status = "published", string keyword = "")
        {
            var post = db.BaiViets.FirstOrDefault(x => x.BaiVietId == id);
            if (post == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết.";
                return RedirectToAction("ManagerBlog", new { status, keyword });
            }

            db.BaiViets.Remove(post);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Xóa bài viết thành công.";
            return RedirectToAction("ManagerBlog", new { status, keyword });
        }

        [HttpGet]
        public ActionResult FindPostByID(string idPost, string status = "published")
        {
            var model = BuildIndexViewModel(status, idPost);
            return View("ManagerBlog", model);
        }

        private AdminBlogIndexVm BuildIndexViewModel(string status, string keyword)
        {
            status = (status ?? "published").Trim().ToLower();
            keyword = (keyword ?? string.Empty).Trim();

            int selectedStatus = GetStatusValue(status);

            var query = db.BaiViets
                .Include(x => x.TaiKhoan)
                .Where(x => x.TrangThai == selectedStatus);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaBaiViet.Contains(keyword) ||
                    x.TieuDe.Contains(keyword));
            }

            var model = new AdminBlogIndexVm
            {
                HeaderTitle = GetHeaderTitle(selectedStatus),
                StatusFilter = status,
                Keyword = keyword,
                Posts = query
                    .OrderByDescending(x => x.UpdatedAt.HasValue ? x.UpdatedAt.Value : x.CreatedAt)
                    .Select(x => new AdminBlogListItemVm
                    {
                        BaiVietId = x.BaiVietId,
                        MaBaiViet = x.MaBaiViet,
                        TieuDe = x.TieuDe,
                        TomTat = x.TomTat,
                        AnhDaiDien = x.AnhDaiDien,
                        TrangThai = x.TrangThai,
                        TrangThaiText = "",
                        NguoiTao = x.TaiKhoan != null ? x.TaiKhoan.HoTen : "",
                        NgayDang = x.NgayDang,
                        CreatedAt = x.CreatedAt,
                        UpdatedAt = x.UpdatedAt
                    })
                    .ToList(),
                StatusOptions = BuildStatusOptions(selectedStatus)
            };

            foreach (var item in model.Posts)
            {
                item.TrangThaiText = BlogStatusConstants.GetName(item.TrangThai);
            }

            return model;
        }

        private int GetStatusValue(string status)
        {
            switch ((status ?? string.Empty).Trim().ToLower())
            {
                case "draft":
                    return BlogStatusConstants.DRAFT;
                case "hidden":
                    return BlogStatusConstants.HIDDEN;
                default:
                    return BlogStatusConstants.PUBLISHED;
            }
        }

        private string GetHeaderTitle(int status)
        {
            switch (status)
            {
                case BlogStatusConstants.DRAFT:
                    return "Bài viết nháp";
                case BlogStatusConstants.HIDDEN:
                    return "Bài viết đã ẩn";
                default:
                    return "Bài viết đã đăng";
            }
        }

        private List<SelectListItem> BuildStatusOptions(int selectedStatus)
        {
            return new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "draft",
                    Text = "Nháp",
                    Selected = selectedStatus == BlogStatusConstants.DRAFT
                },
                new SelectListItem
                {
                    Value = "published",
                    Text = "Đã đăng",
                    Selected = selectedStatus == BlogStatusConstants.PUBLISHED
                },
                new SelectListItem
                {
                    Value = "hidden",
                    Text = "Ẩn",
                    Selected = selectedStatus == BlogStatusConstants.HIDDEN
                }
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