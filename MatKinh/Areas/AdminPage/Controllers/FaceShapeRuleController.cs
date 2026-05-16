using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN)]
    public class FaceShapeRuleController : Controller
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
        public ActionResult Save(AdminFaceShapeRuleEditVm model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Dữ liệu rule gợi ý không hợp lệ.";
                return RedirectToAction("Index");
            }

            NormalizeRuleModel(model);

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.RuleId, 1);
                invalidVm.Form = model;
                return View("Index", invalidVm);
            }

            bool isUpdate = model.RuleId.HasValue && model.RuleId.Value > 0;

            if (isUpdate)
            {
                return UpdateRule(model);
            }

            return CreateRule(model);
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
            var rule = db.RuleGoiYKinhTheoMats.FirstOrDefault(x => x.RuleId == id);

            if (rule == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy rule gợi ý.";
                return RedirectToAction("Index");
            }

            rule.IsActive = !rule.IsActive;
            rule.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = rule.IsActive
                ? "Đã kích hoạt lại rule gợi ý."
                : "Đã chuyển rule gợi ý sang trạng thái ngừng sử dụng.";

            return RedirectToAction("Index");
        }

        private ActionResult CreateRule(AdminFaceShapeRuleEditVm model)
        {
            if (!IsValidFaceShapeCode(model.MaHinhDangMat))
            {
                ModelState.AddModelError("MaHinhDangMat", "Dáng mặt không hợp lệ.");
            }

            if (!IsValidFrameType(model.KieuGongId.Value))
            {
                ModelState.AddModelError("KieuGongId", "Kiểu gọng không hợp lệ hoặc đang ngừng sử dụng.");
            }

            if (IsDuplicatedRule(model.MaHinhDangMat, model.KieuGongId.Value, null))
            {
                ModelState.AddModelError("KieuGongId", "Rule cho dáng mặt và kiểu gọng này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(null, 1);
                invalidVm.Form = model;
                return View("Index", invalidVm);
            }

            var rule = new RuleGoiYKinhTheoMat
            {
                MaHinhDangMat = model.MaHinhDangMat,
                KieuGongId = model.KieuGongId.Value,
                DiemPhuHop = model.DiemPhuHop,
                GiaiThich = model.GiaiThich,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            db.RuleGoiYKinhTheoMats.Add(rule);
            db.SaveChanges();

            TempData["SuccessMessage"] = "Thêm rule gợi ý thành công.";
            return RedirectToAction("Index");
        }

        private ActionResult UpdateRule(AdminFaceShapeRuleEditVm model)
        {
            var rule = db.RuleGoiYKinhTheoMats.FirstOrDefault(x => x.RuleId == model.RuleId.Value);

            if (rule == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy rule gợi ý.";
                return RedirectToAction("Index");
            }

            if (!IsValidFaceShapeCode(model.MaHinhDangMat))
            {
                ModelState.AddModelError("MaHinhDangMat", "Dáng mặt không hợp lệ.");
            }

            if (!IsValidFrameType(model.KieuGongId.Value))
            {
                ModelState.AddModelError("KieuGongId", "Kiểu gọng không hợp lệ hoặc đang ngừng sử dụng.");
            }

            if (IsDuplicatedRule(model.MaHinhDangMat, model.KieuGongId.Value, rule.RuleId))
            {
                ModelState.AddModelError("KieuGongId", "Rule cho dáng mặt và kiểu gọng này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildViewModel(model.RuleId, 1);
                invalidVm.Form = model;
                return View("Index", invalidVm);
            }

            rule.MaHinhDangMat = model.MaHinhDangMat;
            rule.KieuGongId = model.KieuGongId.Value;
            rule.DiemPhuHop = model.DiemPhuHop;
            rule.GiaiThich = model.GiaiThich;
            rule.IsActive = model.IsActive;
            rule.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật rule gợi ý thành công.";
            return RedirectToAction("Index");
        }

        private AdminFaceShapeRuleIndexVm BuildViewModel(int? editId, int page)
        {
            const int pageSize = 4;

            if (page < 1)
            {
                page = 1;
            }

            var frameTypes = db.KieuGongs
                .Where(x => x.IsActive)
                .OrderBy(x => x.TenKieuGong)
                .ToList();

            var query = db.RuleGoiYKinhTheoMats
                .Include(x => x.KieuGong)
                .OrderBy(x => x.MaHinhDangMat)
                .ThenByDescending(x => x.DiemPhuHop);

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

            var rules = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new AdminFaceShapeRuleIndexVm
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,

                FaceShapeOptions = GetFaceShapeOptions(null),

                FrameTypeOptions = frameTypes.Select(x => new SelectListItem
                {
                    Value = x.KieuGongId.ToString(),
                    Text = x.TenKieuGong + " (#" + x.MaKieuGong + ")"
                }).ToList(),

                Rules = rules.Select(x => new AdminFaceShapeRuleListItemVm
                {
                    RuleId = x.RuleId,
                    MaHinhDangMat = x.MaHinhDangMat,
                    TenHinhDangMat = GetFaceShapeName(x.MaHinhDangMat),
                    KieuGongId = x.KieuGongId,
                    MaKieuGong = x.KieuGong != null ? x.KieuGong.MaKieuGong : "",
                    TenKieuGong = x.KieuGong != null ? x.KieuGong.TenKieuGong : "Không xác định",
                    DiemPhuHop = x.DiemPhuHop,
                    GiaiThich = x.GiaiThich,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                }).ToList()
            };

            if (editId.HasValue)
            {
                var rule = db.RuleGoiYKinhTheoMats.FirstOrDefault(x => x.RuleId == editId.Value);

                if (rule != null)
                {
                    vm.Form = new AdminFaceShapeRuleEditVm
                    {
                        RuleId = rule.RuleId,
                        MaHinhDangMat = rule.MaHinhDangMat,
                        KieuGongId = rule.KieuGongId,
                        DiemPhuHop = rule.DiemPhuHop,
                        GiaiThich = rule.GiaiThich,
                        IsActive = rule.IsActive
                    };

                    vm.FaceShapeOptions = GetFaceShapeOptions(rule.MaHinhDangMat);

                    foreach (var item in vm.FrameTypeOptions)
                    {
                        item.Selected = item.Value == rule.KieuGongId.ToString();
                    }
                }
            }

            return vm;
        }

        private void NormalizeRuleModel(AdminFaceShapeRuleEditVm model)
        {
            model.MaHinhDangMat = NormalizeText(model.MaHinhDangMat).ToUpperInvariant();
            model.GiaiThich = NormalizeText(model.GiaiThich);
        }

        private bool IsDuplicatedRule(string faceShapeCode, int frameTypeId, int? currentRuleId)
        {
            faceShapeCode = NormalizeText(faceShapeCode).ToUpperInvariant();

            return db.RuleGoiYKinhTheoMats.Any(x =>
                x.MaHinhDangMat == faceShapeCode &&
                x.KieuGongId == frameTypeId &&
                (!currentRuleId.HasValue || x.RuleId != currentRuleId.Value));
        }

        private bool IsValidFrameType(int frameTypeId)
        {
            return db.KieuGongs.Any(x => x.KieuGongId == frameTypeId && x.IsActive);
        }

        private bool IsValidFaceShapeCode(string code)
        {
            code = NormalizeText(code).ToUpperInvariant();

            return GetFaceShapeDictionary().ContainsKey(code);
        }

        private List<SelectListItem> GetFaceShapeOptions(string selectedCode)
        {
            selectedCode = NormalizeText(selectedCode).ToUpperInvariant();

            return GetFaceShapeDictionary()
                .Select(x => new SelectListItem
                {
                    Value = x.Key,
                    Text = x.Value + " (#" + x.Key + ")",
                    Selected = x.Key == selectedCode
                })
                .ToList();
        }

        private string GetFaceShapeName(string code)
        {
            code = NormalizeText(code).ToUpperInvariant();

            var map = GetFaceShapeDictionary();

            return map.ContainsKey(code) ? map[code] : code;
        }

        private Dictionary<string, string> GetFaceShapeDictionary()
        {
            return new Dictionary<string, string>
            {
                { "ROUND", "Mặt tròn" },
                { "OVAL", "Mặt oval" },
                { "SQUARE", "Mặt vuông" },
                { "HEART", "Mặt trái tim" },
                { "LONG", "Mặt dài" }
            };
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