using System;
using System.Collections.Generic;
using System.Data.Entity;
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
    public class ListProductController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const string VIRTUAL_IMG_FOLDER = "/Asset/SaveImgProduct";
        private const int PRODUCT_STATUS_ACTIVE = 1;
        private const int PRODUCT_STATUS_INACTIVE = 2;
        private const int PAGE_SIZE = 10;

        [HttpGet]
        public ActionResult ProductsList(string keyword = "", int page = 1)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var model = BuildProductListViewModel(currentUser, keyword, page);
            return View(model);
        }

        [HttpGet]
        public ActionResult AddProduct()
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var model = BuildCreateViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult AddProduct(AdminProductCreateVm model, HttpPostedFileBase imageAvatar)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            NormalizeCreateModel(model);
            ValidateProductCreateModel(model, null);

            if (!ModelState.IsValid)
            {
                LoadCreateDropdowns(model);
                return View(model);
            }

            try
            {
                var product = new SanPham
                {
                    MaSanPham = GenerateProductCode(),
                    TenSanPham = model.TenSanPham,
                    MoTaNgan = model.MoTaNgan,
                    MoTaChiTiet = model.MoTaChiTiet,

                    GiaGoc = model.GiaGoc,
                    GiaBan = model.GiaBan,
                    SoLuongTon = model.SoLuongTon,

                    ThuongHieuId = model.ThuongHieuId,
                    LoaiSanPhamId = model.LoaiSanPhamId,
                    KieuGongId = model.KieuGongId.Value,

                    TrangThai = model.TrangThai,
                    IsFeatured = model.IsFeatured,

                    CreatedById = currentUser.TaiKhoanId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null,

                    HinhAnhChinh = string.Empty
                };

                if (imageAvatar != null && imageAvatar.ContentLength > 0)
                {
                    product.HinhAnhChinh = SaveProductImage(imageAvatar);
                }

                db.SanPhams.Add(product);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công.";
                return RedirectToAction("ProductsList");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Thêm sản phẩm thất bại: " + ex.Message);
                LoadCreateDropdowns(model);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult Update(int id, AdminProductCreateVm model, HttpPostedFileBase imageAvatar)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("ProductsList");
            }

            if (!CanManageProduct(currentUser, product))
            {
                return Redirect("~/Error/Index");
            }

            NormalizeCreateModel(model);
            ValidateProductCreateModel(model, id);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu cập nhật chưa hợp lệ.";
                return RedirectToAction("ProductsList");
            }

            try
            {
                product.TenSanPham = model.TenSanPham;
                product.MoTaNgan = model.MoTaNgan;
                product.MoTaChiTiet = model.MoTaChiTiet;

                product.GiaGoc = model.GiaGoc;
                product.GiaBan = model.GiaBan;
                product.SoLuongTon = model.SoLuongTon;

                product.ThuongHieuId = model.ThuongHieuId;
                product.LoaiSanPhamId = model.LoaiSanPhamId;
                product.KieuGongId = model.KieuGongId.Value;

                product.TrangThai = model.TrangThai;
                product.IsFeatured = model.IsFeatured;
                product.UpdatedAt = DateTime.Now;

                if (imageAvatar != null && imageAvatar.ContentLength > 0)
                {
                    DeleteImageFileOnDisk(product.HinhAnhChinh);
                    product.HinhAnhChinh = SaveProductImage(imageAvatar);
                }

                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
                return RedirectToAction("ProductsList");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Cập nhật sản phẩm thất bại: " + ex.Message;
                return RedirectToAction("ProductsList");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("ProductsList");
            }

            if (!CanManageProduct(currentUser, product))
            {
                return Redirect("~/Error/Index");
            }

            product.TrangThai = PRODUCT_STATUS_INACTIVE;
            product.SoLuongTon = 0;
            product.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã chuyển sản phẩm sang trạng thái ngừng bán.";
            return RedirectToAction("ProductsList");
        }

        [HttpGet]
        public ActionResult FindProductByName(string keyword)
        {
            return RedirectToAction("ProductsList", new { keyword, page = 1 });
        }

        private AdminProductListPageVm BuildProductListViewModel(TaiKhoan currentUser, string keyword, int page)
        {
            keyword = (keyword ?? string.Empty).Trim();

            if (page <= 0)
            {
                page = 1;
            }

            var query = db.SanPhams
                .Include(x => x.ThuongHieu)
                .Include(x => x.LoaiSanPham)
                .Include(x => x.KieuGong)
                .AsQueryable();

            if (!IsAdmin(currentUser))
            {
                query = query.Where(x => x.CreatedById == currentUser.TaiKhoanId);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaSanPham.Contains(keyword) ||
                    x.TenSanPham.Contains(keyword));
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

            var products = query
                .OrderByDescending(x => x.UpdatedAt.HasValue ? x.UpdatedAt.Value : x.CreatedAt)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .Select(x => new AdminProductListPageItemVm
                {
                    SanPhamId = x.SanPhamId,
                    MaSanPham = x.MaSanPham,
                    TenSanPham = x.TenSanPham,

                    MoTaNgan = x.MoTaNgan,
                    MoTaChiTiet = x.MoTaChiTiet,
                    HinhAnhChinh = x.HinhAnhChinh,

                    GiaGoc = x.GiaGoc,
                    GiaBan = x.GiaBan,
                    SoLuongTon = x.SoLuongTon,

                    ThuongHieuId = x.ThuongHieuId,
                    ThuongHieuTen = x.ThuongHieu != null ? x.ThuongHieu.TenThuongHieu : "Không xác định",

                    LoaiSanPhamId = x.LoaiSanPhamId,
                    LoaiSanPhamTen = x.LoaiSanPham != null ? x.LoaiSanPham.TenLoaiSanPham : "Không xác định",

                    KieuGongId = x.KieuGongId,
                    MaKieuGong = x.KieuGong != null ? x.KieuGong.MaKieuGong : "",
                    TenKieuGong = x.KieuGong != null ? x.KieuGong.TenKieuGong : "Chưa chọn kiểu gọng",

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

            return new AdminProductListPageVm
            {
                Keyword = keyword,
                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Products = products,
                Brands = GetBrandOptions(),
                Categories = GetCategoryOptions(),
                FrameTypes = GetFrameTypeOptions()
            };
        }

        private AdminProductCreateVm BuildCreateViewModel()
        {
            var model = new AdminProductCreateVm
            {
                TrangThai = PRODUCT_STATUS_ACTIVE,
                IsFeatured = false,
                GiaGoc = 0,
                GiaBan = 0,
                SoLuongTon = 0
            };

            LoadCreateDropdowns(model);

            return model;
        }

        private void LoadCreateDropdowns(AdminProductCreateVm model)
        {
            if (model == null)
            {
                return;
            }

            model.Brands = GetBrandOptions();
            model.Categories = GetCategoryOptions();
            model.FrameTypes = GetFrameTypeOptions();
        }

        private List<SelectListItem> GetBrandOptions()
        {
            return db.ThuongHieux
                .Where(x => x.IsActive)
                .OrderBy(x => x.TenThuongHieu)
                .Select(x => new SelectListItem
                {
                    Value = x.ThuongHieuId.ToString(),
                    Text = x.TenThuongHieu
                })
                .ToList();
        }

        private List<SelectListItem> GetCategoryOptions()
        {
            return db.LoaiSanPhams
                .Where(x => x.IsActive)
                .OrderBy(x => x.TenLoaiSanPham)
                .Select(x => new SelectListItem
                {
                    Value = x.LoaiSanPhamId.ToString(),
                    Text = x.TenLoaiSanPham
                })
                .ToList();
        }

        private List<SelectListItem> GetFrameTypeOptions()
        {
            return db.KieuGongs
                .Where(x => x.IsActive)
                .OrderBy(x => x.TenKieuGong)
                .Select(x => new SelectListItem
                {
                    Value = x.KieuGongId.ToString(),
                    Text = x.TenKieuGong + " (#" + x.MaKieuGong + ")"
                })
                .ToList();
        }

        private TaiKhoan GetCurrentAccount()
        {
            var sessionAccount = Session["LoginInformation"] as TaiKhoan;
            if (sessionAccount == null)
            {
                return null;
            }

            return db.TaiKhoans
                .Include("VaiTro")
                .FirstOrDefault(x => x.TaiKhoanId == sessionAccount.TaiKhoanId && x.IsActive);
        }

        private bool IsAdmin(TaiKhoan currentUser)
        {
            return currentUser != null &&
                   currentUser.VaiTro != null &&
                   string.Equals(currentUser.VaiTro.MaVaiTro, RoleConstants.ADMIN, StringComparison.OrdinalIgnoreCase);
        }

        private bool CanManageProduct(TaiKhoan currentUser, SanPham product)
        {
            if (currentUser == null || product == null)
            {
                return false;
            }

            if (IsAdmin(currentUser))
            {
                return true;
            }

            return product.CreatedById == currentUser.TaiKhoanId;
        }

        private void NormalizeCreateModel(AdminProductCreateVm model)
        {
            if (model == null)
            {
                return;
            }

            model.TenSanPham = string.IsNullOrWhiteSpace(model.TenSanPham)
                ? string.Empty
                : model.TenSanPham.Trim();

            model.MoTaNgan = string.IsNullOrWhiteSpace(model.MoTaNgan)
                ? null
                : model.MoTaNgan.Trim();

            model.MoTaChiTiet = string.IsNullOrWhiteSpace(model.MoTaChiTiet)
                ? null
                : model.MoTaChiTiet.Trim();

            if (model.TrangThai != PRODUCT_STATUS_ACTIVE &&
                model.TrangThai != PRODUCT_STATUS_INACTIVE)
            {
                model.TrangThai = PRODUCT_STATUS_ACTIVE;
            }
        }

        private void ValidateProductCreateModel(AdminProductCreateVm model, int? currentProductId)
        {
            if (model == null)
            {
                ModelState.AddModelError("", "Dữ liệu sản phẩm không hợp lệ.");
                return;
            }

            if (string.IsNullOrWhiteSpace(model.TenSanPham))
            {
                ModelState.AddModelError("TenSanPham", "Vui lòng nhập tên sản phẩm.");
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

            if (model.TrangThai != PRODUCT_STATUS_ACTIVE &&
                model.TrangThai != PRODUCT_STATUS_INACTIVE)
            {
                ModelState.AddModelError("TrangThai", "Trạng thái sản phẩm không hợp lệ.");
            }

            bool brandExists = db.ThuongHieux.Any(x =>
                x.ThuongHieuId == model.ThuongHieuId &&
                x.IsActive);

            if (!brandExists)
            {
                ModelState.AddModelError("ThuongHieuId", "Thương hiệu không hợp lệ.");
            }

            bool categoryExists = db.LoaiSanPhams.Any(x =>
                x.LoaiSanPhamId == model.LoaiSanPhamId &&
                x.IsActive);

            if (!categoryExists)
            {
                ModelState.AddModelError("LoaiSanPhamId", "Loại sản phẩm không hợp lệ.");
            }

            if (!model.KieuGongId.HasValue || model.KieuGongId.Value <= 0)
            {
                ModelState.AddModelError("KieuGongId", "Vui lòng chọn kiểu gọng.");
            }
            else
            {
                bool frameTypeExists = db.KieuGongs.Any(x =>
                    x.KieuGongId == model.KieuGongId.Value &&
                    x.IsActive);

                if (!frameTypeExists)
                {
                    ModelState.AddModelError("KieuGongId", "Kiểu gọng không hợp lệ.");
                }
            }
        }

        private string GenerateProductCode()
        {
            string code;

            do
            {
                code = "SP" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
            while (db.SanPhams.Any(x => x.MaSanPham == code));

            return code;
        }

        private string SaveProductImage(HttpPostedFileBase image)
        {
            if (image == null || image.ContentLength <= 0)
            {
                return string.Empty;
            }

            const int maxSize = 3 * 1024 * 1024;
            if (image.ContentLength > maxSize)
            {
                throw new InvalidOperationException("Ảnh sản phẩm không được vượt quá 3MB.");
            }

            var ext = Path.GetExtension(image.FileName)?.ToLowerInvariant();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp"
            };

            if (string.IsNullOrWhiteSpace(ext) || !allowed.Contains(ext))
            {
                throw new InvalidOperationException("Định dạng ảnh không hợp lệ. Chỉ cho phép jpg, jpeg, png, gif, webp.");
            }

            var physicalFolder = Server.MapPath("~" + VIRTUAL_IMG_FOLDER);
            Directory.CreateDirectory(physicalFolder);

            var fileName = Guid.NewGuid().ToString("N") + ext;
            var fullPath = Path.Combine(physicalFolder, fileName);
            image.SaveAs(fullPath);

            return VIRTUAL_IMG_FOLDER + "/" + fileName;
        }

        private void DeleteImageFileOnDisk(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
            {
                return;
            }

            var cleanPath = StripVersion(virtualPath);
            var fileName = Path.GetFileName(cleanPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var physicalFolder = Server.MapPath("~" + VIRTUAL_IMG_FOLDER);
            var filePath = Path.Combine(physicalFolder, fileName);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private static string StripVersion(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            var q = path.IndexOf('?');
            return q > -1 ? path.Substring(0, q) : path;
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