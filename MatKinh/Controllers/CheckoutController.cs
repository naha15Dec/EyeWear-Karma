using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
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

        private const int ProductStatusActive = 1;
        private const int MaxQuantityPerItem = 10;

        // GET: Checkout
        public ActionResult Index()
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                TempData["NotificationLogin"] = "Bạn cần đăng nhập trước khi thanh toán.";
                return RedirectToAction("LoginAccount", "Account");
            }

            int? khachHangId = GetCurrentKhachHangId();
            if (!khachHangId.HasValue)
            {
                TempData["CartError"] = "Không tìm thấy thông tin khách hàng để thanh toán.";
                return RedirectToAction("Cart", "ShoppingCart");
            }

            List<Cart> cart = GetCartByCustomer(khachHangId.Value);
            if (cart == null || !cart.Any())
            {
                TempData["CartError"] = "Giỏ hàng của bạn đang trống, không thể thanh toán.";
                return RedirectToAction("Cart", "ShoppingCart");
            }

            SetCheckoutCartViewData(cart);

            var model = new PurchaseInformation
            {
                HoTenNguoiNhan = currentAccount.HoTen,
                SoDienThoaiNguoiNhan = currentAccount.SoDienThoai,
                Email = currentAccount.Email,
                DiaChiNhanHang = currentAccount.DiaChi,
                PhuongThucThanhToan = "Payment_Before"
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

            int? khachHangId = GetCurrentKhachHangId();
            if (!khachHangId.HasValue)
            {
                TempData["CartError"] = "Không tìm thấy thông tin khách hàng để thanh toán.";
                return RedirectToAction("Cart", "ShoppingCart");
            }

            List<Cart> cart = GetCartByCustomer(khachHangId.Value);
            SetCheckoutCartViewData(cart);

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
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .Where(x => productIds.Contains(x.SanPhamId))
                .ToList();

            string validateError = ValidateCartBeforeCheckout(cart, products, productIds);
            if (!string.IsNullOrWhiteSpace(validateError))
            {
                ModelState.AddModelError("", validateError);
                SetCheckoutCartViewData(cart);
                return View(model);
            }

            bool isVnPay = string.Equals(
                model.PhuongThucThanhToan,
                "Payment_Auto",
                StringComparison.OrdinalIgnoreCase);

            if (isVnPay)
            {
                try
                {
                    decimal tongTienHang;
                    decimal tongGiamGia;
                    decimal phiVanChuyen;
                    decimal tongThanhToan;

                    CalculateOrderAmount(
                        cart,
                        products,
                        model,
                        out tongTienHang,
                        out tongGiamGia,
                        out phiVanChuyen,
                        out tongThanhToan);

                    if (tongThanhToan <= 0)
                    {
                        ModelState.AddModelError("", "Tổng thanh toán không hợp lệ.");
                        SetCheckoutCartViewData(cart);
                        return View(model);
                    }

                    string pendingOrderCode = GenerateOrderCode();

                    Session["PendingVnPayOrderCode"] = pendingOrderCode;
                    Session["PendingVnPayPurchaseInformation"] = model;
                    Session["PendingVnPayCart"] = CloneCart(cart);
                    Session["PendingVnPayAmount"] = tongThanhToan;
                    Session["PendingVnPayKhachHangId"] = khachHangId.Value;

                    string paymentUrl = BuildVnPayUrl(pendingOrderCode, tongThanhToan);
                    return Redirect(paymentUrl);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Không thể khởi tạo thanh toán VNPAY: " + ex.Message);
                    SetCheckoutCartViewData(cart);
                    return View(model);
                }
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    TaiKhoan accountInDb = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == currentAccount.TaiKhoanId && x.IsActive);
                    if (accountInDb == null)
                    {
                        ModelState.AddModelError("", "Không tìm thấy tài khoản trong hệ thống.");
                        SetCheckoutCartViewData(cart);
                        return View(model);
                    }

                    DonHang order = CreateOrderFromCart(
                        accountInDb,
                        model,
                        cart,
                        products,
                        PaymentConstants.COD,
                        PaymentConstants.PENDING,
                        null,
                        null,
                        "Khởi tạo đơn hàng COD từ website.");

                    ClearDbCart(khachHangId.Value);

                    db.SaveChanges();
                    transaction.Commit();

                    Session["LastCreatedOrderId"] = order.DonHangId;
                    Session["LastCreatedOrderCode"] = order.MaDonHang;

                    return RedirectToAction("CheckoutSuccess", new { id = order.DonHangId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Tạo đơn hàng thất bại: " + ex.Message);
                    SetCheckoutCartViewData(cart);
                    return View(model);
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
                ClearPendingVnPaySession();
                TempData["PaymentError"] = "Không nhận được mã thanh toán từ VNPAY. Đơn hàng chưa được tạo.";
                return RedirectToAction("Index", "Checkout");
            }

            string pendingOrderCode = Session["PendingVnPayOrderCode"] as string;
            PurchaseInformation model = Session["PendingVnPayPurchaseInformation"] as PurchaseInformation;
            List<Cart> cart = Session["PendingVnPayCart"] as List<Cart>;

            int? pendingKhachHangId = null;
            if (Session["PendingVnPayKhachHangId"] != null)
            {
                try
                {
                    pendingKhachHangId = Convert.ToInt32(Session["PendingVnPayKhachHangId"]);
                }
                catch
                {
                    pendingKhachHangId = null;
                }
            }

            if (!string.Equals(maDonHang, pendingOrderCode, StringComparison.OrdinalIgnoreCase))
            {
                ClearPendingVnPaySession();
                TempData["PaymentError"] = "Mã thanh toán không khớp với phiên thanh toán hiện tại. Đơn hàng chưa được tạo.";
                return RedirectToAction("Index", "Checkout");
            }

            if (result != "ok")
            {
                ClearPendingVnPaySession();
                TempData["PaymentError"] = "Thanh toán VNPAY không thành công hoặc đã bị hủy. Đơn hàng chưa được tạo.";
                return RedirectToAction("Index", "Checkout");
            }

            if (model == null || cart == null || !cart.Any())
            {
                ClearPendingVnPaySession();
                TempData["PaymentError"] = "Phiên thanh toán đã hết hạn hoặc giỏ hàng không còn dữ liệu. Đơn hàng chưa được tạo.";
                return RedirectToAction("Index", "Checkout");
            }

            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                ClearPendingVnPaySession();
                TempData["NotificationLogin"] = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.";
                return RedirectToAction("LoginAccount", "Account");
            }

            if (!pendingKhachHangId.HasValue)
            {
                pendingKhachHangId = GetCurrentKhachHangId();
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    TaiKhoan accountInDb = db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == currentAccount.TaiKhoanId && x.IsActive);
                    if (accountInDb == null)
                    {
                        throw new InvalidOperationException("Không tìm thấy tài khoản trong hệ thống.");
                    }

                    List<int> productIds = cart.Select(x => x.SanPhamId).Distinct().ToList();

                    var products = db.SanPhams
                        .Include(x => x.LoaiSanPham)
                        .Include(x => x.ThuongHieu)
                        .Where(x => productIds.Contains(x.SanPhamId))
                        .ToList();

                    string validateError = ValidateCartBeforeCheckout(cart, products, productIds);
                    if (!string.IsNullOrWhiteSpace(validateError))
                    {
                        throw new InvalidOperationException(validateError);
                    }

                    DonHang order = CreateOrderFromCart(
                        accountInDb,
                        model,
                        cart,
                        products,
                        PaymentConstants.VNPAY,
                        PaymentConstants.PAID,
                        string.IsNullOrWhiteSpace(vnpTransactionNo) ? null : vnpTransactionNo,
                        DateTime.Now,
                        "Khởi tạo đơn hàng sau khi thanh toán VNPAY thành công.");

                    if (pendingKhachHangId.HasValue)
                    {
                        ClearDbCart(pendingKhachHangId.Value);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    Session["LastCreatedOrderId"] = order.DonHangId;
                    Session["LastCreatedOrderCode"] = order.MaDonHang;

                    ClearPendingVnPaySession();

                    return RedirectToAction("CheckoutSuccess", new { id = order.DonHangId });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    ClearPendingVnPaySession();

                    TempData["PaymentError"] = "Thanh toán VNPAY đã thành công nhưng tạo đơn hàng thất bại: " + ex.Message;
                    return RedirectToAction("Index", "Checkout");
                }
            }
        }

        private void SetCheckoutCartViewData(List<Cart> cart)
        {
            ViewData["CheckoutCart"] = cart ?? new List<Cart>();
        }

        private List<Cart> GetCartByCustomer(int khachHangId)
        {
            var rows = db.GioHangChiTiets
                .AsNoTracking()
                .Include(x => x.SanPham)
                .Include(x => x.SanPham.LoaiSanPham)
                .Include(x => x.SanPham.ThuongHieu)
                .Where(x => x.KhachHangId == khachHangId)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ToList();

            List<Cart> cart = new List<Cart>();

            foreach (var row in rows)
            {
                var product = row.SanPham;

                if (product == null)
                {
                    continue;
                }

                decimal donGia = GetOriginalPrice(product);
                decimal giamGia = GetDiscountAmount(product);

                cart.Add(new Cart
                {
                    SanPhamId = product.SanPhamId,
                    TenSanPham = product.TenSanPham,
                    HinhAnh = product.HinhAnhChinh,
                    DonGia = donGia,
                    GiamGia = giamGia,
                    SoLuong = row.SoLuong
                });
            }

            return cart;
        }

        private void ClearDbCart(int khachHangId)
        {
            var cartItems = db.GioHangChiTiets
                .Where(x => x.KhachHangId == khachHangId)
                .ToList();

            if (cartItems.Any())
            {
                db.GioHangChiTiets.RemoveRange(cartItems);
            }
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

            return db.TaiKhoans.FirstOrDefault(x => x.TaiKhoanId == account.TaiKhoanId && x.IsActive);
        }

        private int? GetCurrentKhachHangId()
        {
            if (Session["KhachHangId"] != null)
            {
                try
                {
                    return Convert.ToInt32(Session["KhachHangId"]);
                }
                catch
                {
                    return null;
                }
            }

            var account = GetCurrentAccount();
            if (account == null)
            {
                return null;
            }

            var customer = db.KhachHangs.FirstOrDefault(x =>
                x.IsActive &&
                (
                    x.Email == account.Email ||
                    x.SoDienThoai == account.SoDienThoai
                ));

            if (customer == null)
            {
                return null;
            }

            Session["KhachHangId"] = customer.KhachHangId;
            return customer.KhachHangId;
        }

        private KhachHang GetOrCreateCustomer(TaiKhoan account, PurchaseInformation model)
        {
            string accountEmail = account != null && !string.IsNullOrWhiteSpace(account.Email)
                ? account.Email.Trim()
                : null;

            string accountPhone = account != null && !string.IsNullOrWhiteSpace(account.SoDienThoai)
                ? account.SoDienThoai.Trim()
                : null;

            string receiverName = !string.IsNullOrWhiteSpace(model.HoTenNguoiNhan)
                ? model.HoTenNguoiNhan.Trim()
                : account.HoTen;

            string receiverPhone = !string.IsNullOrWhiteSpace(model.SoDienThoaiNguoiNhan)
                ? model.SoDienThoaiNguoiNhan.Trim()
                : accountPhone;

            string receiverEmail = !string.IsNullOrWhiteSpace(model.Email)
                ? model.Email.Trim()
                : accountEmail;

            string receiverAddress = !string.IsNullOrWhiteSpace(model.DiaChiNhanHang)
                ? model.DiaChiNhanHang.Trim()
                : account.DiaChi;

            KhachHang customer = null;

            if (!string.IsNullOrWhiteSpace(accountEmail))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.Email == accountEmail);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(accountPhone))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.SoDienThoai == accountPhone);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(receiverPhone))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.SoDienThoai == receiverPhone);
            }

            if (customer == null && !string.IsNullOrWhiteSpace(receiverEmail))
            {
                customer = db.KhachHangs.FirstOrDefault(x =>
                    x.IsActive &&
                    x.Email == receiverEmail);
            }

            if (customer == null)
            {
                customer = new KhachHang
                {
                    MaKhachHang = GenerateCustomerCode(),
                    HoTen = receiverName,
                    Email = receiverEmail,
                    SoDienThoai = receiverPhone,
                    GioiTinh = account.GioiTinh,
                    NgaySinh = account.NgaySinh,
                    DiaChi = receiverAddress,
                    GhiChu = "Khách hàng được tạo khi thanh toán website.",
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null
                };

                db.KhachHangs.Add(customer);
                db.SaveChanges();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(receiverName))
                {
                    customer.HoTen = receiverName;
                }

                if (!string.IsNullOrWhiteSpace(receiverEmail))
                {
                    customer.Email = receiverEmail;
                }

                if (!string.IsNullOrWhiteSpace(receiverPhone))
                {
                    customer.SoDienThoai = receiverPhone;
                }

                if (!string.IsNullOrWhiteSpace(receiverAddress))
                {
                    customer.DiaChi = receiverAddress;
                }

                if (!string.IsNullOrWhiteSpace(model.GhiChu))
                {
                    customer.GhiChu = model.GhiChu.Trim();
                }

                customer.UpdatedAt = DateTime.Now;
                db.SaveChanges();
            }

            Session["KhachHangId"] = customer.KhachHangId;
            return customer;
        }

        private decimal CalculateShippingFee(List<Cart> cart, PurchaseInformation model)
        {
            return 30000m;
        }

        private bool IsProductAvailable(SanPham product)
        {
            if (product == null)
            {
                return false;
            }

            if (product.TrangThai != ProductStatusActive)
            {
                return false;
            }

            if (product.LoaiSanPham == null || !product.LoaiSanPham.IsActive)
            {
                return false;
            }

            if (product.ThuongHieu == null || !product.ThuongHieu.IsActive)
            {
                return false;
            }

            return true;
        }

        private decimal GetOriginalPrice(SanPham product)
        {
            if (product.GiaGoc > 0)
            {
                return product.GiaGoc;
            }

            return product.GiaBan;
        }

        private decimal GetDiscountAmount(SanPham product)
        {
            if (product.GiaGoc > product.GiaBan)
            {
                return product.GiaGoc - product.GiaBan;
            }

            return 0m;
        }

        private string BuildVnPayUrl(string maDonHang, decimal tongThanhToan)
        {
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"];
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

            if (string.IsNullOrWhiteSpace(vnp_Returnurl))
            {
                throw new InvalidOperationException("Thiếu cấu hình vnp_Returnurl.");
            }

            if (string.IsNullOrWhiteSpace(vnp_Url))
            {
                throw new InvalidOperationException("Thiếu cấu hình vnp_Url.");
            }

            if (string.IsNullOrWhiteSpace(vnp_TmnCode))
            {
                throw new InvalidOperationException("Thiếu cấu hình vnp_TmnCode.");
            }

            if (string.IsNullOrWhiteSpace(vnp_HashSecret))
            {
                throw new InvalidOperationException("Thiếu cấu hình vnp_HashSecret.");
            }

            if (tongThanhToan <= 0)
            {
                throw new InvalidOperationException("Số tiền thanh toán không hợp lệ.");
            }

            var vnNow = DateTime.UtcNow.AddHours(7);
            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((long)(tongThanhToan * 100)).ToString());
            vnpay.AddRequestData("vnp_CreateDate", vnNow.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_ExpireDate", vnNow.AddMinutes(15).ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang " + maDonHang);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", maDonHang);

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

        private string ValidateCartBeforeCheckout(List<Cart> cart, List<SanPham> products, List<int> productIds)
        {
            if (cart == null || !cart.Any())
            {
                return "Giỏ hàng của bạn đang trống, không thể thanh toán.";
            }

            if (products == null || products.Count != productIds.Count)
            {
                return "Một hoặc nhiều sản phẩm không còn tồn tại.";
            }

            foreach (var cartItem in cart)
            {
                var product = products.FirstOrDefault(x => x.SanPhamId == cartItem.SanPhamId);

                if (product == null)
                {
                    return "Một hoặc nhiều sản phẩm không còn tồn tại.";
                }

                if (!IsProductAvailable(product))
                {
                    return $"Sản phẩm '{product.TenSanPham}' hiện không khả dụng.";
                }

                if (cartItem.SoLuong <= 0)
                {
                    return $"Số lượng của sản phẩm '{product.TenSanPham}' không hợp lệ.";
                }

                if (cartItem.SoLuong > MaxQuantityPerItem)
                {
                    return $"Bạn chỉ có thể mua tối đa {MaxQuantityPerItem} sản phẩm cho mỗi mẫu kính.";
                }

                if (product.SoLuongTon < cartItem.SoLuong)
                {
                    return $"Sản phẩm '{product.TenSanPham}' không đủ số lượng tồn kho.";
                }
            }

            return null;
        }

        private void CalculateOrderAmount(
            List<Cart> cart,
            List<SanPham> products,
            PurchaseInformation model,
            out decimal tongTienHang,
            out decimal tongGiamGia,
            out decimal phiVanChuyen,
            out decimal tongThanhToan)
        {
            tongTienHang = 0m;
            tongGiamGia = 0m;
            phiVanChuyen = CalculateShippingFee(cart, model);

            foreach (var cartItem in cart)
            {
                var product = products.First(x => x.SanPhamId == cartItem.SanPhamId);

                decimal donGia = GetOriginalPrice(product);
                decimal giamGia = GetDiscountAmount(product);
                int soLuong = cartItem.SoLuong;

                tongTienHang += donGia * soLuong;
                tongGiamGia += giamGia * soLuong;
            }

            tongThanhToan = tongTienHang + phiVanChuyen - tongGiamGia;
        }

        private DonHang CreateOrderFromCart(
            TaiKhoan accountInDb,
            PurchaseInformation model,
            List<Cart> cart,
            List<SanPham> products,
            string paymentMethod,
            string paymentStatus,
            string transactionNo,
            DateTime? paidAt,
            string historyNote)
        {
            KhachHang customer = GetOrCreateCustomer(accountInDb, model);

            decimal tongTienHang = 0m;
            decimal tongGiamGia = 0m;
            decimal phiVanChuyen = CalculateShippingFee(cart, model);

            string orderCode = Session["PendingVnPayOrderCode"] as string;
            if (string.IsNullOrWhiteSpace(orderCode) || !string.Equals(paymentMethod, PaymentConstants.VNPAY, StringComparison.OrdinalIgnoreCase))
            {
                orderCode = GenerateOrderCode();
            }

            var order = new DonHang
            {
                MaDonHang = orderCode,
                KhachHangId = customer.KhachHangId,
                HoTenNguoiNhan = model.HoTenNguoiNhan.Trim(),
                SoDienThoaiNguoiNhan = model.SoDienThoaiNguoiNhan.Trim(),
                DiaChiNhanHang = model.DiaChiNhanHang.Trim(),
                TongTienHang = 0m,
                PhiVanChuyen = phiVanChuyen,
                GiamGia = 0m,
                TongThanhToan = 0m,
                TrangThai = OrderStatusConstants.PENDING,
                GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim(),
                CreatedById = accountInDb.TaiKhoanId,
                NgayDat = DateTime.Now,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                NgayHuy = null,

                PhuongThucThanhToan = paymentMethod,
                TrangThaiThanhToan = paymentStatus,
                MaGiaoDichThanhToan = transactionNo,
                NgayThanhToan = paidAt
            };

            db.DonHangs.Add(order);
            db.SaveChanges();

            foreach (var cartItem in cart)
            {
                var product = products.First(x => x.SanPhamId == cartItem.SanPhamId);

                decimal donGia = GetOriginalPrice(product);
                decimal giamGia = GetDiscountAmount(product);
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
                TrangThaiMoi = OrderStatusConstants.PENDING,
                ThayDoiBoiId = accountInDb.TaiKhoanId,
                GhiChu = historyNote,
                CreatedAt = DateTime.Now
            });

            accountInDb.HoTen = string.IsNullOrWhiteSpace(model.HoTenNguoiNhan)
                ? accountInDb.HoTen
                : model.HoTenNguoiNhan.Trim();

            accountInDb.SoDienThoai = model.SoDienThoaiNguoiNhan.Trim();
            accountInDb.Email = string.IsNullOrWhiteSpace(model.Email) ? accountInDb.Email : model.Email.Trim();
            accountInDb.DiaChi = model.DiaChiNhanHang.Trim();
            accountInDb.UpdatedAt = DateTime.Now;

            return order;
        }

        private List<Cart> CloneCart(List<Cart> cart)
        {
            if (cart == null)
            {
                return new List<Cart>();
            }

            return cart.Select(x => new Cart
            {
                SanPhamId = x.SanPhamId,
                TenSanPham = x.TenSanPham,
                HinhAnh = x.HinhAnh,
                DonGia = x.DonGia,
                GiamGia = x.GiamGia,
                SoLuong = x.SoLuong
            }).ToList();
        }

        private void ClearPendingVnPaySession()
        {
            Session["PendingVnPayOrderCode"] = null;
            Session["PendingVnPayPurchaseInformation"] = null;
            Session["PendingVnPayCart"] = null;
            Session["PendingVnPayAmount"] = null;
            Session["PendingVnPayKhachHangId"] = null;
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