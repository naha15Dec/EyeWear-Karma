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

        [HttpGet]
        public ActionResult Index(string keyword = "", int? status = null)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var model = BuildIndexViewModel(currentUser, keyword, status);
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

            int oldStatus = order.TrangThai;
            int newStatus = model.TrangThaiMoi;

            order.TrangThai = newStatus;
            order.UpdatedAt = DateTime.Now;

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

            if (newStatus == OrderStatusConstants.DELIVERED &&
                string.Equals(order.PhuongThucThanhToan, PaymentConstants.COD, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(order.TrangThaiThanhToan, PaymentConstants.PAID, StringComparison.OrdinalIgnoreCase))
            {
                order.TrangThaiThanhToan = PaymentConstants.PAID;
                order.NgayThanhToan = DateTime.Now;
            }

            if (newStatus == OrderStatusConstants.CANCELLED)
            {
                order.NgayHuy = DateTime.Now;
            }

            AddOrderHistory(order.DonHangId, oldStatus, newStatus, model.GhiChu, currentUser.TaiKhoanId);

            db.SaveChanges();

            TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công.";
            return RedirectToAction("Detail", new { id = model.DonHangId });
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

            var order = GetOrderQuery()
                .FirstOrDefault(x => x.DonHangId == model.DonHangId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index");
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

            TempData["SuccessMessage"] = "Gán shipper thành công.";
            return RedirectToAction("Detail", new { id = model.DonHangId });
        }

        private AdminOrderIndexVm BuildIndexViewModel(TaiKhoan currentUser, string keyword, int? status)
        {
            keyword = (keyword ?? string.Empty).Trim();

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

            var model = new AdminOrderIndexVm
            {
                Keyword = keyword,
                StatusFilter = status,
                HeaderTitle = IsShipper(currentUser) ? "Đơn hàng được giao cho tôi" : "Quản lý đơn hàng",
                StatusOptions = BuildStatusOptions(status),
                Orders = query
                    .OrderByDescending(x => x.NgayDat)
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
                    .ToList()
            };

            foreach (var item in model.Orders)
            {
                item.TrangThaiText = OrderStatusConstants.GetName(item.TrangThai);
            }

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
                GhiChu = ghiChu,
                CreatedAt = DateTime.Now
            };

            db.LichSuTrangThaiDonHangs.Add(history);
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
                return order.ShipperId.HasValue && order.ShipperId.Value == currentUser.TaiKhoanId;
            }

            return false;
        }

        private bool CanUpdateStatus(TaiKhoan currentUser, DonHang order, int newStatus, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (IsShipper(currentUser))
            {
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
                    (newStatus == OrderStatusConstants.DELIVERED || newStatus == OrderStatusConstants.DELIVERY_FAILED))
                {
                    return true;
                }

                errorMessage = "Không thể chuyển trạng thái theo luồng hiện tại.";
                return false;
            }

            if (IsStaff(currentUser))
            {
                if (newStatus == OrderStatusConstants.CANCELLED)
                {
                    errorMessage = "Nhân viên không được hủy đơn.";
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
                    return true;
                }

                errorMessage = "Không thể chuyển trạng thái theo luồng hiện tại.";
                return false;
            }

            if (IsAdmin(currentUser))
            {
                if (order.TrangThai == OrderStatusConstants.PENDING &&
                    (newStatus == OrderStatusConstants.CONFIRMED || newStatus == OrderStatusConstants.CANCELLED))
                {
                    return true;
                }

                if (order.TrangThai == OrderStatusConstants.CONFIRMED &&
                    newStatus == OrderStatusConstants.PREPARING)
                {
                    return true;
                }

                if (order.TrangThai == OrderStatusConstants.PREPARING &&
                    (newStatus == OrderStatusConstants.ASSIGNED_TO_SHIPPER || newStatus == OrderStatusConstants.CANCELLED))
                {
                    return true;
                }

                if (order.TrangThai == OrderStatusConstants.ASSIGNED_TO_SHIPPER &&
                    newStatus == OrderStatusConstants.DELIVERING)
                {
                    return true;
                }

                if (order.TrangThai == OrderStatusConstants.DELIVERING &&
                    (newStatus == OrderStatusConstants.DELIVERED ||
                     newStatus == OrderStatusConstants.DELIVERY_FAILED ||
                     newStatus == OrderStatusConstants.CANCELLED))
                {
                    return true;
                }

                errorMessage = "Không thể chuyển trạng thái theo luồng hiện tại.";
                return false;
            }

            errorMessage = "Bạn không có quyền cập nhật trạng thái đơn hàng.";
            return false;
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