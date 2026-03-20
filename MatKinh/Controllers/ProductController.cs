using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.Models.DTO;
using MatKinh.ViewModel;
using Newtonsoft.Json;

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

            // 1. Danh mục = ngữ cảnh trang
            if (filter.CategoryId.HasValue && filter.CategoryId.Value > 0)
            {
                int categoryId = filter.CategoryId.Value;
                query = query.Where(x => x.LoaiSanPhamId == categoryId);
            }

            // 2. Bộ lọc bên trong danh mục
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

        public async Task<ActionResult> DetailProduct(int? sanPhamId)
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
                .FirstOrDefault(x => x.SanPhamId == sanPhamId.Value && x.TrangThai == ProductStatusActive);

            if (product == null)
            {
                return RedirectToAction("Index", "Home");
            }

            List<SanPham> relatedProducts = db.SanPhams
                .AsNoTracking()
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Where(x =>
                    x.SanPhamId != product.SanPhamId &&
                    x.TrangThai == ProductStatusActive &&
                    (
                        x.LoaiSanPhamId == product.LoaiSanPhamId ||
                        x.ThuongHieuId == product.ThuongHieuId
                    ))
                .OrderByDescending(x => x.CreatedAt)
                .Take(8)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            List<int> sanPhamGoiYIds = await GetRecommendedProductIds(product.SanPhamId);
            ViewBag.HienThiSanPhamGoiY = sanPhamGoiYIds;

            if (sanPhamGoiYIds.Any())
            {
                List<SanPham> recommendedProducts = db.SanPhams
                    .AsNoTracking()
                    .Include(x => x.LoaiSanPham)
                    .Include(x => x.ThuongHieu)
                    .Where(x => sanPhamGoiYIds.Contains(x.SanPhamId) && x.TrangThai == ProductStatusActive)
                    .ToList();

                recommendedProducts = recommendedProducts
                    .OrderBy(x => sanPhamGoiYIds.IndexOf(x.SanPhamId))
                    .ToList();

                ViewBag.RecommendedProducts = recommendedProducts;
            }
            else
            {
                ViewBag.RecommendedProducts = new List<SanPham>();
            }

            return View(product);
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

        private async Task<List<int>> GetRecommendedProductIds(int sanPhamId)
        {
            List<int> result = new List<int>();

            try
            {
                string baseUrl = "http://localhost:5555/";

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(baseUrl);
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpResponseMessage response = await client.GetAsync($"api?id={sanPhamId}");

                    if (!response.IsSuccessStatusCode)
                    {
                        return result;
                    }

                    string content = await response.Content.ReadAsStringAsync();
                    var productResponse = JsonConvert.DeserializeObject<ProductResponse>(content);

                    if (productResponse?.SanPhamGoiY == null || !productResponse.SanPhamGoiY.Any())
                    {
                        return result;
                    }

                    foreach (var item in productResponse.SanPhamGoiY)
                    {
                        if (int.TryParse(item, out int id))
                        {
                            result.Add(id);
                        }
                    }
                }
            }
            catch
            {
            }

            return result.Distinct().ToList();
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
            else if (price >= 10000000)
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
                : ((currentPage - 1) >= (totalPages - 5) ? Math.Max(totalPages - 5, 0) : (currentPage - 1));

            int displayEnd = currentPage + 4 > totalPages ? totalPages : currentPage + 4;

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