using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.Models.Payments;
using MatKinh.ViewModel;

namespace MatKinh.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int OrderStatusPending = 1;
        private const int OrderStatusCancelled = 8;
        private const int ProductStatusActive = 1;

        // GET: Checkout
        public ActionResult Index()
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                TempData["NotificationLogin"] = "Bạn cần đăng nhập trước khi thanh toán.";
                return RedirectToAction("LoginAccount", "Account");
            }

            var model = new PurchaseInformation
            {
                HoTenNguoiNhan = currentAccount.HoTen,
                SoDienThoaiNguoiNhan = currentAccount.SoDienThoai,
                Email = currentAccount.Email,
                DiaChiNhanHang = currentAccount.DiaChi
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(PurchaseInformation model)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                TempData["NotificationLogin"] = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.";
                return RedirectToAction("LoginAccount", "Account");
            }

            var cart = Session["ShoppingCart"] as List<Cart>;
            if (cart == null || !cart.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng của bạn đang trống, không thể thanh toán.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            List<int> productIds = cart.Select(x => x.SanPhamId).Distinct().ToList();

            var products = db.SanPhams
                .Where(x => productIds.Contains(x.SanPhamId))
                .ToList();

            if (products.Count != productIds.Count)
            {
                ModelState.AddModelError("", "Một hoặc nhiều sản phẩm không còn tồn tại.");
                return View(model);
            }

            foreach (var cartItem in cart)
            {
                var product = products.First(x => x.SanPhamId == cartItem.SanPhamId);

                if (product.TrangThai != ProductStatusActive)
                {
                    ModelState.AddModelError("", $"Sản phẩm '{product.TenSanPham}' hiện không khả dụng.");
                    return View(model);
                }

                if (cartItem.SoLuong <= 0)
                {
                    ModelState.AddModelError("", $"Số lượng của sản phẩm '{product.TenSanPham}' không hợp lệ.");
                    return View(model);
                }

                if (product.SoLuongTon < cartItem.SoLuong)
                {
                    ModelState.AddModelError("", $"Sản phẩm '{product.TenSanPham}' không đủ số lượng tồn kho.");
                    return View(model);
                }
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    TaiKhoan accountInDb = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == currentAccount.TaiKhoanId);
                    if (accountInDb == null)
                    {
                        ModelState.AddModelError("", "Không tìm thấy tài khoản trong hệ thống.");
                        return View(model);
                    }

                    KhachHang customer = GetOrCreateCustomer(accountInDb, model);
                    Session["KhachHangId"] = customer.KhachHangId;

                    decimal tongTienHang = 0m;
                    decimal tongGiamGia = 0m;
                    decimal phiVanChuyen = CalculateShippingFee(cart, model);

                    bool isVnPay = string.Equals(
                          model.PhuongThucThanhToan,
                          "Payment_Auto",
                    StringComparison.OrdinalIgnoreCase);

                    var order = new DonHang
                    {
                        MaDonHang = GenerateOrderCode(),
                        KhachHangId = customer.KhachHangId,
                        HoTenNguoiNhan = model.HoTenNguoiNhan.Trim(),
                        SoDienThoaiNguoiNhan = model.SoDienThoaiNguoiNhan.Trim(),
                        DiaChiNhanHang = model.DiaChiNhanHang.Trim(),
                        TongTienHang = 0m,
                        PhiVanChuyen = phiVanChuyen,
                        GiamGia = 0m,
                        TongThanhToan = 0m,
                        TrangThai = OrderStatusPending,
                        GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim(),
                        CreatedById = accountInDb.TaiKhoanId,
                        NgayDat = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = null,
                        NgayHuy = null,

                        PhuongThucThanhToan = isVnPay ? PaymentConstants.VNPAY : PaymentConstants.COD,
                        TrangThaiThanhToan = PaymentConstants.PENDING,
                        MaGiaoDichThanhToan = null,
                        NgayThanhToan = null
                    };

                    db.DonHangs.Add(order);
                    db.SaveChanges();

                    foreach (var cartItem in cart)
                    {
                        var product = products.First(x => x.SanPhamId == cartItem.SanPhamId);

                        decimal donGia = product.GiaBan;
                        decimal giamGia = cartItem.GiamGia;
                        int soLuong = cartItem.SoLuong;
                        decimal thanhTien = (donGia - giamGia) * soLuong;

                        if (thanhTien < 0)
                        {
                            throw new InvalidOperationException($"Thành tiền của sản phẩm '{product.TenSanPham}' không hợp lệ.");
                        }

                        var detail = new ChiTietDonHang
                        {
                            DonHangId = order.DonHangId,
                            SanPhamId = product.SanPhamId,
                            TenSanPhamSnapshot = product.TenSanPham,
                            DonGiaSnapshot = donGia,
                            SoLuong = soLuong,
                            GiamGiaSnapshot = giamGia,
                            ThanhTien = thanhTien
                        };

                        db.ChiTietDonHangs.Add(detail);

                        UserBehaviorLogger.Log(
                            db,
                            Session,
                            product.SanPhamId,
                            UserBehaviorConstants.PURCHASE,
                            UserBehaviorConstants.PURCHASE_WEIGHT,
                            "CHECKOUT",
                            "DonHangId=" + order.DonHangId
                        );

                        tongTienHang += donGia * soLuong;
                        tongGiamGia += giamGia * soLuong;

                        product.SoLuongTon -= soLuong;
                        product.UpdatedAt = DateTime.Now;
                    }

                    order.TongTienHang = tongTienHang;
                    order.GiamGia = tongGiamGia;
                    order.TongThanhToan = tongTienHang + phiVanChuyen - tongGiamGia;

                    if (order.TongThanhToan < 0)
                    {
                        throw new InvalidOperationException("Tổng thanh toán không hợp lệ.");
                    }

                    db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
                    {
                        DonHangId = order.DonHangId,
                        TrangThaiCu = null,
                        TrangThaiMoi = OrderStatusPending,
                        ThayDoiBoiId = accountInDb.TaiKhoanId,
                        GhiChu = "Khởi tạo đơn hàng từ website.",
                        CreatedAt = DateTime.Now
                    });

                    accountInDb.HoTen = string.IsNullOrWhiteSpace(model.HoTenNguoiNhan)
                        ? accountInDb.HoTen
                        : model.HoTenNguoiNhan.Trim();
                    accountInDb.SoDienThoai = model.SoDienThoaiNguoiNhan.Trim();
                    accountInDb.Email = model.Email.Trim();
                    accountInDb.DiaChi = model.DiaChiNhanHang.Trim();
                    accountInDb.UpdatedAt = DateTime.Now;

                    db.SaveChanges();
                    transaction.Commit();

                    Session["LastCreatedOrderId"] = order.DonHangId;
                    Session["LastCreatedOrderCode"] = order.MaDonHang;

                    if (string.Equals(model.PhuongThucThanhToan, "Payment_Auto", StringComparison.OrdinalIgnoreCase))
                    {
                        string paymentUrl = BuildVnPayUrl(order);
                        return Redirect(paymentUrl);
                    }

                    Session["ShoppingCart"] = null;
                    return RedirectToAction("CheckoutSuccess", new { id = order.DonHangId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Tạo đơn hàng thất bại: " + ex.Message);
                    return View(model);
                }
            }
        }

        private void RestoreStockForOrder(DonHang order)
        {
            if (order == null)
            {
                return;
            }

            var details = db.ChiTietDonHangs
                .Where(x => x.DonHangId == order.DonHangId)
                .ToList();

            foreach (var item in details)
            {
                var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == item.SanPhamId);
                if (product != null)
                {
                    product.SoLuongTon += item.SoLuong;
                    product.UpdatedAt = DateTime.Now;
                }
            }
        }

        public ActionResult CheckoutSuccess(int? id)
        {
            int orderId = id ?? (Session["LastCreatedOrderId"] as int?) ?? 0;
            if (orderId <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var order = db.DonHangs.FirstOrDefault(x => x.DonHangId == orderId);
            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var customer = db.KhachHangs.FirstOrDefault(x => x.KhachHangId == order.KhachHangId);
            var orderDetails = db.ChiTietDonHangs
                .Where(x => x.DonHangId == order.DonHangId)
                .ToList();

            Session["ShoppingCart"] = null;
            ViewData["Order"] = order;
            ViewData["CustomerOrder"] = customer;
            ViewData["OrderDetails"] = orderDetails;

            return View();
        }

        public ActionResult VnPayReturn()
        {
            string result = ValidateVnPayResponse();
            string maDonHang = Request.QueryString["vnp_TxnRef"];
            string vnpTransactionNo = Request.QueryString["vnp_TransactionNo"];

            if (string.IsNullOrWhiteSpace(maDonHang))
            {
                TempData["PaymentError"] = "Không nhận được mã đơn hàng từ cổng thanh toán.";
                return RedirectToAction("Index", "ShoppingCart");
            }

            var order = db.DonHangs.FirstOrDefault(x => x.MaDonHang == maDonHang);
            if (order == null)
            {
                TempData["PaymentError"] = "Không tìm thấy đơn hàng tương ứng.";
                return RedirectToAction("Index", "ShoppingCart");
            }

            if (result == "ok")
            {
                order.TrangThaiThanhToan = PaymentConstants.PAID;
                order.MaGiaoDichThanhToan = string.IsNullOrWhiteSpace(vnpTransactionNo) ? null : vnpTransactionNo;
                order.NgayThanhToan = DateTime.Now;
                order.UpdatedAt = DateTime.Now;

                db.SaveChanges();

                Session["ShoppingCart"] = null;
                Session["LastCreatedOrderId"] = order.DonHangId;
                Session["LastCreatedOrderCode"] = order.MaDonHang;

                return RedirectToAction("CheckoutSuccess", new { id = order.DonHangId });
            }

            if (order.TrangThai != OrderStatusCancelled)
            {
                int? actorId = GetCurrentAccount()?.TaiKhoanId;
                int oldStatus = order.TrangThai;

                order.TrangThaiThanhToan = PaymentConstants.FAILED;
                order.TrangThai = OrderStatusCancelled;
                order.NgayHuy = DateTime.Now;
                order.UpdatedAt = DateTime.Now;

                RestoreStockForOrder(order);

                db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
                {
                    DonHangId = order.DonHangId,
                    TrangThaiCu = oldStatus,
                    TrangThaiMoi = OrderStatusCancelled,
                    ThayDoiBoiId = actorId,
                    GhiChu = "Thanh toán VNPay thất bại hoặc bị hủy.",
                    CreatedAt = DateTime.Now
                });

                db.SaveChanges();
            }

            TempData["PaymentError"] = "Thanh toán không thành công hoặc đã bị hủy.";
            return RedirectToAction("Index", "ShoppingCart");
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

            return db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == account.TaiKhoanId);
        }

        private KhachHang GetOrCreateCustomer(TaiKhoan account, PurchaseInformation model)
        {
            string email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
            string soDienThoai = model.SoDienThoaiNguoiNhan.Trim();

            var customer = db.KhachHangs.FirstOrDefault(x => x.SoDienThoai == soDienThoai);

            if (customer == null && !string.IsNullOrWhiteSpace(email))
            {
                customer = db.KhachHangs.FirstOrDefault(x => x.Email == email);
            }

            if (customer == null)
            {
                customer = new KhachHang
                {
                    MaKhachHang = GenerateCustomerCode(),
                    HoTen = model.HoTenNguoiNhan.Trim(),
                    Email = email,
                    SoDienThoai = soDienThoai,
                    GioiTinh = account.GioiTinh,
                    NgaySinh = account.NgaySinh,
                    DiaChi = model.DiaChiNhanHang.Trim(),
                    GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };

                db.KhachHangs.Add(customer);
                db.SaveChanges();
            }
            else
            {
                customer.HoTen = model.HoTenNguoiNhan.Trim();
                customer.Email = email;
                customer.SoDienThoai = soDienThoai;
                customer.DiaChi = model.DiaChiNhanHang.Trim();
                customer.GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? customer.GhiChu : model.GhiChu.Trim();
                customer.UpdatedAt = DateTime.Now;
                db.SaveChanges();
            }

            return customer;
        }

        private decimal CalculateShippingFee(List<Cart> cart, PurchaseInformation model)
        {
            return 30000m;
        }

        private string BuildVnPayUrl(DonHang order)
        {
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"];
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

            var vnNow = DateTime.UtcNow.AddHours(7);
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(order.TongThanhToan * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", vnNow.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_ExpireDate", vnNow.AddMinutes(15).ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + order.MaDonHang);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", order.MaDonHang);

            return vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
        }

        private string ValidateVnPayResponse()
        {
            if (Request.QueryString.Count <= 0)
            {
                return null;
            }

            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
            var vnpayData = Request.QueryString;
            var vnpay = new VnPayLibrary();

            foreach (string key in vnpayData)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, vnpayData[key]);
                }
            }

            string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
            string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
            if (checkSignature && vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
            {
                return "ok";
            }

            return null;
        }

        private string GenerateOrderCode()
        {
            string code;
            do
            {
                code = "DH" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
            while (db.DonHangs.Any(x => x.MaDonHang == code));

            return code;
        }

        private string GenerateCustomerCode()
        {
            string code;
            do
            {
                code = "KH" + DateTime.Now.ToString("yyyyMMddHHmmssfff");
            }
            while (db.KhachHangs.Any(x => x.MaKhachHang == code));

            return code;
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