using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
using BanGiay.Models.Payments;
using BanGiay.ViewModel;

namespace BanGiay.Controllers
{
    public class CheckoutController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();

        // Biến tạm cho flow thanh toán (demo/bài tập)
        static donHang tempOrder = null;
        static khachHang tempCustomer = null;
        static List<chiTietDonHang> tempDetailOrder = new List<chiTietDonHang>();
        static string idOrder = "";

        // GET: Checkout
        public ActionResult Index()
        {
            // Bắt buộc đăng nhập trước khi vào trang checkout
            var account = Session["LoginInformation"] as taiKhoanThanhVien;
            if (account == null)
            {
                TempData["NotificationLogin"] = "Bạn cần đăng nhập trước khi thanh toán.";
                return RedirectToAction("LoginAccount", "Account");
            }

            // Prefill thông tin từ tài khoản
            var model = new PurchaseInformation
            {
                NameCustomer = account.hoDem + " " + account.tenTV,
                MobileCustomer = account.soDT,
                Email = account.email,
                DeliveryAddress = account.diaChi
            };

            return View(model);
        }

        /// <summary>
        /// Nhận thông tin giao hàng + tạo đơn hàng
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(PurchaseInformation pi)
        {
            var accountSession = Session["LoginInformation"] as taiKhoanThanhVien;
            var cart = Session["ShoppingCart"] as List<Cart>;

            // ===== 1. BẮT BUỘC ĐĂNG NHẬP =====
            if (accountSession == null)
            {
                TempData["NotificationLogin"] = "Phiên đăng nhập đã hết hạn, vui lòng đăng nhập lại.";
                return RedirectToAction("LoginAccount", "Account");
            }

            // ===== 2. VALIDATE GIỎ HÀNG =====
            if (cart == null || !cart.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng của bạn đang trống, không thể thanh toán.");
            }

            if (!ModelState.IsValid)
            {
                return View(pi);
            }

            // ===== 3. RESET BIẾN TẠM =====
            tempDetailOrder = new List<chiTietDonHang>();
            tempCustomer = null;
            tempOrder = null;

            // Mã đơn hàng
            idOrder = string.Format("{0:ssmmhhdd}", DateTime.Now);

            // ===== 4. LẤY TÀI KHOẢN TỪ DB (để EF tracking) =====
            var accountDb = db.taiKhoanThanhViens
                .FirstOrDefault(m => m.taiKhoan == accountSession.taiKhoan);

            if (accountDb == null)
            {
                // Trường hợp hiếm khi xảy ra, nhưng phòng hờ
                ModelState.AddModelError("", "Không tìm thấy thông tin tài khoản. Vui lòng đăng nhập lại.");
                return View(pi);
            }

            // ===== 5. XỬ LÝ KHÁCH HÀNG (LIÊN KẾT VỚI TÀI KHOẢN) =====
            khachHang customer = null;

            if (!string.IsNullOrEmpty(accountDb.maKH))
            {
                customer = db.khachHangs.FirstOrDefault(m => m.maKH == accountDb.maKH);
            }

            if (customer == null)
            {
                // Tạo khách mới
                customer = new khachHang
                {
                    maKH = !string.IsNullOrEmpty(accountDb.maKH)
                        ? accountDb.maKH
                        : string.Format("{0:ddmmhhss}", DateTime.Now),

                    tenKH = string.IsNullOrEmpty(pi.NameCustomer)
                        ? (accountDb.hoDem + " " + accountDb.tenTV)
                        : pi.NameCustomer,

                    soDT = pi.MobileCustomer,
                    email = pi.Email,
                    diaChi = pi.DeliveryAddress,
                    gioiTinh = accountDb.gioiTinh,
                    ngaySinh = accountDb.ngaysinh ?? DateTime.Now,
                    ghiChu = pi.Note ?? ""
                };

                db.khachHangs.Add(customer);

                // Nếu tài khoản chưa có maKH thì gán luôn
                if (string.IsNullOrEmpty(accountDb.maKH))
                {
                    accountDb.maKH = customer.maKH;
                }
            }
            else
            {
                // Cập nhật thông tin giao hàng mới
                customer.tenKH = string.IsNullOrEmpty(pi.NameCustomer)
                    ? customer.tenKH
                    : pi.NameCustomer;

                customer.soDT = pi.MobileCustomer;
                customer.email = pi.Email;
                customer.diaChi = pi.DeliveryAddress;
                customer.ghiChu = pi.Note ?? customer.ghiChu;
            }

            tempCustomer = customer;

            // ===== 6. CẬP NHẬT PROFILE (ĐỊA CHỈ, SĐT, EMAIL) CỦA TÀI KHOẢN =====
            accountDb.diaChi = pi.DeliveryAddress;
            accountDb.soDT = pi.MobileCustomer;
            accountDb.email = pi.Email;

            // Cập nhật lại object trong Session cho đồng bộ
            accountSession.diaChi = accountDb.diaChi;
            accountSession.soDT = accountDb.soDT;
            accountSession.email = accountDb.email;
            Session["LoginInformation"] = accountSession;

            // ===== 7. TẠO ĐƠN HÀNG =====
            var order = new donHang
            {
                soDH = idOrder,
                maKH = customer.maKH,
                taiKhoan = accountDb.taiKhoan,
                ngayDat = DateTime.Now,
                trangThai = false,
                ngayGH = DateTime.Now.AddDays(2),
                diachiGH = pi.DeliveryAddress,
                loaiThanhToan = pi.PaymentMenthods,
                ghiChu = pi.Note ?? "",
                soDT = pi.MobileCustomer
            };

            db.donHangs.Add(order);
            tempOrder = order;

            // ===== 8. LƯU CHI TIẾT ĐƠN HÀNG =====
            foreach (var item in cart)
            {
                var detail = new chiTietDonHang
                {
                    soDH = idOrder,
                    maSP = item.IDProduct,
                    giaBan = item.Price,
                    soLuong = item.Quantity,
                    giamGia = item.Discount
                };

                tempDetailOrder.Add(detail);
                db.chiTietDonHangs.Add(detail);
            }

            db.SaveChanges();

            ViewData["CustomerOrder"] = tempCustomer;
            ViewData["Order"] = tempOrder;

            // ===== 9. THANH TOÁN ONLINE =====
            if (tempOrder.loaiThanhToan == "Payment_Auto")
            {
                var url = UrlPayments(pi.PaymentMenthods, tempOrder.soDH);
                return Redirect(url);
            }

            return RedirectToAction("CheckoutSuccess");
        }

        public ActionResult CheckoutSuccess()
        {
            ViewData["CustomerOrder"] = tempCustomer;
            ViewData["Order"] = tempOrder;

            try
            {
                if (tempOrder != null && tempOrder.loaiThanhToan == "Payment_Auto")
                {
                    string check = statusOrder();
                    if (check == null)
                    {
                        foreach (var item in tempDetailOrder.ToList())
                        {
                            if (item.soDH == tempOrder.soDH)
                            {
                                db.chiTietDonHangs.Remove(item);
                            }
                        }

                        var dh = db.donHangs.Find(tempOrder.soDH);
                        if (dh != null)
                        {
                            db.donHangs.Remove(dh);
                        }

                        if (tempCustomer != null)
                        {
                            db.khachHangs.Remove(tempCustomer);
                        }

                        tempOrder = null;
                        tempCustomer = null;
                        tempDetailOrder = new List<chiTietDonHang>();

                        db.SaveChanges();
                    }

                    return RedirectToAction("Index", "Home");
                }
            }
            catch
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        public string statusOrder()
        {
            if (Request.QueryString.Count > 0)
            {
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];
                var vnpayData = Request.QueryString;
                VnPayLibrary vnpay = new VnPayLibrary();

                foreach (string s in vnpayData)
                {
                    if (!string.IsNullOrEmpty(s) && s.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(s, vnpayData[s]);
                    }
                }

                string vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string vnp_TransactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

                bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);
                if (checkSignature)
                {
                    if (vnp_ResponseCode == "00" && vnp_TransactionStatus == "00")
                    {
                        return "ok";
                    }
                }
            }
            return null;
        }

        public string UrlPayments(string PaymentMenthods, string ordercode)
        {
            if (PaymentMenthods == "Payment_Auto")
            {
                string urlPayment = "";
                var order = db.donHangs.FirstOrDefault(m => m.soDH == ordercode);
                var detailOrder = db.chiTietDonHangs.Where(m => m.soDH == ordercode).ToList();

                int sumPrice = 0;
                foreach (var item in detailOrder)
                {
                    sumPrice += (int)((item.giaBan - item.giamGia) * item.soLuong);
                }

                string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"];
                string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
                string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

                VnPayLibrary vnpay = new VnPayLibrary();
                var price = sumPrice * 100;

                vnpay.AddRequestData("vnp_Version", VnPayLibrary.VERSION);
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                vnpay.AddRequestData("vnp_Amount", price.ToString());
                vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress());
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toán đơn hàng:" + order.soDH);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", order.soDH);

                urlPayment = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
                return urlPayment;
            }
            return "";
        }
    }
}
