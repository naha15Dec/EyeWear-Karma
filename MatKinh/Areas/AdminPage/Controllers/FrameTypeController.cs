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
    public class FrameTypeController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult Index(int? editId = null, int page = 1)
        {
            var model = BuildViewModel(editId, page);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(AdminFrameTypeEditVm model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu kiểu gọng không hợp lệ.";
                return RedirectToAction("Index");
            }

            NormalizeFrameTypeModel(model);

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.KieuGongId,1);
                invalidVm.Form = model;
                return View("Index", invalidVm);
            }

            bool isUpdate = model.KieuGongId.HasValue && model.KieuGongId.Value > 0;

            if (isUpdate)
            {
                return UpdateFrameType(model);
            }

            return CreateFrameType(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id)
        {
            return RedirectToAction("Index", new { editId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            var frameType = db.KieuGongs.FirstOrDefault(x => x.KieuGongId == id);

            if (frameType == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy kiểu gọng.";
                return RedirectToAction("Index");
            }

            frameType.IsActive = !frameType.IsActive;
            frameType.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = frameType.IsActive
                ? "Đã kích hoạt lại kiểu gọng."
                : "Đã chuyển kiểu gọng sang trạng thái ngừng sử dụng.";

            return RedirectToAction("Index");
        }

        private ActionResult CreateFrameType(AdminFrameTypeEditVm model)
        {
            if (IsDuplicatedFrameTypeCode(model.MaKieuGong, null))
            {
                ModelState.AddModelError("MaKieuGong", "Mã kiểu gọng đã tồn tại.");
            }

            if (IsDuplicatedFrameTypeName(model.TenKieuGong, null))
            {
                ModelState.AddModelError("TenKieuGong", "Tên kiểu gọng đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(null,1);
                invalidVm.Form = model;
                return View("Index", invalidVm);
            }

            var frameType = new KieuGong
            {
                MaKieuGong = model.MaKieuGong,
                TenKieuGong = model.TenKieuGong,
                MoTa = model.MoTa,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            db.KieuGongs.Add(frameType);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Thêm kiểu gọng thành công.";
            return RedirectToAction("Index");
        }

        private ActionResult UpdateFrameType(AdminFrameTypeEditVm model)
        {
            var frameType = db.KieuGongs.FirstOrDefault(x => x.KieuGongId == model.KieuGongId.Value);

            if (frameType == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy kiểu gọng.";
                return RedirectToAction("Index");
            }

            if (IsDuplicatedFrameTypeCode(model.MaKieuGong, frameType.KieuGongId))
            {
                ModelState.AddModelError("MaKieuGong", "Mã kiểu gọng đã tồn tại.");
            }

            if (IsDuplicatedFrameTypeName(model.TenKieuGong, frameType.KieuGongId))
            {
                ModelState.AddModelError("TenKieuGong", "Tên kiểu gọng đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.KieuGongId,1);
                invalidVm.Form = model;
                return View("Index", invalidVm);
            }

            frameType.MaKieuGong = model.MaKieuGong;
            frameType.TenKieuGong = model.TenKieuGong;
            frameType.MoTa = model.MoTa;
            frameType.IsActive = model.IsActive;
            frameType.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật kiểu gọng thành công.";
            return RedirectToAction("Index");
        }

        private AdminFrameTypeIndexVm BuildViewModel(int? editId, int page)
        {
            const int pageSize = 3;

            if (page < 1)
            {
                page = 1;
            }

            var query = db.KieuGongs
                .Include(x => x.SanPhams)
                .OrderByDescending(x => x.CreatedAt);

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (totalPages < 1)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var frameTypes = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var ruleCounts = db.RuleGoiYKinhTheoMats
                .GroupBy(x => x.KieuGongId)
                .Select(x => new
                {
                    KieuGongId = x.Key,
                    Count = x.Count()
                })
                .ToList();

            var vm = new AdminFrameTypeIndexVm
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,

                FrameTypes = frameTypes.Select(x => new AdminFrameTypeListItemVm
                {
                    KieuGongId = x.KieuGongId,
                    MaKieuGong = x.MaKieuGong,
                    TenKieuGong = x.TenKieuGong,
                    MoTa = x.MoTa,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    SoSanPham = x.SanPhams != null ? x.SanPhams.Count : 0,
                    SoRule = ruleCounts
                        .Where(r => r.KieuGongId == x.KieuGongId)
                        .Select(r => r.Count)
                        .FirstOrDefault()
                }).ToList()
            };

            if (editId.HasValue)
            {
                var frameType = db.KieuGongs.FirstOrDefault(x => x.KieuGongId == editId.Value);

                if (frameType != null)
                {
                    vm.Form = new AdminFrameTypeEditVm
                    {
                        KieuGongId = frameType.KieuGongId,
                        MaKieuGong = frameType.MaKieuGong,
                        TenKieuGong = frameType.TenKieuGong,
                        MoTa = frameType.MoTa,
                        IsActive = frameType.IsActive
                    };
                }
            }

            return vm;
        }

        private void NormalizeFrameTypeModel(AdminFrameTypeEditVm model)
        {
            model.MaKieuGong = NormalizeText(model.MaKieuGong).ToUpperInvariant();
            model.TenKieuGong = NormalizeText(model.TenKieuGong);
            model.MoTa = string.IsNullOrWhiteSpace(model.MoTa) ? null : model.MoTa.Trim();
        }

        private bool IsDuplicatedFrameTypeCode(string code, int? currentId)
        {
            code = NormalizeText(code).ToUpperInvariant();

            return db.KieuGongs.Any(x =>
                x.MaKieuGong == code &&
                (!currentId.HasValue || x.KieuGongId != currentId.Value));
        }

        private bool IsDuplicatedFrameTypeName(string name, int? currentId)
        {
            name = NormalizeText(name);

            return db.KieuGongs.Any(x =>
                x.TenKieuGong == name &&
                (!currentId.HasValue || x.KieuGongId != currentId.Value));
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