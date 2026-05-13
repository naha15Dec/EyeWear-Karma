using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using MatKinh.Models;

namespace MatKinh.Controllers
{
    public class HomeController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int ProductStatusActive = 1;
        private const int BlogStatusPublished = 2;

        public ActionResult Index()
        {
            LoadHomeData();
            return View();
        }

        /// <summary>
        /// Tìm sản phẩm theo mã sản phẩm hoặc ID sản phẩm.
        /// Chỉ cho tìm sản phẩm đang bán, còn hàng, thuộc loại và thương hiệu còn hoạt động.
        /// </summary>
        public ActionResult FindProductByID(string idProduct)
        {
            if (string.IsNullOrWhiteSpace(idProduct))
            {
                return RedirectToAction("Index");
            }

            idProduct = idProduct.Trim();

            var productQuery = db.SanPhams
                .AsNoTracking()
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Where(x =>
                    x.TrangThai == ProductStatusActive &&
                    x.SoLuongTon > 0 &&
                    x.LoaiSanPham.IsActive &&
                    x.ThuongHieu.IsActive);

            var product = productQuery
                .FirstOrDefault(x => x.MaSanPham == idProduct);

            if (product == null && int.TryParse(idProduct, out int sanPhamId))
            {
                product = productQuery
                    .FirstOrDefault(x => x.SanPhamId == sanPhamId);
            }

            if (product == null)
            {
                TempData["HomeSearchError"] = "Không tìm thấy sản phẩm phù hợp hoặc sản phẩm hiện đã ngừng bán.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("DetailProduct", "Product", new { sanPhamId = product.SanPhamId });
        }

        /// <summary>
        /// Nạp dữ liệu cần hiển thị cho trang chủ.
        /// </summary>
        private void LoadHomeData()
        {
            var baseQuery = db.SanPhams
                .AsNoTracking()
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Where(x =>
                    x.TrangThai == ProductStatusActive &&
                    x.SoLuongTon > 0 &&
                    x.LoaiSanPham.IsActive &&
                    x.ThuongHieu.IsActive);

            ViewData["listDiscountProduct"] = baseQuery
                .Where(x => x.GiaGoc > x.GiaBan)
                .OrderByDescending(x => x.GiaGoc - x.GiaBan)
                .ThenByDescending(x => x.CreatedAt)
                .Take(4)
                .ToList();

            ViewData["listNewProduct"] = baseQuery
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.SanPhamId)
                .Take(4)
                .ToList();

            ViewData["listDealHot"] = baseQuery
                .Where(x => x.GiaGoc > x.GiaBan)
                .OrderByDescending(x => x.GiaGoc - x.GiaBan)
                .ThenByDescending(x => x.CreatedAt)
                .Take(2)
                .ToList();

            ViewData["listLatestBlog"] = db.BaiViets
                .AsNoTracking()
                .Where(x => x.TrangThai == BlogStatusPublished)
                .OrderByDescending(x => x.NgayDang ?? x.CreatedAt)
                .Take(3)
                .ToList();

            ViewData["storeInfo"] = db.ThongTinCuaHangs
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefault();
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