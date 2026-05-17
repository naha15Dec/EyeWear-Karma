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
    public class ReviewManagerController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int PAGE_SIZE = 10;

        [HttpGet]
        public ActionResult Index(string keyword = "", int? rating = null, int? status = null, int page = 1)
        {
            var model = BuildIndexViewModel(keyword, rating, status, page);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Hide(int id, string keyword = "", int? rating = null, int? status = null, int page = 1)
        {
            var review = db.DanhGiaSanPhams.FirstOrDefault(x => x.DanhGiaId == id);

            if (review == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                return RedirectToAction("Index", new { keyword, rating, status, page });
            }

            var admin = GetCurrentAccount();

            review.TrangThai = ReviewStatusConstants.HIDDEN;
            review.UpdatedAt = DateTime.Now;
            review.NgayDuyet = DateTime.Now;
            review.DuyetBoiId = admin != null ? (int?)admin.TaiKhoanId : null;
            review.PhanHoiAdmin = "Đánh giá đã bị ẩn bởi quản trị viên.";

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã ẩn đánh giá.";
            return RedirectToAction("Index", new { keyword, rating, status, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Restore(int id, string keyword = "", int? rating = null, int? status = null, int page = 1)
        {
            var review = db.DanhGiaSanPhams.FirstOrDefault(x => x.DanhGiaId == id);

            if (review == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đánh giá.";
                return RedirectToAction("Index", new { keyword, rating, status, page });
            }

            var admin = GetCurrentAccount();

            review.TrangThai = ReviewStatusConstants.APPROVED;
            review.UpdatedAt = DateTime.Now;
            review.NgayDuyet = DateTime.Now;
            review.DuyetBoiId = admin != null ? (int?)admin.TaiKhoanId : null;
            review.PhanHoiAdmin = null;

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã khôi phục đánh giá.";
            return RedirectToAction("Index", new { keyword, rating, status, page });
        }

        private AdminReviewIndexVm BuildIndexViewModel(string keyword, int? rating, int? status, int page)
        {
            keyword = (keyword ?? string.Empty).Trim();

            if (page <= 0)
            {
                page = 1;
            }

            var baseQuery = db.DanhGiaSanPhams
                .Include(x => x.SanPham)
                .Include(x => x.KhachHang)
                .Include(x => x.TaiKhoan)
                .AsQueryable();

            int totalReviews = baseQuery.Count();
            int visibleReviews = baseQuery.Count(x => x.TrangThai == ReviewStatusConstants.APPROVED);
            int hiddenReviews = baseQuery.Count(x => x.TrangThai == ReviewStatusConstants.HIDDEN);

            decimal averageRating = totalReviews > 0
                ? Math.Round((decimal)baseQuery.Average(x => x.SoSao), 1)
                : 0m;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                baseQuery = baseQuery.Where(x =>
                    x.NoiDung.Contains(keyword) ||
                    x.SanPham.TenSanPham.Contains(keyword) ||
                    x.SanPham.MaSanPham.Contains(keyword) ||
                    x.KhachHang.HoTen.Contains(keyword) ||
                    x.KhachHang.Email.Contains(keyword));
            }

            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
            {
                int star = rating.Value;
                baseQuery = baseQuery.Where(x => x.SoSao == star);
            }
            else
            {
                rating = null;
            }

            if (status.HasValue &&
                (status.Value == ReviewStatusConstants.APPROVED ||
                 status.Value == ReviewStatusConstants.HIDDEN))
            {
                int selectedStatus = status.Value;
                baseQuery = baseQuery.Where(x => x.TrangThai == selectedStatus);
            }
            else
            {
                status = null;
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

            var reviews = baseQuery
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .Select(x => new AdminReviewListItemVm
                {
                    DanhGiaId = x.DanhGiaId,

                    SanPhamId = x.SanPhamId,
                    MaSanPham = x.SanPham != null ? x.SanPham.MaSanPham : "",
                    TenSanPham = x.SanPham != null ? x.SanPham.TenSanPham : "Không xác định",
                    HinhAnhChinh = x.SanPham != null ? x.SanPham.HinhAnhChinh : "",

                    KhachHangId = x.KhachHangId,
                    TenKhachHang = x.KhachHang != null ? x.KhachHang.HoTen : "Khách hàng",
                    EmailKhachHang = x.KhachHang != null ? x.KhachHang.Email : "",

                    SoSao = x.SoSao,
                    NoiDung = x.NoiDung,

                    TrangThai = x.TrangThai,
                    PhanHoiAdmin = x.PhanHoiAdmin,

                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    NgayDuyet = x.NgayDuyet,

                    NguoiXuLy = x.TaiKhoan != null ? x.TaiKhoan.HoTen : ""
                })
                .ToList();

            return new AdminReviewIndexVm
            {
                Keyword = keyword,
                Rating = rating,
                Status = status,

                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = totalPages,

                TotalReviews = totalReviews,
                VisibleReviews = visibleReviews,
                HiddenReviews = hiddenReviews,
                AverageRating = averageRating,

                Reviews = reviews
            };
        }

        private TaiKhoan GetCurrentAccount()
        {
            var sessionAccount = Session["LoginInformation"] as TaiKhoan;

            if (sessionAccount == null)
            {
                return null;
            }

            return db.TaiKhoans.FirstOrDefault(x =>
                x.TaiKhoanId == sessionAccount.TaiKhoanId &&
                x.IsActive);
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