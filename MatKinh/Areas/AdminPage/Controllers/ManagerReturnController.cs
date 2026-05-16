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
    public class ManagerReturnController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();
        private const int PAGE_SIZE = 10;

        // ========================= DANH SÁCH =========================

        [HttpGet]
        public ActionResult Index(string keyword = "", int? status = null, int page = 1)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
                return RedirectToAction("LoginAccount", "Account", new { area = "" });

            keyword = (keyword ?? string.Empty).Trim();
            if (page <= 0) page = 1;

            var query = db.YeuCauTraHangs
                .Include(x => x.DonHang)
                .Include(x => x.KhachHang)
                .Include(x => x.TaiKhoan1)   // Shipper
                .AsQueryable();

            // Shipper chỉ thấy yêu cầu được gán cho mình
            if (IsShipper(currentUser))
            {
                query = query.Where(x => x.ShipperId == currentUser.TaiKhoanId);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.MaYeuCau.Contains(keyword) ||
                    x.DonHang.MaDonHang.Contains(keyword) ||
                    x.KhachHang.HoTen.Contains(keyword));
            }

            if (status.HasValue)
            {
                query = query.Where(x => x.TrangThai == status.Value);
            }

            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / PAGE_SIZE);
            if (totalPages <= 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var returns = query
                .OrderByDescending(x => x.NgayYeuCau)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .Select(x => new AdminReturnItemVm
                {
                    YeuCauId = x.YeuCauId,
                    MaYeuCau = x.MaYeuCau,
                    MaDonHang = x.DonHang.MaDonHang,
                    TenKhachHang = x.KhachHang.HoTen,
                    LyDo = x.LyDo,
                    TrangThai = x.TrangThai,
                    TrangThaiText = "",
                    NgayYeuCau = x.NgayYeuCau,
                    ShipperName = x.TaiKhoan1 != null ? x.TaiKhoan1.HoTen : ""  // TaiKhoan1 = Shipper
                })
                .ToList();

            foreach (var item in returns)
            {
                item.TrangThaiText = ReturnStatusConstants.GetName(item.TrangThai);
            }

            var statusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = ReturnStatusConstants.PENDING.ToString(), Text = ReturnStatusConstants.GetName(ReturnStatusConstants.PENDING), Selected = status == ReturnStatusConstants.PENDING },
                new SelectListItem { Value = ReturnStatusConstants.APPROVED.ToString(), Text = ReturnStatusConstants.GetName(ReturnStatusConstants.APPROVED), Selected = status == ReturnStatusConstants.APPROVED },
                new SelectListItem { Value = ReturnStatusConstants.SHIPPER_PICKING.ToString(), Text = ReturnStatusConstants.GetName(ReturnStatusConstants.SHIPPER_PICKING), Selected = status == ReturnStatusConstants.SHIPPER_PICKING },
                new SelectListItem { Value = ReturnStatusConstants.RECEIVED.ToString(), Text = ReturnStatusConstants.GetName(ReturnStatusConstants.RECEIVED), Selected = status == ReturnStatusConstants.RECEIVED },
                new SelectListItem { Value = ReturnStatusConstants.REJECTED.ToString(), Text = ReturnStatusConstants.GetName(ReturnStatusConstants.REJECTED), Selected = status == ReturnStatusConstants.REJECTED }
            };

            var model = new AdminReturnIndexVm
            {
                Returns = returns,
                Keyword = keyword,
                StatusFilter = status,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = PAGE_SIZE,
                StatusOptions = statusOptions
            };

            return View(model);
        }

        // ========================= CHI TIẾT =========================

        [HttpGet]
        public ActionResult Detail(int id)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
                return RedirectToAction("LoginAccount", "Account", new { area = "" });

            var yeuCau = db.YeuCauTraHangs
                .Include(x => x.DonHang)
                .Include(x => x.KhachHang)
                .Include(x => x.TaiKhoan1)   // Shipper
                .Include(x => x.TaiKhoan)    // DuyetBoi
                .Include(x => x.ChiTietTraHangs.Select(ct => ct.ChiTietDonHang.SanPham))
                .FirstOrDefault(x => x.YeuCauId == id);

            if (yeuCau == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction("Index");
            }

            // Shipper chỉ xem yêu cầu được gán cho mình
            if (IsShipper(currentUser) && yeuCau.ShipperId != currentUser.TaiKhoanId)
            {
                return Redirect("~/Error/Index");
            }

            var shippers = db.TaiKhoans
                .Include(x => x.VaiTro)
                .Where(x => x.IsActive && x.VaiTro.MaVaiTro == RoleConstants.SHIPPER)
                .OrderBy(x => x.HoTen)
                .Select(x => new SelectListItem
                {
                    Value = x.TaiKhoanId.ToString(),
                    Text = x.HoTen + " - " + x.TenDangNhap
                })
                .ToList();

            var model = new AdminReturnDetailVm
            {
                YeuCauId = yeuCau.YeuCauId,
                MaYeuCau = yeuCau.MaYeuCau,
                DonHangId = yeuCau.DonHangId,
                MaDonHang = yeuCau.DonHang.MaDonHang,
                TenKhachHang = yeuCau.KhachHang.HoTen,
                LyDo = yeuCau.LyDo,
                GhiChuKhachHang = yeuCau.GhiChuKhachHang,
                TrangThai = yeuCau.TrangThai,
                TrangThaiText = ReturnStatusConstants.GetName(yeuCau.TrangThai),
                GhiChuAdmin = yeuCau.GhiChuAdmin,
                ShipperId = yeuCau.ShipperId,
                ShipperName = yeuCau.TaiKhoan1 != null ? yeuCau.TaiKhoan1.HoTen : "",   // TaiKhoan1 = Shipper
                DuyetBoiName = yeuCau.TaiKhoan != null ? yeuCau.TaiKhoan.HoTen : "",    // TaiKhoan  = DuyetBoi
                NgayYeuCau = yeuCau.NgayYeuCau,
                NgayDuyet = yeuCau.NgayDuyet,
                NgayShipperLay = yeuCau.NgayShipperLay,
                NgayNhanVe = yeuCau.NgayNhanVe,
                IsVnPayPaid =
                    string.Equals(yeuCau.DonHang.PhuongThucThanhToan, PaymentConstants.VNPAY, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(yeuCau.DonHang.TrangThaiThanhToan, PaymentConstants.PAID, StringComparison.OrdinalIgnoreCase),
                Items = yeuCau.ChiTietTraHangs.Select(ct => new AdminReturnItemDetailVm
                {
                    ChiTietTraHangId = ct.ChiTietTraHangId,
                    TenSanPham = ct.ChiTietDonHang.TenSanPhamSnapshot,
                    HinhAnh = ct.ChiTietDonHang.SanPham != null ? ct.ChiTietDonHang.SanPham.HinhAnhChinh : "",
                    SoLuongTra = ct.SoLuongTra,
                    LyDoChiTiet = ct.LyDoChiTiet
                }).ToList(),
                Shippers = shippers
            };

            return View(model);
        }

        // ========================= CẬP NHẬT TRẠNG THÁI =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(AdminReturnActionVm model)
        {
            var currentUser = GetCurrentAccount();
            if (currentUser == null)
                return RedirectToAction("LoginAccount", "Account", new { area = "" });

            var yeuCau = db.YeuCauTraHangs
                .Include(x => x.DonHang.ChiTietDonHangs)
                .FirstOrDefault(x => x.YeuCauId == model.YeuCauId);

            if (yeuCau == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy yêu cầu trả hàng.";
                return RedirectToAction("Index");
            }

            // Shipper chỉ được cập nhật yêu cầu gán cho mình
            if (IsShipper(currentUser) && yeuCau.ShipperId != currentUser.TaiKhoanId)
            {
                return Redirect("~/Error/Index");
            }

            // Kiểm tra quyền theo role
            string error;
            if (!CanUpdateReturnStatus(currentUser, yeuCau, model.TrangThaiMoi, out error))
            {
                TempData["ErrorMessage"] = error;
                return RedirectToAction("Detail", new { id = model.YeuCauId });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = yeuCau.TrangThai;
                    yeuCau.TrangThai = model.TrangThaiMoi;
                    yeuCau.UpdatedAt = DateTime.Now;

                    if (!string.IsNullOrWhiteSpace(model.GhiChuAdmin))
                    {
                        yeuCau.GhiChuAdmin = model.GhiChuAdmin.Trim();
                    }

                    // Duyệt: gán shipper nếu có
                    if (model.TrangThaiMoi == ReturnStatusConstants.APPROVED)
                    {
                        yeuCau.DuyetBoiId = currentUser.TaiKhoanId;
                        yeuCau.NgayDuyet = DateTime.Now;

                        if (model.ShipperId.HasValue && model.ShipperId.Value > 0)
                        {
                            yeuCau.ShipperId = model.ShipperId.Value;
                        }
                    }

                    // Shipper đang lấy
                    if (model.TrangThaiMoi == ReturnStatusConstants.SHIPPER_PICKING)
                    {
                        yeuCau.NgayShipperLay = DateTime.Now;
                    }

                    // Đã nhận hàng về: hoàn kho + cập nhật đơn hàng
                    if (model.TrangThaiMoi == ReturnStatusConstants.RECEIVED)
                    {
                        yeuCau.NgayNhanVe = DateTime.Now;

                        // Hoàn kho
                        var chiTietTraList = db.ChiTietTraHangs
                            .Include(x => x.ChiTietDonHang)
                            .Where(x => x.YeuCauId == yeuCau.YeuCauId)
                            .ToList();

                        foreach (var ct in chiTietTraList)
                        {
                            var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == ct.ChiTietDonHang.SanPhamId);
                            if (product != null)
                            {
                                product.SoLuongTon += ct.SoLuongTra;
                                product.UpdatedAt = DateTime.Now;
                            }
                        }

                        // Cập nhật trạng thái đơn hàng
                        var donHang = yeuCau.DonHang;
                        int oldOrderStatus = donHang.TrangThai;
                        donHang.TrangThai = OrderStatusConstants.RETURNED;
                        donHang.UpdatedAt = DateTime.Now;

                        db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
                        {
                            DonHangId = donHang.DonHangId,
                            TrangThaiCu = oldOrderStatus,
                            TrangThaiMoi = OrderStatusConstants.RETURNED,
                            ThayDoiBoiId = currentUser.TaiKhoanId,
                            GhiChu = "Đã nhận hàng trả về. Mã yêu cầu: " + yeuCau.MaYeuCau,
                            CreatedAt = DateTime.Now
                        });
                    }

                    // Từ chối: trả đơn hàng về DELIVERED
                    if (model.TrangThaiMoi == ReturnStatusConstants.REJECTED)
                    {
                        yeuCau.DuyetBoiId = currentUser.TaiKhoanId;
                        yeuCau.NgayDuyet = DateTime.Now;

                        var donHang = yeuCau.DonHang;
                        int oldOrderStatus = donHang.TrangThai;
                        donHang.TrangThai = OrderStatusConstants.DELIVERED;
                        donHang.UpdatedAt = DateTime.Now;

                        db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
                        {
                            DonHangId = donHang.DonHangId,
                            TrangThaiCu = oldOrderStatus,
                            TrangThaiMoi = OrderStatusConstants.DELIVERED,
                            ThayDoiBoiId = currentUser.TaiKhoanId,
                            GhiChu = "Yêu cầu trả hàng bị từ chối. Đơn hàng trở về trạng thái giao thành công.",
                            CreatedAt = DateTime.Now
                        });
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    string successMsg = "Cập nhật trạng thái yêu cầu trả hàng thành công.";
                    if (model.TrangThaiMoi == ReturnStatusConstants.RECEIVED && IsVnPayPaid(yeuCau.DonHang))
                    {
                        successMsg += " Đơn hàng đã thanh toán qua VNPay, vui lòng xử lý hoàn tiền trong 3–5 ngày làm việc.";
                    }

                    TempData["SuccessMessage"] = successMsg;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "Cập nhật thất bại: " + ex.Message;
                }
            }

            return RedirectToAction("Detail", new { id = model.YeuCauId });
        }

        // ========================= PRIVATE HELPERS =========================

        private bool CanUpdateReturnStatus(TaiKhoan user, YeuCauTraHang yeuCau, int newStatus, out string error)
        {
            error = string.Empty;

            // Trạng thái kết thúc
            if (yeuCau.TrangThai == ReturnStatusConstants.RECEIVED ||
                yeuCau.TrangThai == ReturnStatusConstants.REJECTED)
            {
                error = "Yêu cầu trả hàng đã ở trạng thái kết thúc.";
                return false;
            }

            if (IsShipper(user))
            {
                // Shipper chỉ được chuyển: APPROVED → SHIPPER_PICKING → RECEIVED
                if (yeuCau.TrangThai == ReturnStatusConstants.APPROVED && newStatus == ReturnStatusConstants.SHIPPER_PICKING)
                    return true;
                if (yeuCau.TrangThai == ReturnStatusConstants.SHIPPER_PICKING && newStatus == ReturnStatusConstants.RECEIVED)
                    return true;

                error = "Shipper không có quyền thực hiện thao tác này.";
                return false;
            }

            if (IsAdminOrStaff(user))
            {
                // Admin/Staff: PENDING → APPROVED hoặc REJECTED
                if (yeuCau.TrangThai == ReturnStatusConstants.PENDING &&
                    (newStatus == ReturnStatusConstants.APPROVED || newStatus == ReturnStatusConstants.REJECTED))
                    return true;

                // Admin/Staff: APPROVED → SHIPPER_PICKING (nếu chưa gán shipper)
                if (yeuCau.TrangThai == ReturnStatusConstants.APPROVED && newStatus == ReturnStatusConstants.SHIPPER_PICKING)
                    return true;

                error = "Không thể chuyển trạng thái theo luồng hiện tại.";
                return false;
            }

            error = "Bạn không có quyền cập nhật yêu cầu trả hàng.";
            return false;
        }

        private bool IsVnPayPaid(DonHang order)
        {
            return order != null &&
                   string.Equals(order.PhuongThucThanhToan, PaymentConstants.VNPAY, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(order.TrangThaiThanhToan, PaymentConstants.PAID, StringComparison.OrdinalIgnoreCase);
        }

        private TaiKhoan GetCurrentAccount()
        {
            var sessionAccount = Session["LoginInformation"] as TaiKhoan;
            if (sessionAccount == null) return null;

            return db.TaiKhoans
                .Include(x => x.VaiTro)
                .FirstOrDefault(x => x.TaiKhoanId == sessionAccount.TaiKhoanId && x.IsActive);
        }

        private bool IsAdmin(TaiKhoan a) =>
            a?.VaiTro != null && string.Equals(a.VaiTro.MaVaiTro, RoleConstants.ADMIN, StringComparison.OrdinalIgnoreCase);

        private bool IsStaff(TaiKhoan a) =>
            a?.VaiTro != null && string.Equals(a.VaiTro.MaVaiTro, RoleConstants.STAFF, StringComparison.OrdinalIgnoreCase);

        private bool IsShipper(TaiKhoan a) =>
            a?.VaiTro != null && string.Equals(a.VaiTro.MaVaiTro, RoleConstants.SHIPPER, StringComparison.OrdinalIgnoreCase);

        private bool IsAdminOrStaff(TaiKhoan a) => IsAdmin(a) || IsStaff(a);

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
