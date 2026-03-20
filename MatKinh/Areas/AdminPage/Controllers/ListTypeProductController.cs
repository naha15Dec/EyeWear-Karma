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
            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.LoaiSanPhamId);
                invalidVm.Form = model;
                return View("ProductType", invalidVm);
            }

            bool isUpdate = model.LoaiSanPhamId.HasValue && model.LoaiSanPhamId.Value > 0;

            if (isUpdate)
            {
                var category = db.LoaiSanPhams.FirstOrDefault(x => x.LoaiSanPhamId == model.LoaiSanPhamId.Value);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy loại sản phẩm.";
                    return RedirectToAction("ProductType");
                }

                bool duplicatedCode = db.LoaiSanPhams.Any(x =>
                    x.LoaiSanPhamId != category.LoaiSanPhamId &&
                    x.MaLoaiSanPham == model.MaLoaiSanPham);

                if (duplicatedCode)
                {
                    ModelState.AddModelError("MaLoaiSanPham", "Mã loại sản phẩm đã tồn tại.");
                    var invalidVm = BuildViewModel(model.LoaiSanPhamId);
                    invalidVm.Form = model;
                    return View("ProductType", invalidVm);
                }

                category.MaLoaiSanPham = (model.MaLoaiSanPham ?? string.Empty).Trim();
                category.TenLoaiSanPham = (model.TenLoaiSanPham ?? string.Empty).Trim();
                category.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
                category.IsActive = model.IsActive;
                category.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật loại sản phẩm thành công.";
                return RedirectToAction("ProductType");
            }
            else
            {
                bool duplicatedCode = db.LoaiSanPhams.Any(x => x.MaLoaiSanPham == model.MaLoaiSanPham);
                if (duplicatedCode)
                {
                    ModelState.AddModelError("MaLoaiSanPham", "Mã loại sản phẩm đã tồn tại.");
                    var invalidVm = BuildViewModel(null);
                    invalidVm.Form = model;
                    return View("ProductType", invalidVm);
                }

                var category = new LoaiSanPham
                {
                    MaLoaiSanPham = (model.MaLoaiSanPham ?? string.Empty).Trim(),
                    TenLoaiSanPham = (model.TenLoaiSanPham ?? string.Empty).Trim(),
                    MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim(),
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };

                db.LoaiSanPhams.Add(category);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thêm loại sản phẩm thành công.";
                return RedirectToAction("ProductType");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var category = db.LoaiSanPhams
                .Include(x => x.SanPhams)
                .FirstOrDefault(x => x.LoaiSanPhamId == id);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy loại sản phẩm.";
                return RedirectToAction("ProductType");
            }

            if (category.SanPhams != null && category.SanPhams.Any())
            {
                category.IsActive = false;
                category.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Loại sản phẩm đã được ngừng sử dụng vì đang gắn với sản phẩm.";
                return RedirectToAction("ProductType");
            }

            db.LoaiSanPhams.Remove(category);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Xóa loại sản phẩm thành công.";
            return RedirectToAction("ProductType");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id)
        {
            return RedirectToAction("ProductType", new { editId = id });
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