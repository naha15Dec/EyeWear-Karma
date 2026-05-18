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
    public class ShipperOrderController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int PAGE_SIZE = 10;

        [HttpGet]
        public ActionResult Index(string keyword = "", int? status = null, int page = 1)
        {
            TaiKhoan shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            var model = BuildIndexViewModel(shipper.TaiKhoanId, keyword, status, page);
            return View(model);
        }

        [HttpGet]
        public ActionResult Detail(int id, string keyword = "", int? status = null, int page = 1)
        {
            TaiKhoan shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            DonHang order = GetOrderQuery()
                .FirstOrDefault(x =>
                    x.DonHangId == id &&
                    x.ShipperId == shipper.TaiKhoanId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng được giao cho bạn.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            AdminOrderDetailVm model = BuildDetailViewModel(order);

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.Page = page;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StartDelivery(int id, string ghiChu = "", string keyword = "", int? status = null, int page = 1)
        {
            TaiKhoan shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            DonHang order = GetAssignedOrderForShipper(id, shipper.TaiKhoanId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng được giao cho bạn.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (order.TrangThai != OrderStatusConstants.ASSIGNED_TO_SHIPPER)
            {
                TempData["ErrorMessage"] = "Chỉ có thể bắt đầu giao khi đơn hàng ở trạng thái đã giao shipper.";
                return RedirectToAction("Detail", new { id, keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = order.TrangThai;

                    order.TrangThai = OrderStatusConstants.DELIVERING;
                    order.NgayGiao = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;

                    AddOrderHistory(
                        order.DonHangId,
                        oldStatus,
                        OrderStatusConstants.DELIVERING,
                        string.IsNullOrWhiteSpace(ghiChu)
                            ? "Shipper bắt đầu giao hàng."
                            : ghiChu.Trim(),
                        shipper.TaiKhoanId);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã cập nhật trạng thái: Đang giao hàng.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Cập nhật trạng thái thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Detail", new { id, keyword, status, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDelivered(int id, string ghiChu = "", string keyword = "", int? status = null, int page = 1)
        {
            TaiKhoan shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            DonHang order = GetAssignedOrderForShipper(id, shipper.TaiKhoanId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng được giao cho bạn.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (order.TrangThai != OrderStatusConstants.DELIVERING)
            {
                TempData["ErrorMessage"] = "Chỉ có thể xác nhận giao thành công khi đơn đang giao.";
                return RedirectToAction("Detail", new { id, keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = order.TrangThai;

                    order.TrangThai = OrderStatusConstants.DELIVERED;
                    order.NgayHoanTat = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;

                    UpdatePaymentWhenDelivered(order);

                    AddOrderHistory(
                        order.DonHangId,
                        oldStatus,
                        OrderStatusConstants.DELIVERED,
                        string.IsNullOrWhiteSpace(ghiChu)
                            ? "Shipper xác nhận giao hàng thành công."
                            : ghiChu.Trim(),
                        shipper.TaiKhoanId);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đã xác nhận giao hàng thành công.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Xác nhận giao hàng thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Detail", new { id, keyword, status, page });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeliveryFailed(int id, string ghiChu = "", string keyword = "", int? status = null, int page = 1)
        {
            TaiKhoan shipper = GetCurrentAccount();

            if (shipper == null)
            {
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            DonHang order = GetAssignedOrderForShipper(id, shipper.TaiKhoanId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng được giao cho bạn.";
                return RedirectToAction("Index", new { keyword, status, page });
            }

            if (order.TrangThai != OrderStatusConstants.DELIVERING)
            {
                TempData["ErrorMessage"] = "Chỉ có thể xác nhận giao thất bại khi đơn đang giao.";
                return RedirectToAction("Detail", new { id, keyword, status, page });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = order.TrangThai;

                    order.TrangThai = OrderStatusConstants.DELIVERY_FAILED;
                    order.UpdatedAt = DateTime.Now;

                    RestoreStockForOrder(order);
                    UpdatePaymentWhenDeliveryFailed(order);

                    string note = string.IsNullOrWhiteSpace(ghiChu)
                        ? "Shipper xác nhận giao hàng thất bại."
                        : ghiChu.Trim();

                    if (NeedManualRefund(order))
                    {
                        note += " Đơn hàng đã thanh toán qua VNPay, cửa hàng cần xử lý hoàn tiền thủ công.";
                    }

                    AddOrderHistory(
                        order.DonHangId,
                        oldStatus,
                        OrderStatusConstants.DELIVERY_FAILED,
                        note,
                        shipper.TaiKhoanId);

                    db.SaveChanges();
                    transaction.Commit();

                    if (NeedManualRefund(order))
                    {
                        TempData["SuccessMessage"] = "Đã cập nhật giao thất bại. Đơn VNPay đã thanh toán, admin cần xử lý hoàn tiền thủ công.";
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Đã cập nhật trạng thái: Giao hàng thất bại.";
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Cập nhật giao thất bại không thành công: " + ex.Message;
                }
            }

            return RedirectToAction("Detail", new { id, keyword, status, page });
        }

        private AdminOrderIndexVm BuildIndexViewModel(int shipperId, string keyword, int? status, int page)
        {
            keyword = (keyword ?? string.Empty).Trim();

            if (page <= 0)
            {
                page = 1;
            }

            IQueryable<DonHang> query = GetOrderQuery()
                .Where(x => x.ShipperId == shipperId);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaDonHang.Contains(keyword) ||
                    x.HoTenNguoiNhan.Contains(keyword) ||
                    x.SoDienThoaiNguoiNhan.Contains(keyword) ||
                    x.DiaChiNhanHang.Contains(keyword));
            }

            if (status.HasValue && IsValidShipperOrderStatus(status.Value))
            {
                query = query.Where(x => x.TrangThai == status.Value);
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

            List<AdminOrderListItemVm> orders = query
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
                        x.TrangThai == OrderStatusConstants.DELIVERY_FAILED &&
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

            return new AdminOrderIndexVm
            {
                Keyword = keyword,
                StatusFilter = status,
                HeaderTitle = "Đơn hàng được giao cho tôi",
                StatusOptions = BuildShipperStatusOptions(status),
                Orders = orders,

                CurrentPage = page,
                PageSize = PAGE_SIZE,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        private AdminOrderDetailVm BuildDetailViewModel(DonHang order)
        {
            return new AdminOrderDetailVm
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
                CanRequireManualRefund = NeedManualRefund(order),
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

                Shippers = new List<SelectListItem>()
            };
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

        private DonHang GetAssignedOrderForShipper(int orderId, int shipperId)
        {
            return GetOrderQuery()
                .FirstOrDefault(x =>
                    x.DonHangId == orderId &&
                    x.ShipperId == shipperId);
        }

        private List<SelectListItem> BuildShipperStatusOptions(int? selectedStatus)
        {
            var statuses = new List<int>
            {
                OrderStatusConstants.ASSIGNED_TO_SHIPPER,
                OrderStatusConstants.DELIVERING,
                OrderStatusConstants.DELIVERED,
                OrderStatusConstants.DELIVERY_FAILED
            };

            return statuses.Select(x => new SelectListItem
            {
                Value = x.ToString(),
                Text = OrderStatusConstants.GetName(x),
                Selected = selectedStatus.HasValue && selectedStatus.Value == x
            }).ToList();
        }

        private bool IsValidShipperOrderStatus(int status)
        {
            return status == OrderStatusConstants.ASSIGNED_TO_SHIPPER ||
                   status == OrderStatusConstants.DELIVERING ||
                   status == OrderStatusConstants.DELIVERED ||
                   status == OrderStatusConstants.DELIVERY_FAILED;
        }

        private void AddOrderHistory(int donHangId, int trangThaiCu, int trangThaiMoi, string ghiChu, int taiKhoanId)
        {
            db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
            {
                DonHangId = donHangId,
                TrangThaiCu = trangThaiCu,
                TrangThaiMoi = trangThaiMoi,
                ThayDoiBoiId = taiKhoanId,
                GhiChu = string.IsNullOrWhiteSpace(ghiChu)
                    ? "Cập nhật trạng thái đơn hàng."
                    : ghiChu.Trim(),
                CreatedAt = DateTime.Now
            });
        }

        private void RestoreStockForOrder(DonHang order)
        {
            if (order == null || order.ChiTietDonHangs == null)
            {
                return;
            }

            foreach (var item in order.ChiTietDonHangs)
            {
                SanPham product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == item.SanPhamId);

                if (product != null)
                {
                    product.SoLuongTon += item.SoLuong;
                    product.UpdatedAt = DateTime.Now;
                }
            }
        }

        private void UpdatePaymentWhenDelivered(DonHang order)
        {
            if (order == null)
            {
                return;
            }

            if (string.Equals(order.PhuongThucThanhToan, PaymentConstants.COD, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(order.TrangThaiThanhToan, PaymentConstants.PAID, StringComparison.OrdinalIgnoreCase))
            {
                order.TrangThaiThanhToan = PaymentConstants.PAID;
                order.NgayThanhToan = DateTime.Now;
            }
        }

        private void UpdatePaymentWhenDeliveryFailed(DonHang order)
        {
            if (order == null)
            {
                return;
            }

            if (string.Equals(order.PhuongThucThanhToan, PaymentConstants.COD, StringComparison.OrdinalIgnoreCase))
            {
                order.TrangThaiThanhToan = PaymentConstants.FAILED;
                order.NgayThanhToan = null;
            }
        }

        private bool NeedManualRefund(DonHang order)
        {
            if (order == null)
            {
                return false;
            }

            return order.TrangThai == OrderStatusConstants.DELIVERY_FAILED &&
                   string.Equals(order.PhuongThucThanhToan, PaymentConstants.VNPAY, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(order.TrangThaiThanhToan, PaymentConstants.PAID, StringComparison.OrdinalIgnoreCase);
        }

        private TaiKhoan GetCurrentAccount()
        {
            TaiKhoan sessionAccount = Session["LoginInformation"] as TaiKhoan;

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