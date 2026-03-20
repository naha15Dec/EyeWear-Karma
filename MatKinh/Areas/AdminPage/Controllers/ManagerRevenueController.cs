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

            var deliveredOrders = db.DonHangs
                .Where(x => x.TrangThai == OrderStatusConstants.DELIVERED);

            var deliveredOrderDetails = db.ChiTietDonHangs
                .Where(x => x.DonHang.TrangThai == OrderStatusConstants.DELIVERED);

            var codDeliveredOrders = deliveredOrders
                .Where(x => x.PhuongThucThanhToan == PaymentConstants.COD);

            var vnpayDeliveredOrders = deliveredOrders
                .Where(x => x.PhuongThucThanhToan == PaymentConstants.VNPAY);

            var model = new AdminRevenueVm
            {
                // Revenue
                TotalRevenue = deliveredOrders.Any()
                    ? deliveredOrders.Sum(x => x.TongThanhToan)
                    : 0,

                TotalMonthRevenue = deliveredOrders
                    .Where(x => x.NgayDat >= firstDayOfMonth && x.NgayDat < firstDayNextMonth)
                    .Any()
                        ? deliveredOrders
                            .Where(x => x.NgayDat >= firstDayOfMonth && x.NgayDat < firstDayNextMonth)
                            .Sum(x => x.TongThanhToan)
                        : 0,

                TotalTodayRevenue = deliveredOrders
                    .Where(x => x.NgayDat >= today && x.NgayDat < tomorrow)
                    .Any()
                        ? deliveredOrders
                            .Where(x => x.NgayDat >= today && x.NgayDat < tomorrow)
                            .Sum(x => x.TongThanhToan)
                        : 0,

                // Product sold
                TotalProductSoldAll = deliveredOrderDetails.Any()
                    ? deliveredOrderDetails.Sum(x => x.SoLuong)
                    : 0,

                TotalProductSoldMonth = deliveredOrderDetails
                    .Where(x => x.DonHang.NgayDat >= firstDayOfMonth && x.DonHang.NgayDat < firstDayNextMonth)
                    .Any()
                        ? deliveredOrderDetails
                            .Where(x => x.DonHang.NgayDat >= firstDayOfMonth && x.DonHang.NgayDat < firstDayNextMonth)
                            .Sum(x => x.SoLuong)
                        : 0,

                TotalProductSoldToday = deliveredOrderDetails
                    .Where(x => x.DonHang.NgayDat >= today && x.DonHang.NgayDat < tomorrow)
                    .Any()
                        ? deliveredOrderDetails
                            .Where(x => x.DonHang.NgayDat >= today && x.DonHang.NgayDat < tomorrow)
                            .Sum(x => x.SoLuong)
                        : 0,

                // Orders
                TotalOrderSuccess = deliveredOrders.Count(),

                TotalOrderMonth = deliveredOrders.Count(x =>
                    x.NgayDat >= firstDayOfMonth && x.NgayDat < firstDayNextMonth),

                TotalOrderToday = deliveredOrders.Count(x =>
                    x.NgayDat >= today && x.NgayDat < tomorrow),

                // By payment method
                TotalOrderCod = codDeliveredOrders.Count(),
                TotalOrderVnpay = vnpayDeliveredOrders.Count(),

                TotalRevenueCod = codDeliveredOrders.Any()
                    ? codDeliveredOrders.Sum(x => x.TongThanhToan)
                    : 0,

                TotalRevenueVnpay = vnpayDeliveredOrders.Any()
                    ? vnpayDeliveredOrders.Sum(x => x.TongThanhToan)
                    : 0,

                // Payment failed
                TotalPaymentFailed = db.DonHangs.Count(x =>
                    x.TrangThaiThanhToan == PaymentConstants.FAILED)
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