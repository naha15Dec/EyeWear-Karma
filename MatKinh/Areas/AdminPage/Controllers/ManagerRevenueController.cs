using System;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = RoleConstants.ADMIN)]
    public class ManagerRevenueController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        [HttpGet]
        public ActionResult Revenue()
        {
            var model = BuildRevenueViewModel();
            return View(model);
        }

        private AdminRevenueVm BuildRevenueViewModel()
        {
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime tomorrow = today.AddDays(1);

            DateTime firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            DateTime firstDayNextMonth = firstDayOfMonth.AddMonths(1);

            /*
             * Doanh thu chỉ tính khi:
             * 1. Đơn đã giao thành công
             * 2. Trạng thái thanh toán là Paid
             *
             * Với COD: khi admin/shipper cập nhật Giao thành công,
             * ManagerOrderController đã set TrangThaiThanhToan = Paid.
             *
             * Với VNPAY: đơn chỉ được tạo sau khi VNPAY thành công,
             * nên trạng thái thanh toán là Paid ngay từ đầu.
             */
            var successOrders = db.DonHangs.Where(x =>
                x.TrangThai == OrderStatusConstants.DELIVERED &&
                x.TrangThaiThanhToan == PaymentConstants.PAID);

            var successOrderDetails = db.ChiTietDonHangs.Where(x =>
                x.DonHang.TrangThai == OrderStatusConstants.DELIVERED &&
                x.DonHang.TrangThaiThanhToan == PaymentConstants.PAID);

            var successOrdersInMonth = successOrders.Where(x =>
                (x.NgayHoanTat ?? x.NgayDat) >= firstDayOfMonth &&
                (x.NgayHoanTat ?? x.NgayDat) < firstDayNextMonth);

            var successOrdersToday = successOrders.Where(x =>
                (x.NgayHoanTat ?? x.NgayDat) >= today &&
                (x.NgayHoanTat ?? x.NgayDat) < tomorrow);

            var successOrderDetailsInMonth = successOrderDetails.Where(x =>
                (x.DonHang.NgayHoanTat ?? x.DonHang.NgayDat) >= firstDayOfMonth &&
                (x.DonHang.NgayHoanTat ?? x.DonHang.NgayDat) < firstDayNextMonth);

            var successOrderDetailsToday = successOrderDetails.Where(x =>
                (x.DonHang.NgayHoanTat ?? x.DonHang.NgayDat) >= today &&
                (x.DonHang.NgayHoanTat ?? x.DonHang.NgayDat) < tomorrow);

            var codSuccessOrders = successOrders.Where(x =>
                x.PhuongThucThanhToan == PaymentConstants.COD);

            var vnpaySuccessOrders = successOrders.Where(x =>
                x.PhuongThucThanhToan == PaymentConstants.VNPAY);

            /*
             * Với flow VNPAY mới:
             * - VNPAY lỗi/hủy: không tạo DonHang
             * - Vì vậy TotalPaymentFailed chỉ đếm được các đơn lỗi cũ còn nằm trong DB
             * - Muốn thống kê mọi lần VNPAY lỗi thì cần bảng log giao dịch riêng
             */
            int paymentFailedCount = db.DonHangs.Count(x =>
                x.TrangThaiThanhToan == PaymentConstants.FAILED ||
                (
                    x.PhuongThucThanhToan == PaymentConstants.VNPAY &&
                    x.TrangThai == OrderStatusConstants.CANCELLED &&
                    x.TrangThaiThanhToan != PaymentConstants.PAID
                ));

            var model = new AdminRevenueVm
            {
                // Revenue
                TotalRevenue = successOrders.Sum(x => (decimal?)x.TongThanhToan) ?? 0,

                TotalMonthRevenue = successOrdersInMonth.Sum(x => (decimal?)x.TongThanhToan) ?? 0,

                TotalTodayRevenue = successOrdersToday.Sum(x => (decimal?)x.TongThanhToan) ?? 0,

                // Product sold
                TotalProductSoldAll = successOrderDetails.Sum(x => (int?)x.SoLuong) ?? 0,

                TotalProductSoldMonth = successOrderDetailsInMonth.Sum(x => (int?)x.SoLuong) ?? 0,

                TotalProductSoldToday = successOrderDetailsToday.Sum(x => (int?)x.SoLuong) ?? 0,

                // Orders
                TotalOrderSuccess = successOrders.Count(),

                TotalOrderMonth = successOrdersInMonth.Count(),

                TotalOrderToday = successOrdersToday.Count(),

                // By payment method
                TotalOrderCod = codSuccessOrders.Count(),

                TotalOrderVnpay = vnpaySuccessOrders.Count(),

                TotalRevenueCod = codSuccessOrders.Sum(x => (decimal?)x.TongThanhToan) ?? 0,

                TotalRevenueVnpay = vnpaySuccessOrders.Sum(x => (decimal?)x.TongThanhToan) ?? 0,

                // Payment failed
                TotalPaymentFailed = paymentFailedCount
            };

            return model;
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