using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN)]
    public class ListTypeProductController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult ProductType(int? editId = null)
        {
            var model = BuildViewModel(editId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTypeProduct(AdminCategoryEditVm model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu loại sản phẩm không hợp lệ.";
                return RedirectToAction("ProductType");
            }

            NormalizeCategoryModel(model);

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.LoaiSanPhamId);
                invalidVm.Form = model;
                return View("ProductType", invalidVm);
            }

            bool isUpdate = model.LoaiSanPhamId.HasValue && model.LoaiSanPhamId.Value > 0;

            if (isUpdate)
            {
                return UpdateCategory(model);
            }

            return CreateCategory(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var category = db.LoaiSanPhams.FirstOrDefault(x => x.LoaiSanPhamId == id);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy loại sản phẩm.";
                return RedirectToAction("ProductType");
            }

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = category.IsActive
                ? "Đã kích hoạt lại loại sản phẩm."
                : "Đã chuyển loại sản phẩm sang trạng thái ngừng sử dụng.";

            return RedirectToAction("ProductType");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id)
        {
            return RedirectToAction("ProductType", new { editId = id });
        }

        private ActionResult CreateCategory(AdminCategoryEditVm model)
        {
            if (IsDuplicatedCategoryCode(model.MaLoaiSanPham, null))
            {
                ModelState.AddModelError("MaLoaiSanPham", "Mã loại sản phẩm đã tồn tại.");
            }

            if (IsDuplicatedCategoryName(model.TenLoaiSanPham, null))
            {
                ModelState.AddModelError("TenLoaiSanPham", "Tên loại sản phẩm đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(null);
                invalidVm.Form = model;
                return View("ProductType", invalidVm);
            }

            var category = new LoaiSanPham
            {
                MaLoaiSanPham = model.MaLoaiSanPham,
                TenLoaiSanPham = model.TenLoaiSanPham,
                MoTa = model.MoTa,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            db.LoaiSanPhams.Add(category);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Thêm loại sản phẩm thành công.";
            return RedirectToAction("ProductType");
        }

        private ActionResult UpdateCategory(AdminCategoryEditVm model)
        {
            var category = db.LoaiSanPhams.FirstOrDefault(x => x.LoaiSanPhamId == model.LoaiSanPhamId.Value);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy loại sản phẩm.";
                return RedirectToAction("ProductType");
            }

            if (IsDuplicatedCategoryCode(model.MaLoaiSanPham, category.LoaiSanPhamId))
            {
                ModelState.AddModelError("MaLoaiSanPham", "Mã loại sản phẩm đã tồn tại.");
            }

            if (IsDuplicatedCategoryName(model.TenLoaiSanPham, category.LoaiSanPhamId))
            {
                ModelState.AddModelError("TenLoaiSanPham", "Tên loại sản phẩm đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.LoaiSanPhamId);
                invalidVm.Form = model;
                return View("ProductType", invalidVm);
            }

            category.MaLoaiSanPham = model.MaLoaiSanPham;
            category.TenLoaiSanPham = model.TenLoaiSanPham;
            category.MoTa = model.MoTa;
            category.IsActive = model.IsActive;
            category.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật loại sản phẩm thành công.";
            return RedirectToAction("ProductType");
        }

        private AdminCategoryIndexVm BuildViewModel(int? editId)
        {
            var categories = db.LoaiSanPhams
                .Include(x => x.SanPhams)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            var vm = new AdminCategoryIndexVm
            {
                Categories = categories.Select(x => new AdminCategoryListItemVm
                {
                    LoaiSanPhamId = x.LoaiSanPhamId,
                    MaLoaiSanPham = x.MaLoaiSanPham,
                    TenLoaiSanPham = x.TenLoaiSanPham,
                    MoTa = x.MoTa,
                    IsActive = x.IsActive,
                    SoSanPham = x.SanPhams != null ? x.SanPhams.Count : 0,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                }).ToList()
            };

            if (editId.HasValue)
            {
                var category = categories.FirstOrDefault(x => x.LoaiSanPhamId == editId.Value);
                if (category != null)
                {
                    vm.Form = new AdminCategoryEditVm
                    {
                        LoaiSanPhamId = category.LoaiSanPhamId,
                        MaLoaiSanPham = category.MaLoaiSanPham,
                        TenLoaiSanPham = category.TenLoaiSanPham,
                        MoTa = category.MoTa,
                        IsActive = category.IsActive
                    };
                }
            }

            return vm;
        }

        private void NormalizeCategoryModel(AdminCategoryEditVm model)
        {
            model.MaLoaiSanPham = NormalizeText(model.MaLoaiSanPham).ToUpperInvariant();
            model.TenLoaiSanPham = NormalizeText(model.TenLoaiSanPham);
            model.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
        }

        private bool IsDuplicatedCategoryCode(string code, int? currentId)
        {
            code = NormalizeText(code).ToUpperInvariant();

            return db.LoaiSanPhams.Any(x =>
                x.MaLoaiSanPham == code &&
                (!currentId.HasValue || x.LoaiSanPhamId != currentId.Value));
        }

        private bool IsDuplicatedCategoryName(string name, int? currentId)
        {
            name = NormalizeText(name);

            return db.LoaiSanPhams.Any(x =>
                x.TenLoaiSanPham == name &&
                (!currentId.HasValue || x.LoaiSanPhamId != currentId.Value));
        }

        private string NormalizeText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
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