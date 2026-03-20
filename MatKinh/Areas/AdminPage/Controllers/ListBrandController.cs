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
    public class ListBrandController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult Brand(int? editId = null)
        {
            var model = BuildViewModel(editId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBrand(AdminBrandEditVm model)
        {
            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.ThuongHieuId);
                invalidVm.Form = model;
                return View("Brand", invalidVm);
            }

            bool isUpdate = model.ThuongHieuId.HasValue && model.ThuongHieuId.Value > 0;

            if (isUpdate)
            {
                var brand = db.ThuongHieux.FirstOrDefault(x => x.ThuongHieuId == model.ThuongHieuId.Value);
                if (brand == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thương hiệu.";
                    return RedirectToAction("Brand");
                }

                bool duplicatedCode = db.ThuongHieux.Any(x =>
                    x.ThuongHieuId != brand.ThuongHieuId &&
                    x.MaThuongHieu == model.MaThuongHieu);

                if (duplicatedCode)
                {
                    ModelState.AddModelError("MaThuongHieu", "Mã thương hiệu đã tồn tại.");
                    var invalidVm = BuildViewModel(model.ThuongHieuId);
                    invalidVm.Form = model;
                    return View("Brand", invalidVm);
                }

                brand.MaThuongHieu = (model.MaThuongHieu ?? string.Empty).Trim();
                brand.TenThuongHieu = (model.TenThuongHieu ?? string.Empty).Trim();
                brand.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
                brand.IsActive = model.IsActive;
                brand.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật thương hiệu thành công.";
                return RedirectToAction("Brand");
            }
            else
            {
                bool duplicatedCode = db.ThuongHieux.Any(x => x.MaThuongHieu == model.MaThuongHieu);
                if (duplicatedCode)
                {
                    ModelState.AddModelError("MaThuongHieu", "Mã thương hiệu đã tồn tại.");
                    var invalidVm = BuildViewModel(null);
                    invalidVm.Form = model;
                    return View("Brand", invalidVm);
                }

                var brand = new ThuongHieu
                {
                    MaThuongHieu = (model.MaThuongHieu ?? string.Empty).Trim(),
                    TenThuongHieu = (model.TenThuongHieu ?? string.Empty).Trim(),
                    MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim(),
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };

                db.ThuongHieux.Add(brand);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thêm thương hiệu thành công.";
                return RedirectToAction("Brand");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var brand = db.ThuongHieux
                .Include(x => x.SanPhams)
                .FirstOrDefault(x => x.ThuongHieuId == id);

            if (brand == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thương hiệu.";
                return RedirectToAction("Brand");
            }

            // Không xóa cứng nếu đã gắn sản phẩm
            if (brand.SanPhams != null && brand.SanPhams.Any())
            {
                brand.IsActive = false;
                brand.UpdatedAt = DateTime.Now;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Thương hiệu đã được ngừng sử dụng vì đang gắn với sản phẩm.";
                return RedirectToAction("Brand");
            }

            db.ThuongHieux.Remove(brand);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Xóa thương hiệu thành công.";
            return RedirectToAction("Brand");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id)
        {
            return RedirectToAction("Brand", new { editId = id });
        }

        private AdminBrandIndexVm BuildViewModel(int? editId)
        {
            var brands = db.ThuongHieux
                .Include(x => x.SanPhams)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            var vm = new AdminBrandIndexVm
            {
                Brands = brands.Select(x => new AdminBrandListItemVm
                {
                    ThuongHieuId = x.ThuongHieuId,
                    MaThuongHieu = x.MaThuongHieu,
                    TenThuongHieu = x.TenThuongHieu,
                    MoTa = x.MoTa,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    SoSanPham = x.SanPhams != null ? x.SanPhams.Count : 0
                }).ToList()
            };

            if (editId.HasValue)
            {
                var brand = brands.FirstOrDefault(x => x.ThuongHieuId == editId.Value);
                if (brand != null)
                {
                    vm.Form = new AdminBrandEditVm
                    {
                        ThuongHieuId = brand.ThuongHieuId,
                        MaThuongHieu = brand.MaThuongHieu,
                        TenThuongHieu = brand.TenThuongHieu,
                        MoTa = brand.MoTa,
                        IsActive = brand.IsActive
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