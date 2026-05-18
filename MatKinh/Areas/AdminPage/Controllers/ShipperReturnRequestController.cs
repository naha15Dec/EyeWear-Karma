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
    [CustomAuthorize(Roles = RoleConstants.SHIPPER)]
    public class ShipperReturnRequestController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int PAGE_SIZE = 10;

        [HttpGet]
        public ActionResult Index(string keyword = "", int? status = null, int page = 1)
        {
            var shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var model = BuildIndexViewModel(shipper.TaiKhoanId, keyword, status, page);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StartPickup(int id, string ghiChuShipper = "", string keyword = "", int? status = null, int page = 1)
        {
            var shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var request = GetAssignedRequestForShipper(id, shipper.TaiKhoanId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng được gán cho bạn.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (!ReturnRequestStatusConstants.CanShipperStartPickup(request.TrangThai))
            {
                TempData["ErrorMessage"] = "Chỉ có thể bắt đầu lấy hàng khi yêu cầu đã được admin giao cho shipper.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = request.TrangThai;

                    request.TrangThai = ReturnRequestStatusConstants.PICKING_UP;
                    request.NgayShipperBatDauLay = DateTime.Now;
                    request.GhiChuShipper = string.IsNullOrWhiteSpace(ghiChuShipper)
                        ? "Shipper bắt đầu đi lấy hàng trả từ khách."
                        : ghiChuShipper.Trim();
                    request.UpdatedAt = DateTime.Now;

                    AddReturnHistory(
                        request.YeuCauTraHangId,
                        oldStatus,
                        request.TrangThai,
                        shipper.TaiKhoanId,
                        request.GhiChuShipper);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã cập nhật trạng thái: Shipper đang lấy hàng.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Cập nhật trạng thái thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Index", new { keyword, status, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPickedUp(int id, string ghiChuShipper = "", string keyword = "", int? status = null, int page = 1)
        {
            var shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var request = GetAssignedRequestForShipper(id, shipper.TaiKhoanId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng được gán cho bạn.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (!ReturnRequestStatusConstants.CanShipperConfirmPickedUp(request.TrangThai))
            {
                TempData["ErrorMessage"] = "Chỉ có thể xác nhận đã lấy hàng khi đang ở trạng thái shipper đang lấy hàng.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = request.TrangThai;

                    request.TrangThai = ReturnRequestStatusConstants.PICKED_UP;
                    request.NgayShipperLayHang = DateTime.Now;
                    request.GhiChuShipper = string.IsNullOrWhiteSpace(ghiChuShipper)
                        ? "Shipper đã lấy hàng trả từ khách."
                        : ghiChuShipper.Trim();
                    request.UpdatedAt = DateTime.Now;

                    AddReturnHistory(
                        request.YeuCauTraHangId,
                        oldStatus,
                        request.TrangThai,
                        shipper.TaiKhoanId,
                        request.GhiChuShipper);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã cập nhật trạng thái: Shipper đã lấy hàng từ khách.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Cập nhật trạng thái thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Index", new { keyword, status, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandOverToStore(int id, string ghiChuShipper = "", string keyword = "", int? status = null, int page = 1)
        {
            var shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var request = GetAssignedRequestForShipper(id, shipper.TaiKhoanId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng được gán cho bạn.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (!ReturnRequestStatusConstants.CanShipperHandOver(request.TrangThai))
            {
                TempData["ErrorMessage"] = "Chỉ có thể bàn giao về cửa hàng sau khi đã lấy hàng từ khách.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = request.TrangThai;

                    request.TrangThai = ReturnRequestStatusConstants.HANDED_OVER;
                    request.NgayBanGiaoVeCuaHang = DateTime.Now;
                    request.GhiChuShipper = string.IsNullOrWhiteSpace(ghiChuShipper)
                        ? "Shipper đã bàn giao hàng trả về cửa hàng."
                        : ghiChuShipper.Trim();
                    request.UpdatedAt = DateTime.Now;

                    AddReturnHistory(
                        request.YeuCauTraHangId,
                        oldStatus,
                        request.TrangThai,
                        shipper.TaiKhoanId,
                        request.GhiChuShipper);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã cập nhật trạng thái: Đã bàn giao hàng trả về cửa hàng.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Bàn giao hàng trả thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Index", new { keyword, status, page });
        }

        [HttpGet]
        public ActionResult Detail(int id, string keyword = "", int? status = null, int page = 1)
        {
            var shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var request = db.YeuCauTraHangs
                .Include(x => x.DonHang)
                .Include(x => x.KhachHang)
                .Include(x => x.ChiTietTraHangs.Select(ct => ct.SanPham))
                .FirstOrDefault(x =>
                    x.YeuCauTraHangId == id &&
                    x.ShipperId == shipper.TaiKhoanId);

            if (request == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng được gán cho bạn.";
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

            return View(request);
        }
        private ShipperReturnRequestIndexVm BuildIndexViewModel(int shipperId, string keyword, int? status, int page)
        {
            keyword = (keyword ?? string.Empty).Trim();

            if (page <= 0)
            {
                page = 1;
            }

            var baseQuery = db.YeuCauTraHangs
                .Include(x => x.DonHang)
                .Include(x => x.KhachHang)
                .Include(x => x.ChiTietTraHangs)
                .Where(x => x.ShipperId == shipperId);

            int assignedCount = baseQuery.Count(x => x.TrangThai == ReturnRequestStatusConstants.ASSIGNED_TO_SHIPPER);
            int pickingUpCount = baseQuery.Count(x => x.TrangThai == ReturnRequestStatusConstants.PICKING_UP);
            int pickedUpCount = baseQuery.Count(x => x.TrangThai == ReturnRequestStatusConstants.PICKED_UP);
            int handedOverCount = baseQuery.Count(x => x.TrangThai == ReturnRequestStatusConstants.HANDED_OVER);

            var query = baseQuery.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaYeuCau.Contains(keyword) ||
                    x.LyDo.Contains(keyword) ||
                    x.DonHang.MaDonHang.Contains(keyword) ||
                    x.KhachHang.HoTen.Contains(keyword) ||
                    x.KhachHang.SoDienThoai.Contains(keyword) ||
                    x.KhachHang.Email.Contains(keyword) ||
                    x.DonHang.HoTenNguoiNhan.Contains(keyword) ||
                    x.DonHang.SoDienThoaiNguoiNhan.Contains(keyword) ||
                    x.DonHang.DiaChiNhanHang.Contains(keyword));
            }

            if (status.HasValue && IsValidShipperStatus(status.Value))
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
                .OrderByDescending(x => x.NgayGanShipper.HasValue ? x.NgayGanShipper.Value : x.NgayYeuCau)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToList();

            var items = rawItems.Select(x => new ShipperReturnRequestListItemVm
            {
                YeuCauTraHangId = x.YeuCauTraHangId,
                MaYeuCau = x.MaYeuCau,

                DonHangId = x.DonHangId,
                MaDonHang = x.DonHang != null ? x.DonHang.MaDonHang : "",

                KhachHangId = x.KhachHangId,
                TenKhachHang = x.KhachHang != null ? x.KhachHang.HoTen : "Khách hàng",
                SoDienThoaiKhachHang = x.KhachHang != null ? x.KhachHang.SoDienThoai : "",
                EmailKhachHang = x.KhachHang != null ? x.KhachHang.Email : "",

                HoTenNguoiNhan = x.DonHang != null ? x.DonHang.HoTenNguoiNhan : "",
                SoDienThoaiNguoiNhan = x.DonHang != null ? x.DonHang.SoDienThoaiNguoiNhan : "",
                DiaChiNhanHang = x.DonHang != null ? x.DonHang.DiaChiNhanHang : "",

                LyDo = x.LyDo,
                GhiChuKhachHang = x.GhiChuKhachHang,
                GhiChuAdmin = x.GhiChuAdmin,
                GhiChuShipper = x.GhiChuShipper,

                TrangThai = x.TrangThai,
                TrangThaiHoanTien = x.TrangThaiHoanTien,

                NgayYeuCau = x.NgayYeuCau,
                NgayDuyet = x.NgayDuyet,
                NgayGanShipper = x.NgayGanShipper,
                NgayShipperBatDauLay = x.NgayShipperBatDauLay,
                NgayShipperLayHang = x.NgayShipperLayHang,
                NgayBanGiaoVeCuaHang = x.NgayBanGiaoVeCuaHang,

                TongTienHoanDuKien = x.TongTienHoanDuKien,

                TotalProductLines = x.ChiTietTraHangs != null ? x.ChiTietTraHangs.Count : 0,
                TotalReturnQuantity = x.ChiTietTraHangs != null ? x.ChiTietTraHangs.Sum(ct => ct.SoLuongTra) : 0,
                ProductSummary = BuildProductSummary(x.ChiTietTraHangs),

                CanStartPickup = ReturnRequestStatusConstants.CanShipperStartPickup(x.TrangThai),
                CanConfirmPickedUp = ReturnRequestStatusConstants.CanShipperConfirmPickedUp(x.TrangThai),
                CanHandOver = ReturnRequestStatusConstants.CanShipperHandOver(x.TrangThai)
            }).ToList();

            return new ShipperReturnRequestIndexVm
            {
                Keyword = keyword,
                Status = status,

                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = totalPages,

                AssignedCount = assignedCount,
                PickingUpCount = pickingUpCount,
                PickedUpCount = pickedUpCount,
                HandedOverCount = handedOverCount,

                Requests = items
            };
        }

        private YeuCauTraHang GetAssignedRequestForShipper(int requestId, int shipperId)
        {
            return db.YeuCauTraHangs.FirstOrDefault(x =>
                x.YeuCauTraHangId == requestId &&
                x.ShipperId == shipperId);
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

        private bool IsValidShipperStatus(int status)
        {
            return status == ReturnRequestStatusConstants.ASSIGNED_TO_SHIPPER ||
                   status == ReturnRequestStatusConstants.PICKING_UP ||
                   status == ReturnRequestStatusConstants.PICKED_UP ||
                   status == ReturnRequestStatusConstants.HANDED_OVER;
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

            return db.TaiKhoans
                .Include(x => x.VaiTro)
                .FirstOrDefault(x =>
                    x.TaiKhoanId == sessionAccount.TaiKhoanId &&
                    x.IsActive &&
                    x.VaiTro != null &&
                    x.VaiTro.MaVaiTro == RoleConstants.SHIPPER);
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