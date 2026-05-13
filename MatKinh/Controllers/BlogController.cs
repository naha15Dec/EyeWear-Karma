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
        private const int SidebarPostTake = 5;
        private const int MaxKeywordLength = 100;

        // ====================== LIST ======================
        [HttpGet]
        public ActionResult Index(int page = 1)
        {
            if (page <= 0)
            {
                page = 1;
            }

            var query = BuildPublishedBlogQuery()
                .OrderByDescending(x => x.NgayDang ?? x.CreatedAt)
                .ThenByDescending(x => x.BaiVietId);

            ViewData["Keyword"] = string.Empty;
            BuildPagination(query, page);

            return View();
        }

        // ====================== DETAIL ======================
        [HttpGet]
        public ActionResult DetailBlog(int? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return RedirectToAction("Index");
            }

            var post = BuildPublishedBlogQuery()
                .FirstOrDefault(x => x.BaiVietId == id.Value);

            if (post == null)
            {
                return RedirectToAction("Index");
            }

            ViewData["listPostPopular"] = GetLatestPosts(post.BaiVietId);

            return View(post);
        }

        // ====================== SEARCH ======================
        [HttpGet]
        public ActionResult FindBlogByName(string keyword, int page = 1)
        {
            if (page <= 0)
            {
                page = 1;
            }

            keyword = NormalizeKeyword(keyword);

            var query = BuildPublishedBlogQuery();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.TieuDe.Contains(keyword) ||
                    (x.TomTat != null && x.TomTat.Contains(keyword)) ||
                    x.NoiDung.Contains(keyword));
            }

            ViewData["Keyword"] = keyword;

            BuildPagination(
                query.OrderByDescending(x => x.NgayDang ?? x.CreatedAt)
                     .ThenByDescending(x => x.BaiVietId),
                page
            );

            return View("Index");
        }

        // ====================== PRIVATE QUERY ======================
        private IQueryable<BaiViet> BuildPublishedBlogQuery()
        {
            return db.BaiViets
                .AsNoTracking()
                .Include(x => x.TaiKhoan)
                .Where(x => x.TrangThai == BlogStatusPublished);
        }

        // ====================== PAGINATION ======================
        private void BuildPagination(IQueryable<BaiViet> query, int page)
        {
            int total = query.Count();
            int totalPages = (int)Math.Ceiling((double)total / PageSize);

            if (totalPages <= 0)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var data = query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewData["listPost"] = data;
            ViewData["listPostPopular"] = GetLatestPosts();

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.DisplayPage = page < 5
                ? 0
                : Math.Max(page - 1, 0);

            ViewBag.NoOfPages = page + 4 > totalPages
                ? totalPages
                : page + 4;
        }

        // ====================== SIDEBAR ======================
        private List<BaiViet> GetLatestPosts(int? excludeId = null)
        {
            var query = BuildPublishedBlogQuery();

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.BaiVietId != excludeId.Value);
            }

            return query
                .OrderByDescending(x => x.NgayDang ?? x.CreatedAt)
                .ThenByDescending(x => x.BaiVietId)
                .Take(SidebarPostTake)
                .ToList();
        }

        private string NormalizeKeyword(string keyword)
        {
            keyword = (keyword ?? string.Empty).Trim();

            while (keyword.Contains("  "))
            {
                keyword = keyword.Replace("  ", " ");
            }

            if (keyword.Length > MaxKeywordLength)
            {
                keyword = keyword.Substring(0, MaxKeywordLength);
            }

            return keyword;
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