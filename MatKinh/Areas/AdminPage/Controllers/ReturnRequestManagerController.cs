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
    public class ReturnRequestManagerController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int PAGE_SIZE = 10;

        [HttpGet]
        public ActionResult Index(string keyword = "", int? status = null, int page = 1)
        {
            var model = BuildIndexViewModel(keyword, status, page);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id, string ghiChuAdmin = "", string keyword = "", int? status = null, int page = 1)
        {
            var admin = GetCurrentAccount();
            var request = db.YeuCauTraHangs.FirstOrDefault(x => x.YeuCauTraHangId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (!ReturnRequestStatusConstants.CanAdminApprove(request.TrangThai))
            {
                TempData["ErrorMessage"] = "Yêu cầu này không thể duyệt ở trạng thái hiện tại.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = request.TrangThai;

                    request.TrangThai = ReturnRequestStatusConstants.APPROVED;
                    request.DuyetBoiId = admin != null ? (int?)admin.TaiKhoanId : null;
                    request.NgayDuyet = DateTime.Now;
                    request.GhiChuAdmin = string.IsNullOrWhiteSpace(ghiChuAdmin)
                        ? "Cửa hàng đã duyệt yêu cầu trả hàng."
                        : ghiChuAdmin.Trim();
                    request.UpdatedAt = DateTime.Now;

                    AddReturnHistory(
                        request.YeuCauTraHangId,
                        oldStatus,
                        request.TrangThai,
                        admin != null ? (int?)admin.TaiKhoanId : null,
                        request.GhiChuAdmin);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã duyệt yêu cầu trả hàng.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Duyệt yêu cầu thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Index", new { keyword, status, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id, string ghiChuAdmin = "", string keyword = "", int? status = null, int page = 1)
        {
            var admin = GetCurrentAccount();
            var request = db.YeuCauTraHangs.FirstOrDefault(x => x.YeuCauTraHangId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (!ReturnRequestStatusConstants.CanAdminApprove(request.TrangThai))
            {
                TempData["ErrorMessage"] = "Chỉ có thể từ chối yêu cầu đang chờ xử lý.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = request.TrangThai;

                    request.TrangThai = ReturnRequestStatusConstants.REJECTED;
                    request.DuyetBoiId = admin != null ? (int?)admin.TaiKhoanId : null;
                    request.NgayDuyet = DateTime.Now;
                    request.TrangThaiHoanTien = RefundStatusConstants.NO_REFUND;
                    request.GhiChuAdmin = string.IsNullOrWhiteSpace(ghiChuAdmin)
                        ? "Cửa hàng từ chối yêu cầu trả hàng."
                        : ghiChuAdmin.Trim();
                    request.UpdatedAt = DateTime.Now;

                    AddReturnHistory(
                        request.YeuCauTraHangId,
                        oldStatus,
                        request.TrangThai,
                        admin != null ? (int?)admin.TaiKhoanId : null,
                        request.GhiChuAdmin);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã từ chối yêu cầu trả hàng.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Từ chối yêu cầu thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Index", new { keyword, status, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignShipper(int id, int? shipperId, string ghiChuAdmin = "", string keyword = "", int? status = null, int page = 1)
        {
            var admin = GetCurrentAccount();

            var request = db.YeuCauTraHangs.FirstOrDefault(x => x.YeuCauTraHangId == id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (!shipperId.HasValue || shipperId.Value <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn shipper trước khi giao nhiệm vụ lấy hàng.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (!ReturnRequestStatusConstants.CanAdminAssignShipper(request.TrangThai))
            {
                TempData["ErrorMessage"] = "Chỉ có thể gán shipper khi yêu cầu đã được duyệt.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            var shipper = db.TaiKhoans
                .Include(x => x.VaiTro)
                .FirstOrDefault(x =>
                    x.TaiKhoanId == shipperId.Value &&
                    x.IsActive &&
                    x.VaiTro != null &&
                    x.VaiTro.MaVaiTro == RoleConstants.SHIPPER);

            if (shipper == null)
            {
                TempData["ErrorMessage"] = "Shipper không hợp lệ hoặc đã bị khóa.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = request.TrangThai;

                    request.TrangThai = ReturnRequestStatusConstants.ASSIGNED_TO_SHIPPER;
                    request.ShipperId = shipper.TaiKhoanId;
                    request.NgayGanShipper = DateTime.Now;
                    request.GhiChuAdmin = string.IsNullOrWhiteSpace(ghiChuAdmin)
                        ? "Cửa hàng đã giao shipper lấy hàng trả."
                        : ghiChuAdmin.Trim();
                    request.UpdatedAt = DateTime.Now;

                    AddReturnHistory(
                        request.YeuCauTraHangId,
                        oldStatus,
                        request.TrangThai,
                        admin != null ? (int?)admin.TaiKhoanId : null,
                        "Đã gán shipper: " + shipper.HoTen + ". " + request.GhiChuAdmin);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã gán shipper lấy hàng trả.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Gán shipper thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Index", new { keyword, status, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmReceived(int id, string ghiChuAdmin = "", string keyword = "", int? status = null, int page = 1)
        {
            var admin = GetCurrentAccount();

            var request = db.YeuCauTraHangs
                .Include(x => x.ChiTietTraHangs)
                .FirstOrDefault(x => x.YeuCauTraHangId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (!ReturnRequestStatusConstants.CanAdminReceive(request.TrangThai))
            {
                TempData["ErrorMessage"] = "Chỉ có thể xác nhận nhận hàng khi shipper đã bàn giao về cửa hàng.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = request.TrangThai;

                    request.TrangThai = ReturnRequestStatusConstants.RECEIVED;
                    request.NgayNhanHangVe = DateTime.Now;
                    request.TrangThaiHoanTien = RefundStatusConstants.MANUAL_REQUIRED;
                    request.GhiChuAdmin = string.IsNullOrWhiteSpace(ghiChuAdmin)
                        ? "Cửa hàng đã xác nhận nhận hàng trả về và cập nhật tồn kho."
                        : ghiChuAdmin.Trim();
                    request.UpdatedAt = DateTime.Now;

                    foreach (var item in request.ChiTietTraHangs)
                    {
                        var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == item.SanPhamId);
                        if (product != null)
                        {
                            product.SoLuongTon += item.SoLuongTra;
                            product.UpdatedAt = DateTime.Now;
                        }
                    }

                    AddReturnHistory(
                        request.YeuCauTraHangId,
                        oldStatus,
                        request.TrangThai,
                        admin != null ? (int?)admin.TaiKhoanId : null,
                        request.GhiChuAdmin);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã xác nhận nhận hàng trả về và hoàn kho.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Xác nhận nhận hàng thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Index", new { keyword, status, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmRefund(int id, decimal? tongTienHoanThucTe = null, string ghiChuAdmin = "", string keyword = "", int? status = null, int page = 1)
        {
            var admin = GetCurrentAccount();

            var request = db.YeuCauTraHangs.FirstOrDefault(x => x.YeuCauTraHangId == id);
            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (!ReturnRequestStatusConstants.CanAdminRefund(request.TrangThai))
            {
                TempData["ErrorMessage"] = "Chỉ có thể xác nhận hoàn tiền sau khi cửa hàng đã nhận hàng trả.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = request.TrangThai;

                    decimal refundAmount = tongTienHoanThucTe.HasValue && tongTienHoanThucTe.Value >= 0
                        ? tongTienHoanThucTe.Value
                        : request.TongTienHoanDuKien;

                    request.TrangThai = ReturnRequestStatusConstants.REFUNDED;
                    request.TrangThaiHoanTien = RefundStatusConstants.REFUNDED;
                    request.TongTienHoanThucTe = refundAmount;
                    request.NgayHoanTien = DateTime.Now;
                    request.GhiChuAdmin = string.IsNullOrWhiteSpace(ghiChuAdmin)
                        ? "Cửa hàng đã xác nhận hoàn tiền cho khách hàng."
                        : ghiChuAdmin.Trim();
                    request.UpdatedAt = DateTime.Now;

                    AddReturnHistory(
                        request.YeuCauTraHangId,
                        oldStatus,
                        request.TrangThai,
                        admin != null ? (int?)admin.TaiKhoanId : null,
                        request.GhiChuAdmin + " Số tiền hoàn: " + refundAmount.ToString("N0") + " đ.");

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã xác nhận hoàn tiền.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Xác nhận hoàn tiền thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Index", new { keyword, status, page });
        }

        [HttpGet]
        public ActionResult Detail(int id, string keyword = "", int? status = null, int page = 1)
        {
            var request = db.YeuCauTraHangs
                .Include(x => x.DonHang)
                .Include(x => x.KhachHang)
                .Include(x => x.ChiTietTraHangs.Select(ct => ct.SanPham))
                .FirstOrDefault(x => x.YeuCauTraHangId == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            ViewData["returnHistories"] = db.LichSuTrangThaiTraHangs
                .Include(x => x.TaiKhoan)
                .Where(x => x.YeuCauTraHangId == request.YeuCauTraHangId)
                .OrderBy(x => x.CreatedAt)
                .ToList();

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.Page = page;

            ViewBag.ShipperName = request.ShipperId.HasValue
                ? db.TaiKhoans
                    .Where(x => x.TaiKhoanId == request.ShipperId.Value)
                    .Select(x => x.HoTen)
                    .FirstOrDefault()
                : "";

            ViewBag.ApproverName = request.DuyetBoiId.HasValue
                ? db.TaiKhoans
                    .Where(x => x.TaiKhoanId == request.DuyetBoiId.Value)
                    .Select(x => x.HoTen)
                    .FirstOrDefault()
                : "";

            return View(request);
        }
        private AdminReturnRequestIndexVm BuildIndexViewModel(string keyword, int? status, int page)
        {
            keyword = (keyword ?? string.Empty).Trim();

            if (page <= 0)
            {
                page = 1;
            }

            var query = db.YeuCauTraHangs
                .Include(x => x.DonHang)
                .Include(x => x.KhachHang)
                .Include(x => x.ChiTietTraHangs)
                .AsQueryable();

            int totalRequests = query.Count();
            int pendingRequests = query.Count(x => x.TrangThai == ReturnRequestStatusConstants.PENDING);

            int processingRequests = query.Count(x =>
                x.TrangThai == ReturnRequestStatusConstants.APPROVED ||
                x.TrangThai == ReturnRequestStatusConstants.ASSIGNED_TO_SHIPPER ||
                x.TrangThai == ReturnRequestStatusConstants.PICKING_UP ||
                x.TrangThai == ReturnRequestStatusConstants.PICKED_UP ||
                x.TrangThai == ReturnRequestStatusConstants.HANDED_OVER ||
                x.TrangThai == ReturnRequestStatusConstants.RECEIVED);

            int completedRequests = query.Count(x => x.TrangThai == ReturnRequestStatusConstants.REFUNDED);
            int rejectedRequests = query.Count(x =>
                x.TrangThai == ReturnRequestStatusConstants.REJECTED ||
                x.TrangThai == ReturnRequestStatusConstants.CANCELLED);

            decimal totalExpectedRefund = query.Sum(x => (decimal?)x.TongTienHoanDuKien) ?? 0m;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaYeuCau.Contains(keyword) ||
                    x.LyDo.Contains(keyword) ||
                    x.DonHang.MaDonHang.Contains(keyword) ||
                    x.KhachHang.HoTen.Contains(keyword) ||
                    x.KhachHang.SoDienThoai.Contains(keyword) ||
                    x.KhachHang.Email.Contains(keyword));
            }

            if (status.HasValue && IsValidReturnStatus(status.Value))
            {
                int selectedStatus = status.Value;
                query = query.Where(x => x.TrangThai == selectedStatus);
            }
            else
            {
                status = null;
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

            var rawItems = query
                .OrderByDescending(x => x.NgayYeuCau)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToList();

            var shipperIds = rawItems
                .Where(x => x.ShipperId.HasValue)
                .Select(x => x.ShipperId.Value)
                .Distinct()
                .ToList();

            var approverIds = rawItems
                .Where(x => x.DuyetBoiId.HasValue)
                .Select(x => x.DuyetBoiId.Value)
                .Distinct()
                .ToList();

            var accountIds = shipperIds
                .Concat(approverIds)
                .Distinct()
                .ToList();

            var accountNames = db.TaiKhoans
                .Where(x => accountIds.Contains(x.TaiKhoanId))
                .ToDictionary(x => x.TaiKhoanId, x => x.HoTen);

            var items = rawItems.Select(x => new AdminReturnRequestListItemVm
            {
                YeuCauTraHangId = x.YeuCauTraHangId,
                MaYeuCau = x.MaYeuCau,

                DonHangId = x.DonHangId,
                MaDonHang = x.DonHang != null ? x.DonHang.MaDonHang : "",

                KhachHangId = x.KhachHangId,
                TenKhachHang = x.KhachHang != null ? x.KhachHang.HoTen : "Khách hàng",
                SoDienThoaiKhachHang = x.KhachHang != null ? x.KhachHang.SoDienThoai : "",
                EmailKhachHang = x.KhachHang != null ? x.KhachHang.Email : "",

                LyDo = x.LyDo,
                GhiChuKhachHang = x.GhiChuKhachHang,
                GhiChuAdmin = x.GhiChuAdmin,
                GhiChuShipper = x.GhiChuShipper,

                TrangThai = x.TrangThai,
                TrangThaiHoanTien = x.TrangThaiHoanTien,

                ShipperId = x.ShipperId,
                ShipperName = x.ShipperId.HasValue && accountNames.ContainsKey(x.ShipperId.Value)
                    ? accountNames[x.ShipperId.Value]
                    : "",

                DuyetBoiId = x.DuyetBoiId,
                DuyetBoiName = x.DuyetBoiId.HasValue && accountNames.ContainsKey(x.DuyetBoiId.Value)
                    ? accountNames[x.DuyetBoiId.Value]
                    : "",

                NgayYeuCau = x.NgayYeuCau,
                NgayDuyet = x.NgayDuyet,
                NgayGanShipper = x.NgayGanShipper,
                NgayShipperBatDauLay = x.NgayShipperBatDauLay,
                NgayShipperLayHang = x.NgayShipperLayHang,
                NgayBanGiaoVeCuaHang = x.NgayBanGiaoVeCuaHang,
                NgayNhanHangVe = x.NgayNhanHangVe,
                NgayHoanTien = x.NgayHoanTien,

                TongTienHoanDuKien = x.TongTienHoanDuKien,
                TongTienHoanThucTe = x.TongTienHoanThucTe,

                TotalProductLines = x.ChiTietTraHangs != null ? x.ChiTietTraHangs.Count : 0,
                TotalReturnQuantity = x.ChiTietTraHangs != null ? x.ChiTietTraHangs.Sum(ct => ct.SoLuongTra) : 0,
                ProductSummary = BuildProductSummary(x.ChiTietTraHangs),

                CanApprove = ReturnRequestStatusConstants.CanAdminApprove(x.TrangThai),
                CanReject = ReturnRequestStatusConstants.CanAdminApprove(x.TrangThai),
                CanAssignShipper = ReturnRequestStatusConstants.CanAdminAssignShipper(x.TrangThai),
                CanReceive = ReturnRequestStatusConstants.CanAdminReceive(x.TrangThai),
                CanRefund = ReturnRequestStatusConstants.CanAdminRefund(x.TrangThai)
            }).ToList();

            return new AdminReturnRequestIndexVm
            {
                Keyword = keyword,
                Status = status,

                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = totalPages,

                TotalRequests = totalRequests,
                PendingRequests = pendingRequests,
                ProcessingRequests = processingRequests,
                CompletedRequests = completedRequests,
                RejectedRequests = rejectedRequests,
                TotalExpectedRefund = totalExpectedRefund,

                StatusOptions = BuildStatusOptions(status),
                ShipperOptions = GetShipperOptions(),
                Requests = items
            };
        }

        private List<SelectListItem> BuildStatusOptions(int? selectedStatus)
        {
            var statuses = new[]
            {
                ReturnRequestStatusConstants.PENDING,
                ReturnRequestStatusConstants.APPROVED,
                ReturnRequestStatusConstants.ASSIGNED_TO_SHIPPER,
                ReturnRequestStatusConstants.PICKING_UP,
                ReturnRequestStatusConstants.PICKED_UP,
                ReturnRequestStatusConstants.HANDED_OVER,
                ReturnRequestStatusConstants.RECEIVED,
                ReturnRequestStatusConstants.REFUNDED,
                ReturnRequestStatusConstants.REJECTED,
                ReturnRequestStatusConstants.CANCELLED
            };

            return statuses.Select(x => new SelectListItem
            {
                Value = x.ToString(),
                Text = ReturnRequestStatusConstants.GetName(x),
                Selected = selectedStatus.HasValue && selectedStatus.Value == x
            }).ToList();
        }

        private List<SelectListItem> GetShipperOptions()
        {
            return db.TaiKhoans
                .Include(x => x.VaiTro)
                .Where(x =>
                    x.IsActive &&
                    x.VaiTro != null &&
                    x.VaiTro.MaVaiTro == RoleConstants.SHIPPER)
                .OrderBy(x => x.HoTen)
                .Select(x => new SelectListItem
                {
                    Value = x.TaiKhoanId.ToString(),
                    Text = x.HoTen
                })
                .ToList();
        }

        private string BuildProductSummary(IEnumerable<ChiTietTraHang> details)
        {
            if (details == null || !details.Any())
            {
                return "Chưa có sản phẩm trả.";
            }

            var list = details
                .OrderBy(x => x.ChiTietTraHangId)
                .ToList();

            var first = list.FirstOrDefault();

            if (first == null)
            {
                return "Chưa có sản phẩm trả.";
            }

            string name = first.TenSanPhamSnapshot ?? "Sản phẩm";

            if (name.Length > 42)
            {
                name = name.Substring(0, 42) + "...";
            }

            string result = name + " x" + first.SoLuongTra;

            int remain = list.Count - 1;

            if (remain > 0)
            {
                result += " +" + remain + " sản phẩm khác";
            }

            return result;
        }

        private bool IsValidReturnStatus(int status)
        {
            return status >= ReturnRequestStatusConstants.PENDING &&
                   status <= ReturnRequestStatusConstants.CANCELLED;
        }

        private void AddReturnHistory(int requestId, int? oldStatus, int newStatus, int? changedById, string note)
        {
            db.LichSuTrangThaiTraHangs.Add(new LichSuTrangThaiTraHang
            {
                YeuCauTraHangId = requestId,
                TrangThaiCu = oldStatus,
                TrangThaiMoi = newStatus,
                ThayDoiBoiId = changedById,
                GhiChu = note,
                CreatedAt = DateTime.Now
            });
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