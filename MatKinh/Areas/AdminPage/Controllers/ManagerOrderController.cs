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
    [CustomAuthorize(Roles = RoleConstants.ADMIN + "," + RoleConstants.STAFF + "," + RoleConstants.SHIPPER)]
    public class ManagerOrderController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();
        private const int PAGE_SIZE = 10;

        [HttpGet]
        public ActionResult Index(string keyword = "", int? status = null, int page = 1)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var model = BuildIndexViewModel(currentUser, keyword, status, page);
            return View(model);
        }

        [HttpGet]
        public ActionResult Detail(int id)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var order = GetOrderQuery()
                .FirstOrDefault(x => x.DonHangId == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            if (!CanViewOrder(currentUser, order))
            {
                return Redirect("~/Error/Index");
            }

            var model = BuildDetailViewModel(order);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(AdminOrderUpdateStatusVm model)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var order = GetOrderQuery()
                .FirstOrDefault(x => x.DonHangId == model.DonHangId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            if (!CanViewOrder(currentUser, order))
            {
                return Redirect("~/Error/Index");
            }

            if (!CanUpdateStatus(currentUser, order, model.TrangThaiMoi, out string error))
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Detail", new { id = model.DonHangId });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = order.TrangThai;
                    int newStatus = model.TrangThaiMoi;

                    order.TrangThai = newStatus;
                    order.UpdatedAt = DateTime.Now;

                    ApplyOrderDateByStatus(order, currentUser, newStatus);

                    if (ShouldRestoreStock(oldStatus, newStatus))
                    {
                        RestoreStockForOrder(order);
                    }

                    UpdatePaymentWhenOrderFinal(order, newStatus);

                    string historyNote = BuildStatusHistoryNote(order, newStatus, model.GhiChu);

                    AddOrderHistory(
                        order.DonHangId,
                        oldStatus,
                        newStatus,
                        historyNote,
                        currentUser.TaiKhoanId);

                    db.SaveChanges();
                    transaction.Commit();

                    if (NeedManualRefund(order, newStatus))
                    {
                        TempData["SuccessMessage"] = "Cập nhật trạng thái thành công. Đơn hàng đã thanh toán qua VNPay, vui lòng xử lý hoàn tiền trong 3–5 ngày làm việc.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công.";
                    }

                    return RedirectToAction("Detail", new { id = model.DonHangId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    TempData["ErrorMessage"] = "Cập nhật trạng thái thất bại: " + ex.Message;
                    return RedirectToAction("Detail", new { id = model.DonHangId });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignShipper(AdminOrderAssignShipperVm model)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            if (!IsAdminOrStaff(currentUser))
            {
                return Redirect("~/Error/Index");
            }

            if (model.ShipperId <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn shipper giao hàng.";
                return RedirectToAction("Detail", new { id = model.DonHangId });
            }

            var order = GetOrderQuery()
                .FirstOrDefault(x => x.DonHangId == model.DonHangId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
            }

            if (!CanViewOrder(currentUser, order))
            {
                return Redirect("~/Error/Index");
            }

            if (IsFinalStatus(order.TrangThai))
            {
                TempData["ErrorMessage"] = "Đơn hàng đã ở trạng thái kết thúc, không thể gán shipper.";
                return RedirectToAction("Detail", new { id = model.DonHangId });
            }

            var shipper = db.TaiKhoans
                .Include(x => x.VaiTro)
                .FirstOrDefault(x =>
                    x.TaiKhoanId == model.ShipperId &&
                    x.IsActive &&
                    x.VaiTro.MaVaiTro == RoleConstants.SHIPPER);

            if (shipper == null)
            {
                TempData["ErrorMessage"] = "Shipper không hợp lệ.";
                return RedirectToAction("Detail", new { id = model.DonHangId });
            }

            if (order.TrangThai != OrderStatusConstants.PREPARING &&
                order.TrangThai != OrderStatusConstants.ASSIGNED_TO_SHIPPER)
            {
                TempData["ErrorMessage"] = "Chỉ được gán shipper khi đơn đang chuẩn bị hoặc đã giao shipper.";
                return RedirectToAction("Detail", new { id = model.DonHangId });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = order.TrangThai;

                    order.ShipperId = shipper.TaiKhoanId;
                    order.TrangThai = OrderStatusConstants.ASSIGNED_TO_SHIPPER;
                    order.UpdatedAt = DateTime.Now;

                    AddOrderHistory(
                        order.DonHangId,
                        oldStatus,
                        OrderStatusConstants.ASSIGNED_TO_SHIPPER,
                        string.IsNullOrWhiteSpace(model.GhiChu)
                            ? "Gán shipper: " + shipper.HoTen
                            : model.GhiChu,
                        currentUser.TaiKhoanId);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Gán shipper thành công. Đơn hàng đã chuyển sang trạng thái đã giao shipper.";
                    return RedirectToAction("Detail", new { id = model.DonHangId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    TempData["ErrorMessage"] = "Gán shipper thất bại: " + ex.Message;
                    return RedirectToAction("Detail", new { id = model.DonHangId });
                }
            }
        }

        private AdminOrderIndexVm BuildIndexViewModel(TaiKhoan currentUser, string keyword, int? status, int page)
        {
            keyword = (keyword ?? string.Empty).Trim();

            if (page <= 0)
            {
                page = 1;
            }

            var query = GetOrderQuery();

            if (IsShipper(currentUser))
            {
                query = query.Where(x => x.ShipperId == currentUser.TaiKhoanId);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaDonHang.Contains(keyword) ||
                    x.HoTenNguoiNhan.Contains(keyword) ||
                    x.SoDienThoaiNguoiNhan.Contains(keyword));
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.TrangThai == status.Value);
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

            var orders = query
                .OrderByDescending(x => x.NgayDat)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .Select(x => new AdminOrderListItemVm
                {
                    DonHangId = x.DonHangId,
                    MaDonHang = x.MaDonHang,
                    TenKhachHang = x.KhachHang.HoTen,
                    HoTenNguoiNhan = x.HoTenNguoiNhan,
                    SoDienThoaiNguoiNhan = x.SoDienThoaiNguoiNhan,
                    DiaChiNhanHang = x.DiaChiNhanHang,
                    TongTienHang = x.TongTienHang,
                    PhiVanChuyen = x.PhiVanChuyen,
                    GiamGia = x.GiamGia,
                    TongThanhToan = x.TongThanhToan,
                    TrangThai = x.TrangThai,
                    TrangThaiText = "",
                    PhuongThucThanhToan = x.PhuongThucThanhToan,
                    TrangThaiThanhToan = x.TrangThaiThanhToan,
                    MaGiaoDichThanhToan = x.MaGiaoDichThanhToan,
                    NgayThanhToan = x.NgayThanhToan,
                    CanRequireManualRefund =
                        (x.TrangThai == OrderStatusConstants.CANCELLED ||
                         x.TrangThai == OrderStatusConstants.DELIVERY_FAILED) &&
                        x.PhuongThucThanhToan == PaymentConstants.VNPAY &&
                        x.TrangThaiThanhToan == PaymentConstants.PAID,
                    NguoiTao = x.TaiKhoan1 != null ? x.TaiKhoan1.HoTen : "",
                    NguoiXacNhan = x.TaiKhoan != null ? x.TaiKhoan.HoTen : "",
                    ShipperName = x.TaiKhoan2 != null ? x.TaiKhoan2.HoTen : "",
                    NgayDat = x.NgayDat,
                    NgayXacNhan = x.NgayXacNhan,
                    NgayGiao = x.NgayGiao,
                    NgayHoanTat = x.NgayHoanTat,
                    NgayHuy = x.NgayHuy,
                    SoLuongSanPham = x.ChiTietDonHangs.Sum(ct => (int?)ct.SoLuong) ?? 0
                })
                .ToList();

            foreach (var item in orders)
            {
                item.TrangThaiText = OrderStatusConstants.GetName(item.TrangThai);
            }

            var model = new AdminOrderIndexVm
            {
                Keyword = keyword,
                StatusFilter = status,
                HeaderTitle = IsShipper(currentUser) ? "Đơn hàng được giao cho tôi" : "Quản lý đơn hàng",
                StatusOptions = BuildStatusOptions(status),
                Orders = orders,

                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return model;
        }

        private AdminOrderDetailVm BuildDetailViewModel(DonHang order)
        {
            var model = new AdminOrderDetailVm
            {
                DonHangId = order.DonHangId,
                MaDonHang = order.MaDonHang,
                KhachHangId = order.KhachHangId,
                TenKhachHang = order.KhachHang != null ? order.KhachHang.HoTen : string.Empty,
                ShipperId = order.ShipperId,
                ShipperName = order.TaiKhoan2 != null ? order.TaiKhoan2.HoTen : string.Empty,
                ConfirmedById = order.ConfirmedById,
                ConfirmedByName = order.TaiKhoan != null ? order.TaiKhoan.HoTen : string.Empty,
                CreatedById = order.CreatedById,
                CreatedByName = order.TaiKhoan1 != null ? order.TaiKhoan1.HoTen : string.Empty,
                HoTenNguoiNhan = order.HoTenNguoiNhan,
                SoDienThoaiNguoiNhan = order.SoDienThoaiNguoiNhan,
                DiaChiNhanHang = order.DiaChiNhanHang,
                GhiChu = order.GhiChu,
                TongTienHang = order.TongTienHang,
                PhiVanChuyen = order.PhiVanChuyen,
                GiamGia = order.GiamGia,
                TongThanhToan = order.TongThanhToan,
                TrangThai = order.TrangThai,
                TrangThaiText = OrderStatusConstants.GetName(order.TrangThai),
                PhuongThucThanhToan = order.PhuongThucThanhToan,
                TrangThaiThanhToan = order.TrangThaiThanhToan,
                MaGiaoDichThanhToan = order.MaGiaoDichThanhToan,
                NgayThanhToan = order.NgayThanhToan,
                CanRequireManualRefund = NeedManualRefund(order, order.TrangThai),
                NgayDat = order.NgayDat,
                NgayXacNhan = order.NgayXacNhan,
                NgayGiao = order.NgayGiao,
                NgayHoanTat = order.NgayHoanTat,
                NgayHuy = order.NgayHuy,

                Items = order.ChiTietDonHangs
                    .Select(x => new AdminOrderDetailItemVm
                    {
                        ChiTietDonHangId = x.ChiTietDonHangId,
                        SanPhamId = x.SanPhamId,
                        TenSanPhamSnapshot = x.TenSanPhamSnapshot,
                        DonGiaSnapshot = x.DonGiaSnapshot,
                        SoLuong = x.SoLuong,
                        GiamGiaSnapshot = x.GiamGiaSnapshot,
                        ThanhTien = x.ThanhTien,
                        HinhAnhChinh = x.SanPham != null ? x.SanPham.HinhAnhChinh : "",
                        MaSanPham = x.SanPham != null ? x.SanPham.MaSanPham : ""
                    })
                    .ToList(),

                Histories = order.LichSuTrangThaiDonHangs
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new AdminOrderHistoryVm
                    {
                        LichSuTrangThaiDonHangId = x.LichSuId,
                        TrangThaiCu = x.TrangThaiCu ?? 0,
                        TrangThaiCuText = OrderStatusConstants.GetName(x.TrangThaiCu ?? 0),
                        TrangThaiMoi = x.TrangThaiMoi,
                        TrangThaiMoiText = OrderStatusConstants.GetName(x.TrangThaiMoi),
                        GhiChu = x.GhiChu,
                        NguoiCapNhat = x.TaiKhoan != null ? x.TaiKhoan.HoTen : "",
                        CreatedAt = x.CreatedAt
                    })
                    .ToList(),

                Shippers = db.TaiKhoans
                    .Include(x => x.VaiTro)
                    .Where(x => x.IsActive && x.VaiTro.MaVaiTro == RoleConstants.SHIPPER)
                    .OrderBy(x => x.HoTen)
                    .Select(x => new SelectListItem
                    {
                        Value = x.TaiKhoanId.ToString(),
                        Text = x.HoTen + " - " + x.TenDangNhap
                    })
                    .ToList()
            };

            return model;
        }

        private IQueryable<DonHang> GetOrderQuery()
        {
            return db.DonHangs
                .Include(x => x.KhachHang)
                .Include(x => x.ChiTietDonHangs.Select(ct => ct.SanPham))
                .Include(x => x.LichSuTrangThaiDonHangs.Select(ls => ls.TaiKhoan))
                .Include(x => x.TaiKhoan)
                .Include(x => x.TaiKhoan1)
                .Include(x => x.TaiKhoan2);
        }

        private List<SelectListItem> BuildStatusOptions(int? selectedStatus)
        {
            var statuses = new List<int>
            {
                OrderStatusConstants.PENDING,
                OrderStatusConstants.CONFIRMED,
                OrderStatusConstants.PREPARING,
                OrderStatusConstants.ASSIGNED_TO_SHIPPER,
                OrderStatusConstants.DELIVERING,
                OrderStatusConstants.DELIVERED,
                OrderStatusConstants.DELIVERY_FAILED,
                OrderStatusConstants.CANCELLED
            };

            return statuses.Select(x => new SelectListItem
            {
                Value = x.ToString(),
                Text = OrderStatusConstants.GetName(x),
                Selected = selectedStatus.HasValue && selectedStatus.Value == x
            }).ToList();
        }

        private void AddOrderHistory(int donHangId, int trangThaiCu, int trangThaiMoi, string ghiChu, int taiKhoanId)
        {
            var history = new LichSuTrangThaiDonHang
            {
                DonHangId = donHangId,
                TrangThaiCu = trangThaiCu,
                TrangThaiMoi = trangThaiMoi,
                ThayDoiBoiId = taiKhoanId,
                GhiChu = string.IsNullOrWhiteSpace(ghiChu)
                    ? "Cập nhật trạng thái đơn hàng."
                    : ghiChu.Trim(),
                CreatedAt = DateTime.Now
            };

            db.LichSuTrangThaiDonHangs.Add(history);
        }

        private string BuildStatusHistoryNote(DonHang order, int newStatus, string inputNote)
        {
            string note = string.IsNullOrWhiteSpace(inputNote)
                ? "Cập nhật trạng thái đơn hàng."
                : inputNote.Trim();

            if (NeedManualRefund(order, newStatus))
            {
                note += " Đơn hàng đã thanh toán qua VNPay nhưng đã bị hủy/giao thất bại. Cửa hàng cần kiểm tra giao dịch và xử lý hoàn tiền trong 3–5 ngày làm việc.";
            }

            return note;
        }

        private void ApplyOrderDateByStatus(DonHang order, TaiKhoan currentUser, int newStatus)
        {
            if (order == null)
            {
                return;
            }

            if (newStatus == OrderStatusConstants.CONFIRMED)
            {
                order.ConfirmedById = currentUser.TaiKhoanId;
                order.NgayXacNhan = DateTime.Now;
            }

            if (newStatus == OrderStatusConstants.DELIVERING)
            {
                order.NgayGiao = DateTime.Now;
            }

            if (newStatus == OrderStatusConstants.DELIVERED)
            {
                order.NgayHoanTat = DateTime.Now;
            }

            if (newStatus == OrderStatusConstants.CANCELLED)
            {
                order.NgayHuy = DateTime.Now;
            }
        }

        private bool ShouldRestoreStock(int oldStatus, int newStatus)
        {
            bool isFinalFailure =
                newStatus == OrderStatusConstants.CANCELLED ||
                newStatus == OrderStatusConstants.DELIVERY_FAILED;

            bool wasAlreadyFinalFailure =
                oldStatus == OrderStatusConstants.CANCELLED ||
                oldStatus == OrderStatusConstants.DELIVERY_FAILED;

            return isFinalFailure && !wasAlreadyFinalFailure;
        }

        private void RestoreStockForOrder(DonHang order)
        {
            if (order == null || order.ChiTietDonHangs == null)
            {
                return;
            }

            foreach (var item in order.ChiTietDonHangs)
            {
                var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == item.SanPhamId);
                if (product != null)
                {
                    product.SoLuongTon += item.SoLuong;
                    product.UpdatedAt = DateTime.Now;
                }
            }
        }

        private void UpdatePaymentWhenOrderFinal(DonHang order, int newStatus)
        {
            if (order == null)
            {
                return;
            }

            if (newStatus == OrderStatusConstants.DELIVERED)
            {
                if (string.Equals(order.PhuongThucThanhToan, PaymentConstants.COD, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(order.TrangThaiThanhToan, PaymentConstants.PAID, StringComparison.OrdinalIgnoreCase))
                {
                    order.TrangThaiThanhToan = PaymentConstants.PAID;
                    order.NgayThanhToan = DateTime.Now;
                }

                return;
            }

            if (newStatus == OrderStatusConstants.CANCELLED ||
                newStatus == OrderStatusConstants.DELIVERY_FAILED)
            {
                if (string.Equals(order.PhuongThucThanhToan, PaymentConstants.COD, StringComparison.OrdinalIgnoreCase))
                {
                    order.TrangThaiThanhToan = PaymentConstants.FAILED;
                    order.NgayThanhToan = null;
                }

                // Lưu ý:
                // Nếu VNPAY đã Paid mà bị hủy/giao thất bại thì KHÔNG tự set Refunded.
                // Giữ Paid để admin biết đơn này cần xử lý hoàn tiền thủ công.
            }
        }

        private bool NeedManualRefund(DonHang order, int newStatus)
        {
            if (order == null)
            {
                return false;
            }

            bool isCancelledOrFailed =
                newStatus == OrderStatusConstants.CANCELLED ||
                newStatus == OrderStatusConstants.DELIVERY_FAILED;

            return isCancelledOrFailed &&
                   string.Equals(order.PhuongThucThanhToan, PaymentConstants.VNPAY, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(order.TrangThaiThanhToan, PaymentConstants.PAID, StringComparison.OrdinalIgnoreCase);
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
                .FirstOrDefault(x => x.TaiKhoanId == sessionAccount.TaiKhoanId && x.IsActive);
        }

        private bool IsAdmin(TaiKhoan account)
        {
            return account != null &&
                   account.VaiTro != null &&
                   string.Equals(account.VaiTro.MaVaiTro, RoleConstants.ADMIN, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsStaff(TaiKhoan account)
        {
            return account != null &&
                   account.VaiTro != null &&
                   string.Equals(account.VaiTro.MaVaiTro, RoleConstants.STAFF, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsShipper(TaiKhoan account)
        {
            return account != null &&
                   account.VaiTro != null &&
                   string.Equals(account.VaiTro.MaVaiTro, RoleConstants.SHIPPER, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsAdminOrStaff(TaiKhoan account)
        {
            return IsAdmin(account) || IsStaff(account);
        }

        private bool CanViewOrder(TaiKhoan currentUser, DonHang order)
        {
            if (IsAdmin(currentUser) || IsStaff(currentUser))
            {
                return true;
            }

            if (IsShipper(currentUser))
            {
                return order.ShipperId.HasValue &&
                       order.ShipperId.Value == currentUser.TaiKhoanId;
            }

            return false;
        }

        private bool CanUpdateStatus(TaiKhoan currentUser, DonHang order, int newStatus, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (order == null)
            {
                errorMessage = "Không tìm thấy đơn hàng.";
                return false;
            }

            if (IsFinalStatus(order.TrangThai))
            {
                errorMessage = "Đơn hàng đã ở trạng thái kết thúc, không thể cập nhật tiếp.";
                return false;
            }

            if (IsShipper(currentUser))
            {
                return CanShipperUpdateStatus(currentUser, order, newStatus, out errorMessage);
            }

            if (IsStaff(currentUser))
            {
                return CanStaffUpdateStatus(order, newStatus, out errorMessage);
            }

            if (IsAdmin(currentUser))
            {
                return CanAdminUpdateStatus(order, newStatus, out errorMessage);
            }

            errorMessage = "Bạn không có quyền cập nhật trạng thái đơn hàng.";
            return false;
        }

        private bool CanShipperUpdateStatus(TaiKhoan currentUser, DonHang order, int newStatus, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!order.ShipperId.HasValue || order.ShipperId.Value != currentUser.TaiKhoanId)
            {
                errorMessage = "Bạn không có quyền xử lý đơn hàng này.";
                return false;
            }

            if (newStatus != OrderStatusConstants.DELIVERING &&
                newStatus != OrderStatusConstants.DELIVERED &&
                newStatus != OrderStatusConstants.DELIVERY_FAILED)
            {
                errorMessage = "Shipper chỉ được cập nhật trạng thái giao hàng.";
                return false;
            }

            if (order.TrangThai == OrderStatusConstants.ASSIGNED_TO_SHIPPER &&
                newStatus == OrderStatusConstants.DELIVERING)
            {
                return true;
            }

            if (order.TrangThai == OrderStatusConstants.DELIVERING &&
                (newStatus == OrderStatusConstants.DELIVERED ||
                 newStatus == OrderStatusConstants.DELIVERY_FAILED))
            {
                return true;
            }

            errorMessage = "Không thể chuyển trạng thái theo luồng hiện tại.";
            return false;
        }

        private bool CanStaffUpdateStatus(DonHang order, int newStatus, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (newStatus == OrderStatusConstants.CANCELLED)
            {
                errorMessage = "Nhân viên không được hủy đơn. Vui lòng liên hệ quản trị viên.";
                return false;
            }

            if (order.TrangThai == OrderStatusConstants.PENDING &&
                newStatus == OrderStatusConstants.CONFIRMED)
            {
                return true;
            }

            if (order.TrangThai == OrderStatusConstants.CONFIRMED &&
                newStatus == OrderStatusConstants.PREPARING)
            {
                return true;
            }

            if (order.TrangThai == OrderStatusConstants.PREPARING &&
                newStatus == OrderStatusConstants.ASSIGNED_TO_SHIPPER)
            {
                errorMessage = "Vui lòng chọn shipper giao hàng để chuyển đơn sang trạng thái đã giao shipper.";
                return false;
            }

            errorMessage = "Không thể chuyển trạng thái theo luồng hiện tại.";
            return false;
        }

        private bool CanAdminUpdateStatus(DonHang order, int newStatus, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (order.TrangThai == OrderStatusConstants.PENDING &&
                (newStatus == OrderStatusConstants.CONFIRMED ||
                 newStatus == OrderStatusConstants.CANCELLED))
            {
                return true;
            }

            if (order.TrangThai == OrderStatusConstants.CONFIRMED &&
                (newStatus == OrderStatusConstants.PREPARING ||
                 newStatus == OrderStatusConstants.CANCELLED))
            {
                return true;
            }

            if (order.TrangThai == OrderStatusConstants.PREPARING &&
                newStatus == OrderStatusConstants.CANCELLED)
            {
                return true;
            }

            if (order.TrangThai == OrderStatusConstants.PREPARING &&
                newStatus == OrderStatusConstants.ASSIGNED_TO_SHIPPER)
            {
                errorMessage = "Vui lòng chọn shipper giao hàng để chuyển đơn sang trạng thái đã giao shipper.";
                return false;
            }

            if (order.TrangThai == OrderStatusConstants.ASSIGNED_TO_SHIPPER &&
                newStatus == OrderStatusConstants.DELIVERING)
            {
                if (!order.ShipperId.HasValue)
                {
                    errorMessage = "Đơn hàng chưa được gán shipper.";
                    return false;
                }

                return true;
            }

            if (order.TrangThai == OrderStatusConstants.DELIVERING &&
                (newStatus == OrderStatusConstants.DELIVERED ||
                 newStatus == OrderStatusConstants.DELIVERY_FAILED))
            {
                return true;
            }

            if (order.TrangThai == OrderStatusConstants.DELIVERING &&
                newStatus == OrderStatusConstants.CANCELLED)
            {
                errorMessage = "Đơn hàng đang giao không nên hủy trực tiếp. Vui lòng chuyển sang giao thành công hoặc giao thất bại.";
                return false;
            }

            errorMessage = "Không thể chuyển trạng thái theo luồng hiện tại.";
            return false;
        }

        private bool IsFinalStatus(int status)
        {
            return status == OrderStatusConstants.DELIVERED ||
                   status == OrderStatusConstants.DELIVERY_FAILED ||
                   status == OrderStatusConstants.CANCELLED;
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