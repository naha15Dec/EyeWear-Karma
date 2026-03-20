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

        public ActionResult Index()
        {
            UpdateInterface();
            return View();
        }

        /// <summary>
        /// Tìm sản phẩm theo mã sản phẩm hoặc id sản phẩm.
        /// </summary>
        public ActionResult FindProductByID(string idProduct)
        {
            if (string.IsNullOrWhiteSpace(idProduct))
            {
                return RedirectToAction("Index");
            }

            idProduct = idProduct.Trim();

            SanPham product = null;

            // Ưu tiên tìm theo mã sản phẩm nghiệp vụ
            product = db.SanPhams
                .AsNoTracking()
                .FirstOrDefault(x => x.MaSanPham == idProduct && x.TrangThai == ProductStatusActive);

            // Nếu người dùng nhập id số thì tìm thêm theo khóa chính
            if (product == null && int.TryParse(idProduct, out int sanPhamId))
            {
                product = db.SanPhams
                    .AsNoTracking()
                    .FirstOrDefault(x => x.SanPhamId == sanPhamId && x.TrangThai == ProductStatusActive);
            }

            if (product == null)
            {
                TempData["HomeSearchError"] = "Không tìm thấy sản phẩm phù hợp.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("DetailProduct", "Product", new { sanPhamId = product.SanPhamId });
        }

        /// <summary>
        /// Nạp dữ liệu trang chủ theo DB mới.
        /// </summary>
        private void UpdateInterface()
        {
            var baseQuery = db.SanPhams
                .AsNoTracking()
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Where(x => x.TrangThai == ProductStatusActive);

            // Sản phẩm đang giảm giá: GiaGoc > GiaBan
            ViewData["listDiscountProduct"] = baseQuery
                .Where(x => x.GiaGoc > x.GiaBan)
                .OrderByDescending(x => (x.GiaGoc - x.GiaBan))
                .ThenByDescending(x => x.CreatedAt)
                .Take(8)
                .ToList();

            // Sản phẩm mới: ưu tiên sản phẩm active mới tạo gần đây
            ViewData["listNewProduct"] = baseQuery
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.SanPhamId)
                .Take(8)
                .ToList();

            // Deal hot: sản phẩm giảm mạnh nhất
            ViewData["listDealHot"] = baseQuery
                .Where(x => x.GiaGoc > x.GiaBan)
                .OrderByDescending(x => (x.GiaGoc - x.GiaBan))
                .ThenByDescending(x => x.CreatedAt)
                .Take(2)
                .ToList();

            // Blog mới nhất cho homepage nếu cần dùng luôn ở view
            ViewData["listLatestBlog"] = db.BaiViets
                .AsNoTracking()
                .Where(x => x.TrangThai == 2)
                .OrderByDescending(x => x.NgayDang ?? x.CreatedAt)
                .Take(3)
                .ToList();

            // Thông tin cửa hàng nếu homepage đang dùng
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