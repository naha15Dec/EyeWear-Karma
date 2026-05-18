using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.USER)]
    public class ProfileUserController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int PageSize = 5;

        public ActionResult Profile(int page = 1)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            if (page <= 0)
            {
                page = 1;
            }

            IQueryable<DonHang> orderQuery = db.DonHangs
                .AsNoTracking()
                .Where(x => x.CreatedById == currentAccount.TaiKhoanId)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.DonHangId);

            BuildOrderPagination(orderQuery, page);

            return View(currentAccount);
        }

        /// <summary>
        /// Cập nhật thông tin tài khoản hiện tại.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateInformationAccount(string idAccount, UpdateAccount uda)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            if (uda == null)
            {
                TempData["ProfileError"] = "Dữ liệu cập nhật không hợp lệ.";
                return RedirectToAction("Profile");
            }

            var account = db.TaiKhoans.FirstOrDefault(x =>
                x.TaiKhoanId == currentAccount.TaiKhoanId &&
                x.IsActive);

            if (account == null)
            {
                TempData["ProfileError"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Profile");
            }

            string fullName = BuildFullName(uda.LastName, uda.FirstName);
            string oldEmail = account.Email;
            string oldPhone = account.SoDienThoai;

            account.HoTen = fullName;

            if (!string.IsNullOrWhiteSpace(uda.Address))
            {
                account.DiaChi = uda.Address.Trim();
            }

            if (!string.IsNullOrWhiteSpace(uda.Mobile))
            {
                account.SoDienThoai = uda.Mobile.Trim();
            }

            account.GioiTinh = ParseGender(uda.Sex);
            account.UpdatedAt = DateTime.Now;

            SyncCustomerFromAccount(account, oldEmail, oldPhone);

            db.SaveChanges();

            Session["LoginInformation"] = account;
            TempData["ProfileSuccess"] = "Cập nhật thông tin tài khoản thành công.";

            return RedirectToAction("Profile");
        }

        /// <summary>
        /// Đổi mật khẩu tài khoản hiện tại.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePasswordAccount(string idAccount, string passwdCurrent, UpdateAccount uda)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            var account = db.TaiKhoans.FirstOrDefault(x =>
                x.TaiKhoanId == currentAccount.TaiKhoanId &&
                x.IsActive);

            if (account == null)
            {
                TempData["ProfileError"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Profile");
            }

            string currentPasswordHash = HashPassword.SHA512HashPass(passwdCurrent ?? string.Empty);
            if (!string.Equals(account.MatKhauHash, currentPasswordHash, StringComparison.Ordinal))
            {
                TempData["PasswordError"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("Profile");
            }

            if (uda == null || string.IsNullOrWhiteSpace(uda.PassWord))
            {
                TempData["PasswordError"] = "Mật khẩu mới không hợp lệ.";
                return RedirectToAction("Profile");
            }

            if (!string.IsNullOrWhiteSpace(uda.ComfirmPassword) &&
                !string.Equals(uda.PassWord.Trim(), uda.ComfirmPassword.Trim(), StringComparison.Ordinal))
            {
                TempData["PasswordError"] = "Mật khẩu xác nhận không khớp.";
                return RedirectToAction("Profile");
            }

            if (uda.PassWord.Trim().Length < 6)
            {
                TempData["PasswordError"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction("Profile");
            }

            account.MatKhauHash = HashPassword.SHA512HashPass(uda.PassWord.Trim());
            account.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            Session["LoginInformation"] = account;
            TempData["PasswordSuccess"] = "Đổi mật khẩu thành công.";

            return RedirectToAction("Profile");
        }

        /// <summary>
        /// Chi tiết đơn hàng của user hiện tại.
        /// </summary>
        /// <summary>
        /// Chi tiết đơn hàng của user hiện tại.
        /// </summary>
        /// <summary>
        /// Chi tiết đơn hàng của user hiện tại.
        /// </summary>
        public ActionResult DetailPurchaseOrder(string maDonHang)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            if (string.IsNullOrWhiteSpace(maDonHang))
            {
                return RedirectToAction("Profile");
            }

            maDonHang = maDonHang.Trim();

            DonHang order = db.DonHangs
                .Include(x => x.KhachHang)
                .Include(x => x.LichSuTrangThaiDonHangs.Select(ls => ls.TaiKhoan))
                .FirstOrDefault(x =>
                    x.MaDonHang == maDonHang &&
                    x.CreatedById == currentAccount.TaiKhoanId);

            if (order == null)
            {
                TempData["ProfileError"] = "Không tìm thấy đơn hàng phù hợp.";
                return RedirectToAction("Profile");
            }

            List<ChiTietDonHang> details = db.ChiTietDonHangs
                .Include(x => x.SanPham)
                .Where(x => x.DonHangId == order.DonHangId)
                .OrderBy(x => x.ChiTietDonHangId)
                .ToList();

            List<int> detailIds = details
                .Select(x => x.ChiTietDonHangId)
                .ToList();

            Dictionary<int, DanhGiaSanPham> reviewMap = db.DanhGiaSanPhams
                .Where(x => detailIds.Contains(x.ChiTietDonHangId))
                .ToList()
                .ToDictionary(x => x.ChiTietDonHangId, x => x);

            YeuCauTraHang returnRequest = db.YeuCauTraHangs
                .Include(x => x.ChiTietTraHangs)
                .FirstOrDefault(x => x.DonHangId == order.DonHangId);

            bool hasActiveReturnRequest =
                returnRequest != null &&
                returnRequest.TrangThai != ReturnRequestStatusConstants.REJECTED &&
                returnRequest.TrangThai != ReturnRequestStatusConstants.CANCELLED;

            ViewData["listOfProductInOrder"] = details;
            ViewData["reviewMap"] = reviewMap;
            ViewData["returnRequest"] = returnRequest;

            ViewData["orderHistories"] = db.LichSuTrangThaiDonHangs
                .Include(x => x.TaiKhoan)
                .Where(x => x.DonHangId == order.DonHangId)
                .OrderBy(x => x.CreatedAt)
                .ToList();

            ViewBag.CanCancelOrder = CanCustomerCancelOrder(order);

            ViewBag.CanReviewOrder =
                order.TrangThai == OrderStatusConstants.DELIVERED &&
                !hasActiveReturnRequest;

            ViewBag.CanCreateReturnRequest =
                order.TrangThai == OrderStatusConstants.DELIVERED &&
                returnRequest == null;

            return View(order);
        }

        /// <summary>
        /// Khách hàng đánh giá sản phẩm trong đơn đã giao thành công.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitProductReview(
        string maDonHang,
        int chiTietDonHangId,
        int soSao,
        string noiDung)
            {
                TaiKhoan currentAccount = GetCurrentAccount();
                if (currentAccount == null)
                {
                    return RedirectToAction("LoginAccount", "Account");
                }

                if (string.IsNullOrWhiteSpace(maDonHang))
                {
                    TempData["ProfileError"] = "Mã đơn hàng không hợp lệ.";
                    return RedirectToAction("Profile");
                }

                maDonHang = maDonHang.Trim();

                DonHang order = db.DonHangs
                    .Include(x => x.KhachHang)
                    .FirstOrDefault(x =>
                        x.MaDonHang == maDonHang &&
                        x.CreatedById == currentAccount.TaiKhoanId);

                if (order == null)
                {
                    TempData["ProfileError"] = "Không tìm thấy đơn hàng phù hợp.";
                    return RedirectToAction("Profile");
                }

                if (order.TrangThai != OrderStatusConstants.DELIVERED)
                {
                    TempData["ProfileError"] = "Bạn chỉ có thể đánh giá khi đơn hàng đã giao thành công.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                bool hasActiveReturnRequest = db.YeuCauTraHangs.Any(x =>
                    x.DonHangId == order.DonHangId &&
                    x.TrangThai != ReturnRequestStatusConstants.REJECTED &&
                    x.TrangThai != ReturnRequestStatusConstants.CANCELLED);

                if (hasActiveReturnRequest)
                {
                    TempData["ProfileError"] = "Đơn hàng đang có yêu cầu trả hàng nên chưa thể đánh giá sản phẩm.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                ChiTietDonHang detail = db.ChiTietDonHangs
                    .FirstOrDefault(x =>
                        x.ChiTietDonHangId == chiTietDonHangId &&
                        x.DonHangId == order.DonHangId);

                if (detail == null)
                {
                    TempData["ProfileError"] = "Không tìm thấy sản phẩm cần đánh giá trong đơn hàng.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                bool alreadyReviewed = db.DanhGiaSanPhams.Any(x =>
                    x.ChiTietDonHangId == detail.ChiTietDonHangId);

                if (alreadyReviewed)
                {
                    TempData["ProfileError"] = "Sản phẩm này đã được gửi đánh giá trước đó.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                if (soSao < 1 || soSao > 5)
                {
                    TempData["ProfileError"] = "Số sao đánh giá không hợp lệ.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                noiDung = string.IsNullOrWhiteSpace(noiDung) ? null : noiDung.Trim();

                if (!string.IsNullOrWhiteSpace(noiDung) && noiDung.Length > 1000)
                {
                    TempData["ProfileError"] = "Nội dung đánh giá không được vượt quá 1000 ký tự.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                var review = new DanhGiaSanPham
                {
                    ChiTietDonHangId = detail.ChiTietDonHangId,
                    KhachHangId = order.KhachHangId,
                    SanPhamId = detail.SanPhamId,
                    SoSao = (byte)soSao,
                    NoiDung = noiDung,

                    // Khách gửi là hiển thị luôn
                    TrangThai = ReviewStatusConstants.APPROVED,

                    // Không cần admin duyệt
                    DuyetBoiId = null,
                    NgayDuyet = DateTime.Now,
                    PhanHoiAdmin = null,

                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };

                db.DanhGiaSanPhams.Add(review);
                db.SaveChanges();

                TempData["ProfileSuccess"] = "Đã gửi đánh giá thành công. Cảm ơn bạn đã chia sẻ trải nghiệm về sản phẩm.";
                return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
            }

        /// <summary>
        /// Khách hàng hủy đơn hàng của chính mình.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelPurchaseOrder(string maDonHang, string lyDoHuy)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            if (string.IsNullOrWhiteSpace(maDonHang))
            {
                TempData["ProfileError"] = "Mã đơn hàng không hợp lệ.";
                return RedirectToAction("Profile");
            }

            maDonHang = maDonHang.Trim();

            DonHang order = db.DonHangs
                .Include(x => x.ChiTietDonHangs)
                .FirstOrDefault(x =>
                    x.MaDonHang == maDonHang &&
                    x.CreatedById == currentAccount.TaiKhoanId);

            if (order == null)
            {
                TempData["ProfileError"] = "Không tìm thấy đơn hàng phù hợp.";
                return RedirectToAction("Profile");
            }

            if (!CanCustomerCancelOrder(order))
            {
                TempData["ProfileError"] = "Đơn hàng này không thể hủy ở trạng thái hiện tại. Nếu cần hỗ trợ, vui lòng liên hệ cửa hàng.";
                return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int oldStatus = order.TrangThai;

                    bool isPaidVnPayOrder =
                        string.Equals(order.PhuongThucThanhToan, PaymentConstants.VNPAY, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(order.TrangThaiThanhToan, PaymentConstants.PAID, StringComparison.OrdinalIgnoreCase);

                    order.TrangThai = OrderStatusConstants.CANCELLED;
                    order.NgayHuy = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;

                    // Không tự động set Refunded vì hệ thống chưa xử lý hoàn tiền thật qua VNPAY.
                    // Nếu là đơn VNPAY đã thanh toán, vẫn giữ trạng thái Paid và ghi chú để shop xử lý hoàn tiền.
                    RestoreStockForOrder(order);

                    string cancelNote = string.IsNullOrWhiteSpace(lyDoHuy)
                        ? "Khách hàng hủy đơn hàng."
                        : "Khách hàng hủy đơn hàng. Lý do: " + lyDoHuy.Trim();

                    if (isPaidVnPayOrder)
                    {
                        cancelNote += " Đơn hàng đã thanh toán qua VNPay, cửa hàng sẽ kiểm tra và xử lý hoàn tiền trong 3–5 ngày làm việc.";
                    }

                    db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
                    {
                        DonHangId = order.DonHangId,
                        TrangThaiCu = oldStatus,
                        TrangThaiMoi = OrderStatusConstants.CANCELLED,
                        ThayDoiBoiId = currentAccount.TaiKhoanId,
                        GhiChu = cancelNote,
                        CreatedAt = DateTime.Now
                    });

                    db.SaveChanges();
                    transaction.Commit();

                    if (isPaidVnPayOrder)
                    {
                        TempData["ProfileSuccess"] = "Đã hủy đơn hàng thành công. Đơn hàng đã thanh toán qua VNPay, cửa hàng sẽ xử lý hoàn tiền trong 3–5 ngày làm việc.";
                    }
                    else
                    {
                        TempData["ProfileSuccess"] = "Đã hủy đơn hàng thành công.";
                    }

                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ProfileError"] = "Hủy đơn hàng thất bại: " + ex.Message;
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitReturnRequest(
        string maDonHang,
        string lyDo,
        string ghiChuKhachHang,
        int[] selectedDetailIds,
        int[] soLuongTra)
            {
                TaiKhoan currentAccount = GetCurrentAccount();
                if (currentAccount == null)
                {
                    return RedirectToAction("LoginAccount", "Account");
                }

                if (string.IsNullOrWhiteSpace(maDonHang))
                {
                    TempData["ProfileError"] = "Mã đơn hàng không hợp lệ.";
                    return RedirectToAction("Profile");
                }

                maDonHang = maDonHang.Trim();

                DonHang order = db.DonHangs
                    .Include(x => x.ChiTietDonHangs)
                    .FirstOrDefault(x =>
                        x.MaDonHang == maDonHang &&
                        x.CreatedById == currentAccount.TaiKhoanId);

                if (order == null)
                {
                    TempData["ProfileError"] = "Không tìm thấy đơn hàng phù hợp.";
                    return RedirectToAction("Profile");
                }

                if (order.TrangThai != OrderStatusConstants.DELIVERED)
                {
                    TempData["ProfileError"] = "Chỉ có thể yêu cầu trả hàng khi đơn hàng đã giao thành công.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                bool hasReturnRequest = db.YeuCauTraHangs.Any(x => x.DonHangId == order.DonHangId);
                if (hasReturnRequest)
                {
                    TempData["ProfileError"] = "Đơn hàng này đã có yêu cầu trả hàng.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                if (string.IsNullOrWhiteSpace(lyDo))
                {
                    TempData["ProfileError"] = "Vui lòng nhập lý do trả hàng.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                if (selectedDetailIds == null || selectedDetailIds.Length == 0)
                {
                    TempData["ProfileError"] = "Vui lòng chọn ít nhất một sản phẩm cần trả.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                if (soLuongTra == null || soLuongTra.Length == 0)
                {
                    TempData["ProfileError"] = "Số lượng trả không hợp lệ.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                var selectedIds = selectedDetailIds.Distinct().ToList();

                var details = db.ChiTietDonHangs
                    .Where(x =>
                        x.DonHangId == order.DonHangId &&
                        selectedIds.Contains(x.ChiTietDonHangId))
                    .ToList();

                if (!details.Any())
                {
                    TempData["ProfileError"] = "Không tìm thấy sản phẩm hợp lệ để trả hàng.";
                    return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var request = new YeuCauTraHang
                        {
                            MaYeuCau = GenerateReturnRequestCode(),
                            DonHangId = order.DonHangId,
                            KhachHangId = order.KhachHangId,

                            LyDo = lyDo.Trim(),
                            GhiChuKhachHang = string.IsNullOrWhiteSpace(ghiChuKhachHang) ? null : ghiChuKhachHang.Trim(),

                            TrangThai = ReturnRequestStatusConstants.PENDING,
                            TrangThaiHoanTien = RefundStatusConstants.NOT_REFUNDED,

                            TongTienHoanDuKien = 0,
                            TongTienHoanThucTe = null,

                            NgayYeuCau = DateTime.Now,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = null
                        };

                        db.YeuCauTraHangs.Add(request);
                        db.SaveChanges();

                        decimal tongTienHoanDuKien = 0;

                        foreach (var detail in details)
                        {
                            int index = Array.IndexOf(selectedDetailIds, detail.ChiTietDonHangId);

                            if (index < 0 || index >= soLuongTra.Length)
                            {
                                continue;
                            }

                            int quantity = soLuongTra[index];

                            if (quantity <= 0)
                            {
                                continue;
                            }

                            if (quantity > detail.SoLuong)
                            {
                                quantity = detail.SoLuong;
                            }

                            decimal donGiaHoan = detail.DonGiaSnapshot - detail.GiamGiaSnapshot;

                            if (donGiaHoan < 0)
                            {
                                donGiaHoan = 0;
                            }

                            decimal thanhTienHoan = donGiaHoan * quantity;
                            tongTienHoanDuKien += thanhTienHoan;

                            db.ChiTietTraHangs.Add(new ChiTietTraHang
                            {
                                YeuCauTraHangId = request.YeuCauTraHangId,
                                ChiTietDonHangId = detail.ChiTietDonHangId,
                                SanPhamId = detail.SanPhamId,

                                TenSanPhamSnapshot = detail.TenSanPhamSnapshot,
                                SoLuongMua = detail.SoLuong,
                                SoLuongTra = quantity,

                                DonGiaSnapshot = detail.DonGiaSnapshot,
                                GiamGiaSnapshot = detail.GiamGiaSnapshot,
                                DonGiaHoan = donGiaHoan,
                                ThanhTienHoan = thanhTienHoan,

                                LyDoChiTiet = null,
                                CreatedAt = DateTime.Now
                            });
                        }

                        if (tongTienHoanDuKien <= 0)
                        {
                            transaction.Rollback();
                            TempData["ProfileError"] = "Vui lòng chọn sản phẩm và số lượng trả hợp lệ.";
                            return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                        }

                        request.TongTienHoanDuKien = tongTienHoanDuKien;

                        db.LichSuTrangThaiTraHangs.Add(new LichSuTrangThaiTraHang
                        {
                            YeuCauTraHangId = request.YeuCauTraHangId,
                            TrangThaiCu = null,
                            TrangThaiMoi = ReturnRequestStatusConstants.PENDING,
                            ThayDoiBoiId = currentAccount.TaiKhoanId,
                            GhiChu = "Khách hàng gửi yêu cầu trả hàng.",
                            CreatedAt = DateTime.Now
                        });

                        db.SaveChanges();
                        transaction.Commit();

                        TempData["ProfileSuccess"] = "Đã gửi yêu cầu trả hàng thành công. Cửa hàng sẽ kiểm tra và phản hồi sớm.";
                        return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["ProfileError"] = "Gửi yêu cầu trả hàng thất bại: " + ex.Message;
                        return RedirectToAction("DetailPurchaseOrder", new { maDonHang = order.MaDonHang });
                    }
                }
            }

        /// <summary>
        /// Chi tiết yêu cầu trả hàng của user hiện tại.
        /// </summary>
        public ActionResult DetailReturnRequest(string maYeuCau)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                return RedirectToAction("LoginAccount", "Account");
            }

            if (string.IsNullOrWhiteSpace(maYeuCau))
            {
                TempData["ProfileError"] = "Mã yêu cầu trả hàng không hợp lệ.";
                return RedirectToAction("Profile");
            }

            maYeuCau = maYeuCau.Trim();

            YeuCauTraHang request = db.YeuCauTraHangs
                .Include(x => x.DonHang)
                .Include(x => x.KhachHang)
                .Include(x => x.ChiTietTraHangs.Select(ct => ct.SanPham))
                .FirstOrDefault(x =>
                    x.MaYeuCau == maYeuCau &&
                    x.DonHang.CreatedById == currentAccount.TaiKhoanId);

            if (request == null)
            {
                TempData["ProfileError"] = "Không tìm thấy yêu cầu trả hàng phù hợp.";
                return RedirectToAction("Profile");
            }

            ViewData["returnHistories"] = db.LichSuTrangThaiTraHangs
                .Include(x => x.TaiKhoan)
                .Where(x => x.YeuCauTraHangId == request.YeuCauTraHangId)
                .OrderBy(x => x.CreatedAt)
                .ToList();

            return View(request);
        }

        private string GenerateReturnRequestCode()
        {
            string code;

            do
            {
                code = "TH" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
            while (db.YeuCauTraHangs.Any(x => x.MaYeuCau == code));

            return code;
        }
        private bool CanCustomerCancelOrder(DonHang order)
        {
            if (order == null)
            {
                return false;
            }

            // Khách chỉ được tự hủy khi đơn còn chờ xác nhận.
            // Các trạng thái đã xác nhận/chuẩn bị/giao hàng nên để shop hỗ trợ.
            return order.TrangThai == OrderStatusConstants.PENDING;
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

        private void BuildOrderPagination(IQueryable<DonHang> orderQuery, int page)
        {
            int totalCount = orderQuery.Count();
            int totalPages = (int)Math.Ceiling((double)totalCount / PageSize);

            if (totalPages <= 0)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            List<DonHang> orders = orderQuery
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewData["listOrderUser"] = orders;

            ViewBag.Page = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.NoOfPages = page >= 5
                ? ((page + 4 > totalPages) ? totalPages : (page + 4))
                : totalPages;

            ViewBag.DisplayPage = page < 5
                ? 0
                : (((page - 1) >= (totalPages - 5)) ? Math.Max(totalPages - 5, 0) : (page - 1));
        }

        private TaiKhoan GetCurrentAccount()
        {
            if (Session["LoginInformation"] == null)
            {
                return null;
            }

            var account = Session["LoginInformation"] as TaiKhoan;
            if (account == null)
            {
                return null;
            }

            return db.TaiKhoans.FirstOrDefault(x =>
                x.TaiKhoanId == account.TaiKhoanId &&
                x.IsActive);
        }

        private void SyncCustomerFromAccount(TaiKhoan account, string oldEmail, string oldPhone)
        {
            if (account == null)
            {
                return;
            }

            KhachHang customer = null;

            if (!string.IsNullOrWhiteSpace(oldEmail))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.Email == oldEmail);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(oldPhone))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.SoDienThoai == oldPhone);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(account.Email))
            {
                string email = account.Email.Trim();

                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.Email == email);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(account.SoDienThoai))
            {
                string phone = account.SoDienThoai.Trim();

                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.SoDienThoai == phone);
            }

            if (customer == null)
            {
                return;
            }

            customer.HoTen = account.HoTen;
            customer.Email = account.Email;
            customer.SoDienThoai = account.SoDienThoai;
            customer.GioiTinh = account.GioiTinh;
            customer.NgaySinh = account.NgaySinh;
            customer.DiaChi = account.DiaChi;
            customer.UpdatedAt = DateTime.Now;

            Session["KhachHangId"] = customer.KhachHangId;
        }

        private string BuildFullName(string lastName, string firstName)
        {
            string fullName = string.Format("{0} {1}",
                string.IsNullOrWhiteSpace(lastName) ? "" : lastName.Trim(),
                string.IsNullOrWhiteSpace(firstName) ? "" : firstName.Trim()).Trim();

            return string.IsNullOrWhiteSpace(fullName) ? "Người dùng" : fullName;
        }

        private bool? ParseGender(string sex)
        {
            if (string.IsNullOrWhiteSpace(sex))
            {
                return null;
            }

            string value = sex.Trim().ToLowerInvariant();

            if (value == "nam")
            {
                return true;
            }

            if (value == "nữ" || value == "nu")
            {
                return false;
            }

            return null;
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