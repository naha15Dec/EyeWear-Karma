using System;
using System.Collections.Generic;
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

        // ================= LIST =================

        [HttpGet]
        public ActionResult ProductsList(string keyword = "")
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var model = BuildProductListViewModel(currentUser.TaiKhoanId, keyword);
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

        // ================= CREATE =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddProduct(AdminProductCreateVm model, HttpPostedFileBase imageAvatar)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            ValidateProductCreateModel(model, null);

            if (!ModelState.IsValid)
            {
                model.Brands = GetBrandOptions();
                model.Categories = GetCategoryOptions();
                return View(model);
            }

            var product = new SanPham
            {
                MaSanPham = GenerateProductCode(),
                TenSanPham = (model.TenSanPham ?? string.Empty).Trim(),
                MoTaNgan = string.IsNullOrWhiteSpace(model.MoTaNgan) ? null : model.MoTaNgan.Trim(),
                MoTaChiTiet = string.IsNullOrWhiteSpace(model.MoTaChiTiet) ? null : model.MoTaChiTiet.Trim(),
                GiaGoc = model.GiaGoc,
                GiaBan = model.GiaBan,
                SoLuongTon = model.SoLuongTon,
                ThuongHieuId = model.ThuongHieuId,
                LoaiSanPhamId = model.LoaiSanPhamId,
                TrangThai = model.TrangThai,
                IsFeatured = model.IsFeatured,
                CreatedById = currentUser.TaiKhoanId,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            if (imageAvatar != null && imageAvatar.ContentLength > 0)
            {
                product.HinhAnhChinh = SaveProductImage(imageAvatar);
            }
            else
            {
                product.HinhAnhChinh = string.Empty;
            }

            db.SanPhams.Add(product);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Thêm sản phẩm thành công.";
            return RedirectToAction("ProductsList");
        }

        // ================= UPDATE =================

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            ValidateProductCreateModel(model, id);

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu cập nhật chưa hợp lệ.";
                return RedirectToAction("ProductsList");
            }

            product.TenSanPham = (model.TenSanPham ?? string.Empty).Trim();
            product.MoTaNgan = string.IsNullOrWhiteSpace(model.MoTaNgan) ? null : model.MoTaNgan.Trim();
            product.MoTaChiTiet = string.IsNullOrWhiteSpace(model.MoTaChiTiet) ? null : model.MoTaChiTiet.Trim();
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
                DeleteImageFileOnDisk(product.HinhAnhChinh);
                product.HinhAnhChinh = SaveProductImage(imageAvatar);
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
            return RedirectToAction("ProductsList");
        }

        // ================= DELETE =================

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

            bool hasOrderDetail = db.ChiTietDonHangs.Any(x => x.SanPhamId == product.SanPhamId);
            if (hasOrderDetail)
            {
                product.TrangThai = 2; // ngừng bán
                product.SoLuongTon = 0;
                product.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Sản phẩm đã được chuyển sang trạng thái ngừng bán vì đã phát sinh đơn hàng.";
                return RedirectToAction("ProductsList");
            }

            DeleteImageFileOnDisk(product.HinhAnhChinh);
            db.SanPhams.Remove(product);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Xóa sản phẩm thành công.";
            return RedirectToAction("ProductsList");
        }

        // ================= SEARCH =================

        [HttpGet]
        public ActionResult FindProductByName(string keyword)
        {
            return RedirectToAction("ProductsList", new { keyword });
        }

        // ================= PRIVATE =================

        private AdminProductListPageVm BuildProductListViewModel(int currentUserId, string keyword)
        {
            keyword = (keyword ?? string.Empty).Trim();

            var query = db.SanPhams
                .Where(x => x.CreatedById == currentUserId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaSanPham.Contains(keyword) ||
                    x.TenSanPham.Contains(keyword));
            }

            var products = query
                .OrderByDescending(x => x.UpdatedAt.HasValue ? x.UpdatedAt.Value : x.CreatedAt)
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
                    ThuongHieuTen = x.ThuongHieu.TenThuongHieu,
                    LoaiSanPhamId = x.LoaiSanPhamId,
                    LoaiSanPhamTen = x.LoaiSanPham.TenLoaiSanPham,
                    TrangThai = x.TrangThai,
                    IsFeatured = x.IsFeatured,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList();

            return new AdminProductListPageVm
            {
                Keyword = keyword,
                Products = products,
                Brands = GetBrandOptions(),
                Categories = GetCategoryOptions()
            };
        }

        private AdminProductCreateVm BuildCreateViewModel()
        {
            return new AdminProductCreateVm
            {
                TrangThai = 1,
                IsFeatured = false,
                Brands = GetBrandOptions(),
                Categories = GetCategoryOptions()
            };
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

        private bool CanManageProduct(TaiKhoan currentUser, SanPham product)
        {
            if (currentUser == null || product == null)
            {
                return false;
            }

            if (currentUser.VaiTro != null &&
                string.Equals(currentUser.VaiTro.MaVaiTro, RoleConstants.ADMIN, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return product.CreatedById == currentUser.TaiKhoanId;
        }

        private void ValidateProductCreateModel(AdminProductCreateVm model, int? currentProductId)
        {
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

            bool duplicateDescription = db.SanPhams.Any(x =>
                (!currentProductId.HasValue || x.SanPhamId != currentProductId.Value) &&
                x.MoTaChiTiet == model.MoTaChiTiet &&
                !string.IsNullOrEmpty(model.MoTaChiTiet));

            if (duplicateDescription)
            {
                ModelState.AddModelError("MoTaChiTiet", "Mô tả chi tiết sản phẩm đã tồn tại.");
            }

            bool brandExists = db.ThuongHieux.Any(x => x.ThuongHieuId == model.ThuongHieuId && x.IsActive);
            if (!brandExists)
            {
                ModelState.AddModelError("ThuongHieuId", "Thương hiệu không hợp lệ.");
            }

            bool categoryExists = db.LoaiSanPhams.Any(x => x.LoaiSanPhamId == model.LoaiSanPhamId && x.IsActive);
            if (!categoryExists)
            {
                ModelState.AddModelError("LoaiSanPhamId", "Loại sản phẩm không hợp lệ.");
            }
        }

        private string GenerateProductCode()
        {
            return DateTime.Now.ToString("MMddHHmmss");
        }

        private string SaveProductImage(HttpPostedFileBase image)
        {
            if (image == null || image.ContentLength <= 0)
            {
                return string.Empty;
            }

            var ext = Path.GetExtension(image.FileName)?.ToLowerInvariant();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp"
            };

            if (!allowed.Contains(ext))
            {
                throw new InvalidOperationException("Định dạng ảnh không hợp lệ.");
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
            if (string.IsNullOrEmpty(path)) return path;
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