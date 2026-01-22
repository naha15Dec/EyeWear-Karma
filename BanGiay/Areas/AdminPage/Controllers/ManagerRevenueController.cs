using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
namespace BanGiay.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = "Quản trị")]
    public class ManagerRevenueController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        public ActionResult Revenue()
        {
            TotalRevenue();
            return View();
        }
        /// <summary>
        /// Phương thức này dùng để tính các doanh thu của website
        /// Như là: Tổng doanh thu tháng, doanh thu tuần, doanh thu, sản phẩm bán được trong tháng trong ngày, tổng đơn hàng thanh toán bằng phương thức tự động và sau giao hàng
        /// </summary>
        private void TotalRevenue()
        {
            int totalMonthRevenue = 0;
            int totalTodayRevenue = 0;
            int totalRevenue = 0;

            int totalProductSoldToday = 0;
            int totalProductSoldMonth = 0;

            int totalOrderSuccess = 0;

            int totalOrderPaymentAuto = 0;
            int totalOrderPaymentAfterDelivery = 0;
          
            List<donHang> Orders = db.donHangs.ToList();
            List<chiTietDonHang> detailOrders = db.chiTietDonHangs.ToList();
            foreach(var item in detailOrders)
            {
                if (item.donHang.trangThai == true )
                {
                    if((((DateTime)item.donHang.ngayDat).Year == DateTime.Now.Year && ((DateTime)item.donHang.ngayDat).Month == DateTime.Now.Month))
                    {
                        totalMonthRevenue += (int)((item.giaBan - item.giamGia) * item.soLuong);
                        totalProductSoldMonth += (int)item.soLuong;
                    }
                    if ((((DateTime)item.donHang.ngayDat).Year == DateTime.Now.Year && ((DateTime)item.donHang.ngayDat).Month == DateTime.Now.Month && ((DateTime)item.donHang.ngayDat).Day == DateTime.Now.Day))
                    {
                        totalTodayRevenue += (int)((item.giaBan - item.giamGia) * item.soLuong);
                        totalProductSoldToday += (int)item.soLuong;
                    }
                    totalRevenue += (int)((item.giaBan - item.giamGia) * item.soLuong);
                    totalOrderSuccess++;
                }
                if (item.donHang.loaiThanhToan.Equals("Payment_Before"))
                {
                    totalOrderPaymentAfterDelivery++;
                }
                else
                {
                    totalOrderPaymentAuto++;
                }
            }
            ViewData["totalMonthRevenue"] = totalMonthRevenue;
            ViewData["totalTodayRevenue"] = totalTodayRevenue;
            ViewData["totalRevenue"] = totalRevenue;
            ViewData["totalProductSoldToday"] = totalProductSoldToday;
            ViewData["totalProductSoldMonth"] = totalProductSoldMonth;
            ViewData["totalOrderSuccess"] = totalOrderSuccess;
            ViewData["totalOrderPaymentAuto"] = totalOrderPaymentAuto;
            ViewData["totalOrderPaymentAfterDelivery"] = totalOrderPaymentAfterDelivery;
        }
    }
}