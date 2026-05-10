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

        // ========================= DANH SÁCH SẢN PHẨM =========================

        public ActionResult Index(ProductCatalogFilterVm filter)
        {
            if (filter == null)
            {
                filter = new ProductCatalogFilterVm();
            }

            if (filter.Page <= 0) filter.Page = 1;
            if (filter.PageSize <= 0) filter.PageSize = DefaultPageSize;

            db.Database.CommandTimeout = 30;

            IQueryable<SanPham> query = BuildBaseProductQuery();

            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                int categoryId = filter.CategoryId.Value;
                query = query.Where(x => x.LoaiSanPhamId == categoryId);
            }

            ApplyFilters(ref query, filter);

            int totalCount = query.Count();

            List<SanPham> products = query
                .OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.SanPhamId)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            SetPagination(filter.Page, filter.PageSize, totalCount);
            UpdateInterface();

            ViewData["listProduct"] = products;
            ViewBag.Filter = filter;

            return View();
        }

        // ========================= CHI TIẾT SẢN PHẨM =========================

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
                .FirstOrDefault(x =>
                    x.SanPhamId == sanPhamId.Value &&
                    x.TrangThai == ProductStatusActive
                );

            if (product == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Ghi nhận hành vi xem sản phẩm
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

            // Gợi ý sản phẩm bằng Rule-Based Scoring
            List<SanPham> recommendedProducts = GetRuleBasedRecommendedProducts(product, 8);

            ViewBag.RecommendedProducts = recommendedProducts;
            ViewBag.HienThiSanPhamGoiY = recommendedProducts.Any();

            return View(product);
        }
        public ActionResult Detail(int? id)
        {
            return RedirectToAction("DetailProduct", new { sanPhamId = id });
        }

        // ========================= TÌM NHANH =========================

        public ActionResult FindProductById(int? sanPhamId)
        {
            if (!sanPhamId.HasValue || sanPhamId.Value <= 0)
            {
                return RedirectToAction("Index");
            }

            return RedirectToAction("DetailProduct", new { sanPhamId = sanPhamId.Value });
        }

        // ========================= GỢI Ý RULE-BASED =========================

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

            // Lấy lịch sử hành vi gần nhất của người dùng
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
                    .Where(x =>
                        behaviorProductIds.Contains(x.SanPhamId) &&
                        x.TrangThai == ProductStatusActive
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

            decimal avgBehaviorPrice = behaviorProducts.Any()
                ? behaviorProducts.Average(x => x.GiaBan)
                : currentProduct.GiaBan;

            decimal minBehaviorPrice = avgBehaviorPrice * 0.8m;
            decimal maxBehaviorPrice = avgBehaviorPrice * 1.2m;

            var recommendedProducts = db.SanPhams
                .AsNoTracking()
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Where(x =>
                    x.SanPhamId != currentProduct.SanPhamId &&
                    x.TrangThai == ProductStatusActive
                )
                .Select(x => new
                {
                    Product = x,

                    Score =
                        // Ưu tiên sản phẩm cùng loại với sản phẩm đang xem
                        (x.LoaiSanPhamId == currentProduct.LoaiSanPhamId ? 40 : 0)

                        // Ưu tiên sản phẩm cùng thương hiệu với sản phẩm đang xem
                        + (x.ThuongHieuId == currentProduct.ThuongHieuId ? 30 : 0)

                        // Ưu tiên sản phẩm có giá gần với sản phẩm đang xem
                        + (x.GiaBan >= minPrice && x.GiaBan <= maxPrice ? 20 : 0)

                        // Ưu tiên sản phẩm thuộc loại người dùng từng quan tâm
                        + (interestedCategoryIds.Contains(x.LoaiSanPhamId) ? 35 : 0)

                        // Ưu tiên thương hiệu người dùng từng quan tâm
                        + (interestedBrandIds.Contains(x.ThuongHieuId) ? 25 : 0)

                        // Ưu tiên khoảng giá người dùng từng quan tâm
                        + (x.GiaBan >= minBehaviorPrice && x.GiaBan <= maxBehaviorPrice ? 15 : 0)

                        // Ưu tiên sản phẩm nổi bật
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

        // ========================= PRIVATE METHODS =========================

        private IQueryable<SanPham> BuildBaseProductQuery()
        {
            return db.SanPhams
                .AsNoTracking()
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Where(x => x.TrangThai == ProductStatusActive);
        }

        private void ApplyFilters(ref IQueryable<SanPham> query, ProductCatalogFilterVm filter)
        {
            decimal minPrice;
            decimal maxPrice;
            ResolvePriceRange(filter.PriceRange, out minPrice, out maxPrice);

            query = query.Where(x => x.GiaBan >= minPrice && x.GiaBan <= maxPrice);

            if (filter.BrandId.HasValue && filter.BrandId.Value > 0)
            {
                int brandId = filter.BrandId.Value;
                query = query.Where(x => x.ThuongHieuId == brandId);
            }

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                string keyword = filter.Keyword.Trim();
                query = query.Where(x => x.TenSanPham.Contains(keyword));
            }
        }

        private void ResolvePriceRange(string selectedPrice, out decimal minPrice, out decimal maxPrice)
        {
            minPrice = 0m;
            maxPrice = decimal.MaxValue;

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
            else if (price > 10000000)
            {
                minPrice = 10000000m;
                maxPrice = decimal.MaxValue;
            }
        }

        private void SetPagination(int page, int pageSize, int totalCount)
        {
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            if (totalPages <= 0) totalPages = 1;

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

        private void UpdateInterface()
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