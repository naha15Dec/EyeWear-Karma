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
    [CustomAuthorize(Roles = RoleConstants.ADMIN)]
    public class ManagerBlogController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int PAGE_SIZE = 10;

        [HttpGet]
        public ActionResult ManagerBlog(string status = "draft", string keyword = "", int page = 1)
        {
            var model = BuildIndexViewModel(status, keyword, page);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ActivateBlog(int id, string status = "draft", string keyword = "", int page = 1)
        {
            var post = db.BaiViets.FirstOrDefault(x => x.BaiVietId == id);
            if (post == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết.";
                return RedirectToAction("ManagerBlog", new { status, keyword, page });
            }

            switch (post.TrangThai)
            {
                case BlogStatusConstants.DRAFT:
                    post.TrangThai = BlogStatusConstants.PUBLISHED;
                    post.NgayDang = DateTime.Now;
                    break;

                case BlogStatusConstants.PUBLISHED:
                    post.TrangThai = BlogStatusConstants.HIDDEN;
                    break;

                case BlogStatusConstants.HIDDEN:
                    post.TrangThai = BlogStatusConstants.PUBLISHED;
                    if (!post.NgayDang.HasValue)
                    {
                        post.NgayDang = DateTime.Now;
                    }
                    break;

                default:
                    post.TrangThai = BlogStatusConstants.PUBLISHED;
                    post.NgayDang = DateTime.Now;
                    break;
            }

            post.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật trạng thái bài viết thành công.";
            return RedirectToAction("ManagerBlog", new { status, keyword, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string status = "draft", string keyword = "", int page = 1)
        {
            var post = db.BaiViets.FirstOrDefault(x => x.BaiVietId == id);
            if (post == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bài viết.";
                return RedirectToAction("ManagerBlog", new { status, keyword, page });
            }

            post.TrangThai = BlogStatusConstants.HIDDEN;
            post.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã chuyển bài viết sang trạng thái ẩn.";
            return RedirectToAction("ManagerBlog", new { status, keyword, page });
        }

        [HttpGet]
        public ActionResult FindPostByID(string idPost, string status = "draft")
        {
            return RedirectToAction("ManagerBlog", new { status, keyword = idPost });
        }

        private AdminBlogIndexVm BuildIndexViewModel(string status, string keyword, int page)
        {
            status = (status ?? "draft").Trim().ToLower();
            keyword = (keyword ?? string.Empty).Trim();

            if (page <= 0)
            {
                page = 1;
            }

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

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);

            if (totalPages <= 0)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var posts = query
                .OrderByDescending(x => x.UpdatedAt.HasValue ? x.UpdatedAt.Value : x.CreatedAt)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .Select(x => new AdminBlogListItemVm
                {
                    BaiVietId = x.BaiVietId,
                    MaBaiViet = x.MaBaiViet,
                    TieuDe = x.TieuDe,
                    TomTat = x.TomTat,
                    NoiDung = x.NoiDung,
                    AnhDaiDien = x.AnhDaiDien,
                    TrangThai = x.TrangThai,
                    TrangThaiText = "",
                    NguoiTao = x.TaiKhoan != null ? x.TaiKhoan.HoTen : "",
                    NgayDang = x.NgayDang,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList();

            foreach (var item in posts)
            {
                item.TrangThaiText = BlogStatusConstants.GetName(item.TrangThai);
            }

            return new AdminBlogIndexVm
            {
                HeaderTitle = GetHeaderTitle(selectedStatus),
                StatusFilter = status,
                Keyword = keyword,
                Posts = posts,
                StatusOptions = BuildStatusOptions(selectedStatus),
                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        private int GetStatusValue(string status)
        {
            switch ((status ?? string.Empty).Trim().ToLower())
            {
                case "published":
                    return BlogStatusConstants.PUBLISHED;

                case "hidden":
                    return BlogStatusConstants.HIDDEN;

                default:
                    return BlogStatusConstants.DRAFT;
            }
        }

        private string GetHeaderTitle(int status)
        {
            switch (status)
            {
                case BlogStatusConstants.PUBLISHED:
                    return "Bài viết đã đăng";

                case BlogStatusConstants.HIDDEN:
                    return "Bài viết đã ẩn";

                default:
                    return "Bài viết chờ duyệt";
            }
        }

        private List<SelectListItem> BuildStatusOptions(int selectedStatus)
        {
            return new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "draft",
                    Text = "Chờ duyệt",
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