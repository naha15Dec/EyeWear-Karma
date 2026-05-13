using System;
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
    public class ManagerProductController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int PRODUCT_STATUS_ACTIVE = 1;
        private const int PRODUCT_STATUS_INACTIVE = 2;

        // Muốn 18 sản phẩm/trang thì đổi dòng này thành 18
        private const int PAGE_SIZE = 10;

        // stock      -> còn hàng
        // outofstock -> hết hàng
        // inactive   -> ngừng bán
        [HttpGet]
        public ActionResult Index(string statusProduct = "stock", string keyword = "", int page = 1)
        {
            var model = BuildIndexViewModel(statusProduct, keyword, page);
            return View(model);
        }

        [HttpGet]
        public ActionResult FindProductByID(string idProduct, string statusProduct = "stock", int page = 1)
        {
            var model = BuildIndexViewModel(statusProduct, idProduct, page);
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(AdminProductEditVm model, HttpPostedFileBase imageAvatar)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu cập nhật không hợp lệ.";
                return RedirectToAction("Index");
            }

            if (model.Page <= 0)
            {
                model.Page = 1;
            }

            var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == model.SanPhamId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index", new
                {
                    statusProduct = model.StatusFilter,
                    keyword = model.Keyword,
                    page = model.Page
                });
            }

            ValidateProductModel(model, product.SanPhamId);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu cập nhật chưa hợp lệ.";
                return RedirectToAction("Index", new
                {
                    statusProduct = model.StatusFilter,
                    keyword = model.Keyword,
                    page = model.Page
                });
            }

            product.MaSanPham = (model.MaSanPham ?? string.Empty).Trim();
            product.TenSanPham = (model.TenSanPham ?? string.Empty).Trim();
            product.MoTaNgan = model.MoTaNgan;
            product.MoTaChiTiet = model.MoTaChiTiet;
            product.GiaGoc = model.GiaGoc;
            product.GiaBan = model.GiaBan;
            product.SoLuongTon = model.SoLuongTon;
            product.ThuongHieuId = model.ThuongHieuId;
            product.LoaiSanPhamId = model.LoaiSanPhamId;
            product.TrangThai = model.TrangThai;
            product.IsFeatured = model.IsFeatured;
            product.UpdatedAt = DateTime.Now;

            if (imageAvatar != null && imageAvatar.ContentLength > 0)
            {
                product.HinhAnhChinh = SaveProductImage(imageAvatar);
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
            return RedirectToAction("Index", new
            {
                statusProduct = model.StatusFilter,
                keyword = model.Keyword,
                page = model.Page
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string statusProduct = "stock", string keyword = "", int page = 1)
        {
            if (page <= 0)
            {
                page = 1;
            }

            var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index", new { statusProduct, keyword, page });
            }

            // Không xóa cứng vì sản phẩm có thể đã nằm trong đơn hàng
            product.TrangThai = PRODUCT_STATUS_INACTIVE;
            product.SoLuongTon = 0;
            product.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã chuyển sản phẩm sang trạng thái ngừng bán.";
            return RedirectToAction("Index", new { statusProduct, keyword, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleFeatured(int id, string statusProduct = "stock", string keyword = "", int page = 1)
        {
            if (page <= 0)
            {
                page = 1;
            }

            var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index", new { statusProduct, keyword, page });
            }

            product.IsFeatured = !product.IsFeatured;
            product.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã cập nhật trạng thái nổi bật.";
            return RedirectToAction("Index", new { statusProduct, keyword, page });
        }

        private AdminProductIndexVm BuildIndexViewModel(string statusProduct, string keyword, int page)
        {
            statusProduct = (statusProduct ?? "stock").Trim().ToLower();
            keyword = (keyword ?? string.Empty).Trim();

            if (page <= 0)
            {
                page = 1;
            }

            var baseQuery = db.SanPhams.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                baseQuery = baseQuery.Where(x =>
                    x.MaSanPham.Contains(keyword) ||
                    x.TenSanPham.Contains(keyword));
            }

            switch (statusProduct)
            {
                case "outofstock":
                    baseQuery = baseQuery.Where(x => x.TrangThai == PRODUCT_STATUS_ACTIVE && x.SoLuongTon <= 0);
                    break;

                case "inactive":
                    baseQuery = baseQuery.Where(x => x.TrangThai == PRODUCT_STATUS_INACTIVE);
                    break;

                default:
                    statusProduct = "stock";
                    baseQuery = baseQuery.Where(x => x.TrangThai == PRODUCT_STATUS_ACTIVE && x.SoLuongTon > 0);
                    break;
            }

            int totalItems = baseQuery.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);

            if (totalPages <= 0)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            int totalStock = baseQuery.Sum(x => (int?)x.SoLuongTon) ?? 0;
            int totalFeatured = baseQuery.Count(x => x.IsFeatured);

            var products = baseQuery
                .OrderByDescending(x => x.UpdatedAt.HasValue ? x.UpdatedAt.Value : x.CreatedAt)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .Select(x => new AdminProductListItemVm
                {
                    SanPhamId = x.SanPhamId,
                    MaSanPham = x.MaSanPham,
                    TenSanPham = x.TenSanPham,
                    ThuongHieuId = x.ThuongHieuId,
                    ThuongHieuTen = x.ThuongHieu.TenThuongHieu,
                    LoaiSanPhamId = x.LoaiSanPhamId,
                    LoaiSanPhamTen = x.LoaiSanPham.TenLoaiSanPham,
                    GiaGoc = x.GiaGoc,
                    GiaBan = x.GiaBan,
                    SoLuongTon = x.SoLuongTon,
                    HinhAnhChinh = x.HinhAnhChinh,
                    MoTaNgan = x.MoTaNgan,
                    MoTaChiTiet = x.MoTaChiTiet,
                    TrangThai = x.TrangThai,
                    IsFeatured = x.IsFeatured,
                    NguoiTao = db.TaiKhoans
                        .Where(tk => tk.TaiKhoanId == x.CreatedById)
                        .Select(tk => tk.HoTen)
                        .FirstOrDefault(),
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList();

            var model = new AdminProductIndexVm
            {
                HeaderTitle = GetHeaderTitle(statusProduct),
                StatusFilter = statusProduct,
                Keyword = keyword,

                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = totalPages,
                TotalStock = totalStock,
                TotalFeatured = totalFeatured,

                Products = products,

                Brands = db.ThuongHieux
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.TenThuongHieu)
                    .Select(x => new SelectListItem
                    {
                        Value = x.ThuongHieuId.ToString(),
                        Text = x.TenThuongHieu
                    })
                    .ToList(),

                Categories = db.LoaiSanPhams
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.TenLoaiSanPham)
                    .Select(x => new SelectListItem
                    {
                        Value = x.LoaiSanPhamId.ToString(),
                        Text = x.TenLoaiSanPham
                    })
                    .ToList()
            };

            return model;
        }

        private string GetHeaderTitle(string statusProduct)
        {
            switch (statusProduct)
            {
                case "outofstock":
                    return "Danh sách hết hàng";

                case "inactive":
                    return "Danh sách ngừng bán";

                default:
                    return "Danh sách còn hàng";
            }
        }

        private void ValidateProductModel(AdminProductEditVm model, int currentProductId)
        {
            if (string.IsNullOrWhiteSpace(model.MaSanPham))
            {
                ModelState.AddModelError("MaSanPham", "Mã sản phẩm không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(model.TenSanPham))
            {
                ModelState.AddModelError("TenSanPham", "Tên sản phẩm không được để trống.");
            }

            if (model.GiaGoc < 0)
            {
                ModelState.AddModelError("GiaGoc", "Giá gốc không hợp lệ.");
            }

            if (model.GiaBan < 0)
            {
                ModelState.AddModelError("GiaBan", "Giá bán không hợp lệ.");
            }

            if (model.SoLuongTon < 0)
            {
                ModelState.AddModelError("SoLuongTon", "Số lượng tồn không hợp lệ.");
            }

            if (model.GiaGoc > 0 && model.GiaBan > model.GiaGoc)
            {
                ModelState.AddModelError("GiaBan", "Giá bán không được lớn hơn giá gốc.");
            }

            if (model.TrangThai != PRODUCT_STATUS_ACTIVE && model.TrangThai != PRODUCT_STATUS_INACTIVE)
            {
                ModelState.AddModelError("TrangThai", "Trạng thái sản phẩm không hợp lệ.");
            }

            var duplicatedCode = db.SanPhams.Any(x =>
                x.SanPhamId != currentProductId &&
                x.MaSanPham == model.MaSanPham);

            if (duplicatedCode)
            {
                ModelState.AddModelError("MaSanPham", "Mã sản phẩm đã tồn tại.");
            }

            var brandExists = db.ThuongHieux.Any(x =>
                x.ThuongHieuId == model.ThuongHieuId &&
                x.IsActive);

            if (!brandExists)
            {
                ModelState.AddModelError("ThuongHieuId", "Thương hiệu không tồn tại hoặc đã bị khóa.");
            }

            var categoryExists = db.LoaiSanPhams.Any(x =>
                x.LoaiSanPhamId == model.LoaiSanPhamId &&
                x.IsActive);

            if (!categoryExists)
            {
                ModelState.AddModelError("LoaiSanPhamId", "Loại sản phẩm không tồn tại hoặc đã bị khóa.");
            }
        }

        private string SaveProductImage(HttpPostedFileBase image)
        {
            string virtualFolder = "/Asset/SaveImgProduct/";
            string physicalFolder = Server.MapPath("~" + virtualFolder);

            if (!Directory.Exists(physicalFolder))
            {
                Directory.CreateDirectory(physicalFolder);
            }

            string fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
            string fullPath = Path.Combine(physicalFolder, fileName);

            image.SaveAs(fullPath);

            return virtualFolder + fileName;
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