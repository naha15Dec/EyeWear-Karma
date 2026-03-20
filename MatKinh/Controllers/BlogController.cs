using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;

namespace MatKinh.Controllers
{
    public class BlogController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int BlogStatusPublished = 2;
        private const int PageSize = 3;

        // ====================== LIST ======================
        public ActionResult Index(int page = 1)
        {
            if (page <= 0) page = 1;

            var query = db.BaiViets
                .AsNoTracking()
                .Where(x => x.TrangThai == BlogStatusPublished)
                .OrderByDescending(x => x.NgayDang ?? x.CreatedAt);

            BuildPagination(query, page);

            return View();
        }

        // ====================== DETAIL ======================
        public ActionResult DetailBlog(int? id)
        {
            if (!id.HasValue)
                return RedirectToAction("Index");

            var post = db.BaiViets
                .FirstOrDefault(x =>
                    x.BaiVietId == id &&
                    x.TrangThai == BlogStatusPublished);

            if (post == null)
                return RedirectToAction("Index");

            // ❌ KHÔNG có lượt xem -> bỏ

            ViewData["listPostPopular"] = GetLatestPosts(post.BaiVietId);

            return View(post);
        }

        // ====================== SEARCH ======================
        public ActionResult FindBlogByName(string keyword, int page = 1)
        {
            keyword = (keyword ?? "").Trim();

            var query = db.BaiViets
                .AsNoTracking()
                .Where(x => x.TrangThai == BlogStatusPublished);

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(x => x.TieuDe.Contains(keyword));
            }

            ViewData["Keyword"] = keyword;

            BuildPagination(
                query.OrderByDescending(x => x.NgayDang ?? x.CreatedAt),
                page
            );

            return View("Index");
        }

        // ====================== PAGINATION ======================
        private void BuildPagination(IQueryable<BaiViet> query, int page)
        {
            int total = query.Count();
            int totalPages = (int)Math.Ceiling((double)total / PageSize);

            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var data = query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewData["listPost"] = data;
            ViewData["listPostPopular"] = GetLatestPosts();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.DisplayPage = (page < 5 ? 0 : Math.Max(page - 1, 0));
            ViewBag.NoOfPages = (page + 4 > totalPages) ? totalPages : (page + 4);
        }

        // ====================== SIDEBAR ======================
        private List<BaiViet> GetLatestPosts(int? excludeId = null)
        {
            var query = db.BaiViets
                .AsNoTracking()
                .Where(x => x.TrangThai == BlogStatusPublished);

            if (excludeId.HasValue)
                query = query.Where(x => x.BaiVietId != excludeId.Value);

            return query
                .OrderByDescending(x => x.NgayDang ?? x.CreatedAt)
                .Take(5)
                .ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}