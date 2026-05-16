using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN + "," + RoleConstants.STAFF)]
    public class ManagerReviewController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();
        private const int PAGE_SIZE = 15;

        [HttpGet]
        public ActionResult Index(int statusFilter = ReviewStatusConstants.PENDING, string keyword = "", int page = 1)
        {
            keyword = (keyword ?? string.Empty).Trim();
            if (page <= 0) page = 1;

            var query = db.DanhGiaSanPhams
                .Include(x => x.KhachHang)
                .Include(x => x.SanPham)
                .Include(x => x.TaiKhoan)
                .Where(x => x.TrangThai == statusFilter);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.SanPham.TenSanPham.Contains(keyword) ||
                    x.KhachHang.HoTen.Contains(keyword));
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);
            if (totalPages <= 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var reviews = query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .Select(x => new AdminReviewItemVm
                {
                    DanhGiaId = x.DanhGiaId,
                    ChiTietDonHangId = x.ChiTietDonHangId,
                    SanPhamId = x.SanPhamId,
                    TenSanPham = x.SanPham.TenSanPham,
                    HinhAnhChinh = x.SanPham.HinhAnhChinh,
                    TenKhachHang = x.KhachHang.HoTen,
                    SoSao = x.SoSao,
                    NoiDung = x.NoiDung,
                    TrangThai = x.TrangThai,
                    TrangThaiText = "",
                    LyDoTuChoi = x.LyDoTuChoi,
                    CreatedAt = x.CreatedAt,
                    NgayDuyet = x.NgayDuyet,
                    DuyetBoiName = x.TaiKhoan != null ? x.TaiKhoan.HoTen : ""
                })
                .ToList();

            foreach (var r in reviews)
            {
                r.TrangThaiText = ReviewStatusConstants.GetName(r.TrangThai);
            }

            var model = new AdminReviewIndexVm
            {
                Reviews = reviews,
                StatusFilter = statusFilter,
                Keyword = keyword,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = PAGE_SIZE
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(AdminReviewActionVm model)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
                return RedirectToAction("LoginAccount", "Account", new { area = "" });

            var review = db.DanhGiaSanPhams.FirstOrDefault(x => x.DanhGiaId == model.DanhGiaId);
            if (review == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                return RedirectToAction("Index");
            }

            if (review.TrangThai != ReviewStatusConstants.PENDING)
            {
                TempData["ErrorMessage"] = "Đánh giá này đã được xử lý rồi.";
                return RedirectToAction("Index");
            }

            review.TrangThai = ReviewStatusConstants.APPROVED;
            review.DuyetBoiId = currentUser.TaiKhoanId;
            review.NgayDuyet = DateTime.Now;
            review.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã duyệt đánh giá thành công.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(AdminReviewActionVm model)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
                return RedirectToAction("LoginAccount", "Account", new { area = "" });

            var review = db.DanhGiaSanPhams.FirstOrDefault(x => x.DanhGiaId == model.DanhGiaId);
            if (review == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                return RedirectToAction("Index");
            }

            if (review.TrangThai != ReviewStatusConstants.PENDING)
            {
                TempData["ErrorMessage"] = "Đánh giá này đã được xử lý rồi.";
                return RedirectToAction("Index");
            }

            review.TrangThai = ReviewStatusConstants.REJECTED;
            review.DuyetBoiId = currentUser.TaiKhoanId;
            review.NgayDuyet = DateTime.Now;
            review.LyDoTuChoi = string.IsNullOrWhiteSpace(model.LyDoTuChoi) ? "Không phù hợp." : model.LyDoTuChoi.Trim();
            review.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã từ chối đánh giá.";
            return RedirectToAction("Index");
        }

        private TaiKhoan GetCurrentAccount()
        {
            var sessionAccount = Session["LoginInformation"] as TaiKhoan;
            if (sessionAccount == null) return null;

            return db.TaiKhoans
                .Include(x => x.VaiTro)
                .FirstOrDefault(x => x.TaiKhoanId == sessionAccount.TaiKhoanId && x.IsActive);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
