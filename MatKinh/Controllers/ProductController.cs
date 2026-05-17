using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;
using MatKinh.Services;

namespace MatKinh.Controllers
{
    public class ProductController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int ProductStatusActive = 1;
        private const int DefaultPageSize = 9;
        private const int MaxPageSize = 24;

        public ActionResult Index(ProductCatalogFilterVm filter)
        {
            if (filter == null)
            {
                filter = new ProductCatalogFilterVm();
            }

            if (filter.Page <= 0)
            {
                filter.Page = 1;
            }

            if (filter.PageSize <= 0)
            {
                filter.PageSize = DefaultPageSize;
            }

            if (filter.PageSize > MaxPageSize)
            {
                filter.PageSize = MaxPageSize;
            }

            db.Database.CommandTimeout = 30;

            IQueryable<SanPham> query = BuildBaseProductQuery();

            ApplyFilters(ref query, filter);

            int totalCount = query.Count();

            int totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);
            if (totalPages <= 0)
            {
                totalPages = 1;
            }

            if (filter.Page > totalPages)
            {
                filter.Page = totalPages;
            }

            List<SanPham> products = query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.SanPhamId)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            SetPagination(filter.Page, filter.PageSize, totalCount);
            LoadFilterData();

            ViewData["listProduct"] = products;
            ViewBag.Filter = filter;

            return View(filter);
        }

        public ActionResult DetailProduct(int? sanPhamId)
        {
            if (!sanPhamId.HasValue || sanPhamId.Value <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            db.Database.CommandTimeout = 30;

            SanPham product = db.SanPhams
                .AsNoTracking()
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Include(x => x.KieuGong)
                .FirstOrDefault(x =>
                    x.SanPhamId == sanPhamId.Value &&
                    x.TrangThai == ProductStatusActive &&
                    x.LoaiSanPham.IsActive &&
                    x.ThuongHieu.IsActive
                );

            if (product == null)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                UserBehaviorLogger.Log(
                    db,
                    Session,
                    product.SanPhamId,
                    UserBehaviorConstants.VIEW,
                    UserBehaviorConstants.VIEW_WEIGHT,
                    "PRODUCT_DETAIL",
                    null
                );

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            List<DanhGiaSanPham> reviews = db.DanhGiaSanPhams
                .AsNoTracking()
                .Include(x => x.KhachHang)
                .Where(x =>
                    x.SanPhamId == product.SanPhamId &&
                    x.TrangThai == ReviewStatusConstants.APPROVED)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            int reviewCount = reviews.Count;

            decimal averageRating = reviewCount > 0
                ? Math.Round((decimal)reviews.Average(x => x.SoSao), 1)
                : 0m;

            List<SanPham> recommendedProducts = GetRuleBasedRecommendedProducts(product, 8);

            ViewBag.ProductReviews = reviews;
            ViewBag.ReviewCount = reviewCount;
            ViewBag.AverageRating = averageRating;

            ViewBag.RecommendedProducts = recommendedProducts;
            ViewBag.HienThiSanPhamGoiY = recommendedProducts.Any();

            return View(product);
        }

        public ActionResult Detail(int? id)
        {
            return RedirectToAction("DetailProduct", new { sanPhamId = id });
        }

        public ActionResult FindProductById(int? sanPhamId)
        {
            if (!sanPhamId.HasValue || sanPhamId.Value <= 0)
            {
                return RedirectToAction("Index");
            }

            return RedirectToAction("DetailProduct", new { sanPhamId = sanPhamId.Value });
        }

        private List<SanPham> GetRuleBasedRecommendedProducts(SanPham currentProduct, int take = 8)
        {
            if (currentProduct == null)
            {
                return new List<SanPham>();
            }

            int? khachHangId = GetCurrentKhachHangId();

            string sessionId = Session["BehaviorSessionId"] != null
                ? Session["BehaviorSessionId"].ToString()
                : Session.SessionID;

            var behaviorQuery = db.HanhViNguoiDungs
                .AsNoTracking()
                .Where(x =>
                    x.SanPhamId != currentProduct.SanPhamId &&
                    (
                        (khachHangId.HasValue && x.KhachHangId == khachHangId.Value) ||
                        (!khachHangId.HasValue && x.SessionId == sessionId)
                    )
                )
                .OrderByDescending(x => x.CreatedAt)
                .Take(30);

            List<int> behaviorProductIds = behaviorQuery
                .Select(x => x.SanPhamId)
                .Distinct()
                .ToList();

            List<SanPham> behaviorProducts = new List<SanPham>();

            if (behaviorProductIds.Any())
            {
                behaviorProducts = db.SanPhams
                    .AsNoTracking()
                    .Include(x => x.LoaiSanPham)
                    .Include(x => x.ThuongHieu)
                    .Include(x => x.KieuGong)
                    .Where(x =>
                        behaviorProductIds.Contains(x.SanPhamId) &&
                        x.TrangThai == ProductStatusActive &&
                        x.SoLuongTon > 0 &&
                        x.LoaiSanPham.IsActive &&
                        x.ThuongHieu.IsActive
                    )
                    .ToList();
            }

            decimal currentPrice = currentProduct.GiaBan;
            decimal minPrice = currentPrice * 0.8m;
            decimal maxPrice = currentPrice * 1.2m;

            List<int> interestedCategoryIds = behaviorProducts
                .Select(x => x.LoaiSanPhamId)
                .Distinct()
                .ToList();

            List<int> interestedBrandIds = behaviorProducts
                .Select(x => x.ThuongHieuId)
                .Distinct()
                .ToList();

            List<int> interestedFrameTypeIds = behaviorProducts
                .Where(x => x.KieuGongId.HasValue)
                .Select(x => x.KieuGongId.Value)
                .Distinct()
                .ToList();

            decimal avgBehaviorPrice = behaviorProducts.Any()
                ? behaviorProducts.Average(x => x.GiaBan)
                : currentProduct.GiaBan;

            decimal minBehaviorPrice = avgBehaviorPrice * 0.8m;
            decimal maxBehaviorPrice = avgBehaviorPrice * 1.2m;

            var recommendedProducts = db.SanPhams
                .AsNoTracking()
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Include(x => x.KieuGong)
                .Where(x =>
                    x.SanPhamId != currentProduct.SanPhamId &&
                    x.TrangThai == ProductStatusActive &&
                    x.SoLuongTon > 0 &&
                    x.LoaiSanPham.IsActive &&
                    x.ThuongHieu.IsActive
                )
                .Select(x => new
                {
                    Product = x,

                    Score =
                        (x.LoaiSanPhamId == currentProduct.LoaiSanPhamId ? 40 : 0)
                        + (x.ThuongHieuId == currentProduct.ThuongHieuId ? 30 : 0)
                        + (x.KieuGongId.HasValue && currentProduct.KieuGongId.HasValue && x.KieuGongId.Value == currentProduct.KieuGongId.Value ? 35 : 0)
                        + (x.GiaBan >= minPrice && x.GiaBan <= maxPrice ? 20 : 0)
                        + (interestedCategoryIds.Contains(x.LoaiSanPhamId) ? 35 : 0)
                        + (interestedBrandIds.Contains(x.ThuongHieuId) ? 25 : 0)
                        + (x.KieuGongId.HasValue && interestedFrameTypeIds.Contains(x.KieuGongId.Value) ? 25 : 0)
                        + (x.GiaBan >= minBehaviorPrice && x.GiaBan <= maxBehaviorPrice ? 15 : 0)
                        + (x.IsFeatured ? 10 : 0)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Product.CreatedAt)
                .Take(take)
                .Select(x => x.Product)
                .ToList();

            return recommendedProducts;
        }

        private int? GetCurrentKhachHangId()
        {
            if (Session["KhachHangId"] == null)
            {
                return null;
            }

            try
            {
                return Convert.ToInt32(Session["KhachHangId"]);
            }
            catch
            {
                return null;
            }
        }

        private IQueryable<SanPham> BuildBaseProductQuery()
        {
            return db.SanPhams
                .AsNoTracking()
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Include(x => x.KieuGong)
                .Where(x =>
                    x.TrangThai == ProductStatusActive &&
                    x.LoaiSanPham.IsActive &&
                    x.ThuongHieu.IsActive);
        }

        private void ApplyFilters(ref IQueryable<SanPham> query, ProductCatalogFilterVm filter)
        {
            decimal minPrice;
            decimal? maxPrice;

            ResolvePriceRange(filter.PriceRange, out minPrice, out maxPrice);

            query = query.Where(x => x.GiaBan >= minPrice);

            if (maxPrice.HasValue)
            {
                decimal max = maxPrice.Value;
                query = query.Where(x => x.GiaBan <= max);
            }

            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                int categoryId = filter.CategoryId.Value;
                query = query.Where(x => x.LoaiSanPhamId == categoryId);
            }

            if (filter.BrandId.HasValue && filter.BrandId.Value > 0)
            {
                int brandId = filter.BrandId.Value;
                query = query.Where(x => x.ThuongHieuId == brandId);
            }

            if (filter.KieuGongId.HasValue && filter.KieuGongId.Value > 0)
            {
                int frameTypeId = filter.KieuGongId.Value;
                query = query.Where(x => x.KieuGongId == frameTypeId);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                string keyword = filter.Keyword.Trim();

                query = query.Where(x =>
                    x.TenSanPham.Contains(keyword) ||
                    x.MaSanPham.Contains(keyword) ||
                    x.ThuongHieu.TenThuongHieu.Contains(keyword) ||
                    x.LoaiSanPham.TenLoaiSanPham.Contains(keyword) ||
                    (x.KieuGong != null && x.KieuGong.TenKieuGong.Contains(keyword))
                );
            }
        }

        private void ResolvePriceRange(string selectedPrice, out decimal minPrice, out decimal? maxPrice)
        {
            minPrice = 0m;
            maxPrice = null;

            if (string.IsNullOrWhiteSpace(selectedPrice))
            {
                return;
            }

            if (!int.TryParse(selectedPrice, out int price))
            {
                return;
            }

            if (price == 500000)
            {
                minPrice = 0m;
                maxPrice = 500000m;
            }
            else if (price == 3000000)
            {
                minPrice = 500000m;
                maxPrice = 3000000m;
            }
            else if (price == 5000000)
            {
                minPrice = 3000000m;
                maxPrice = 5000000m;
            }
            else if (price == 10000000)
            {
                minPrice = 5000000m;
                maxPrice = 10000000m;
            }
            else if (price == 10000001 || price > 10000000)
            {
                minPrice = 10000000m;
                maxPrice = null;
            }
        }

        private void SetPagination(int page, int pageSize, int totalCount)
        {
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            if (totalPages <= 0)
            {
                totalPages = 1;
            }

            int currentPage = page > totalPages ? totalPages : page;

            int displayStart = currentPage < 5
                ? 0
                : ((currentPage - 1) >= (totalPages - 5)
                    ? Math.Max(totalPages - 5, 0)
                    : (currentPage - 1));

            int displayEnd = currentPage + 4 > totalPages
                ? totalPages
                : currentPage + 4;

            ViewBag.Page = currentPage;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = totalPages;
            ViewBag.NoOfPages = displayEnd;
            ViewBag.DisplayPage = displayStart;
        }

        private void LoadFilterData()
        {
            ViewData["brandList"] = db.ThuongHieux
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.TenThuongHieu)
                .ToList();

            ViewData["typeProductList"] = db.LoaiSanPhams
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.TenLoaiSanPham)
                .ToList();

            ViewData["frameTypeList"] = db.KieuGongs
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.TenKieuGong)
                .ToList();
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