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
            if (model == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu thương hiệu không hợp lệ.";
                return RedirectToAction("Brand");
            }

            NormalizeBrandModel(model);

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.ThuongHieuId);
                invalidVm.Form = model;
                return View("Brand", invalidVm);
            }

            bool isUpdate = model.ThuongHieuId.HasValue && model.ThuongHieuId.Value > 0;

            if (isUpdate)
            {
                return UpdateBrand(model);
            }

            return CreateBrand(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var brand = db.ThuongHieux.FirstOrDefault(x => x.ThuongHieuId == id);

            if (brand == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thương hiệu.";
                return RedirectToAction("Brand");
            }

            brand.IsActive = !brand.IsActive;
            brand.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = brand.IsActive
                ? "Đã kích hoạt lại thương hiệu."
                : "Đã chuyển thương hiệu sang trạng thái ngừng sử dụng.";

            return RedirectToAction("Brand");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(int id)
        {
            return RedirectToAction("Brand", new { editId = id });
        }

        private ActionResult CreateBrand(AdminBrandEditVm model)
        {
            if (IsDuplicatedBrandCode(model.MaThuongHieu, null))
            {
                ModelState.AddModelError("MaThuongHieu", "Mã thương hiệu đã tồn tại.");
            }

            if (IsDuplicatedBrandName(model.TenThuongHieu, null))
            {
                ModelState.AddModelError("TenThuongHieu", "Tên thương hiệu đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(null);
                invalidVm.Form = model;
                return View("Brand", invalidVm);
            }

            var brand = new ThuongHieu
            {
                MaThuongHieu = model.MaThuongHieu,
                TenThuongHieu = model.TenThuongHieu,
                MoTa = model.MoTa,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            db.ThuongHieux.Add(brand);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Thêm thương hiệu thành công.";
            return RedirectToAction("Brand");
        }

        private ActionResult UpdateBrand(AdminBrandEditVm model)
        {
            var brand = db.ThuongHieux.FirstOrDefault(x => x.ThuongHieuId == model.ThuongHieuId.Value);

            if (brand == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thương hiệu.";
                return RedirectToAction("Brand");
            }

            if (IsDuplicatedBrandCode(model.MaThuongHieu, brand.ThuongHieuId))
            {
                ModelState.AddModelError("MaThuongHieu", "Mã thương hiệu đã tồn tại.");
            }

            if (IsDuplicatedBrandName(model.TenThuongHieu, brand.ThuongHieuId))
            {
                ModelState.AddModelError("TenThuongHieu", "Tên thương hiệu đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.ThuongHieuId);
                invalidVm.Form = model;
                return View("Brand", invalidVm);
            }

            brand.MaThuongHieu = model.MaThuongHieu;
            brand.TenThuongHieu = model.TenThuongHieu;
            brand.MoTa = model.MoTa;
            brand.IsActive = model.IsActive;
            brand.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật thương hiệu thành công.";
            return RedirectToAction("Brand");
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

        private void NormalizeBrandModel(AdminBrandEditVm model)
        {
            model.MaThuongHieu = NormalizeText(model.MaThuongHieu).ToUpperInvariant();
            model.TenThuongHieu = NormalizeText(model.TenThuongHieu);
            model.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
        }

        private bool IsDuplicatedBrandCode(string code, int? currentId)
        {
            code = NormalizeText(code).ToUpperInvariant();

            return db.ThuongHieux.Any(x =>
                x.MaThuongHieu == code &&
                (!currentId.HasValue || x.ThuongHieuId != currentId.Value));
        }

        private bool IsDuplicatedBrandName(string name, int? currentId)
        {
            name = NormalizeText(name);

            return db.ThuongHieux.Any(x =>
                x.TenThuongHieu == name &&
                (!currentId.HasValue || x.ThuongHieuId != currentId.Value));
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