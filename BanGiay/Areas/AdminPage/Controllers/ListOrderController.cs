using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
namespace BanGiay.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    public class ListOrderController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        static bool checkEnableTheOrder;
        public ActionResult OrdersList(string enable)
        {
            checkEnableTheOrder = (enable.Equals("enable"));
            UpdateInterface(null);
            ViewBag.titleListOrder = (checkEnableTheOrder == true ? "Đơn hàng thành công" : "Đơn hàng đang xử lí");
            return View();
        }
        /// <summary>
        /// Action này dùng để xem chi tiết đơn hàng
        /// </summary>
        /// <param name="numberOfOrder"></param>
        /// <returns></returns>
      
        public ActionResult DetailOrder(string numberOfOrder)
        {
            donHang order = db.donHangs.FirstOrDefault(m=>m.soDH.Equals(numberOfOrder));
            List<chiTietDonHang> detailOrders = db.chiTietDonHangs.Where(m => m.soDH.Contains(numberOfOrder)).ToList();
            //ViewData["listOfProductInOrder"] = db.chiTietDonHangs.Where(m => m.soDH.Contains(numberOfOrder)).ToList();

            int totalPrice = 0;
            foreach (var item in detailOrders)
            {
                totalPrice += (int)(((item.giaBan - item.giamGia ) * item.soLuong));
            }

            ViewData["totalPrice"] = totalPrice;
            ViewData["listOfProductInOrder"] = detailOrders;
            ViewData["MostRecentChanges"] = db.thongTinCuaHangs.OrderByDescending(m => m.thoiGian).FirstOrDefault();
            return View(order);
        }
        /// <summary>
        /// Action này dùng để báo là đơn hàng đã hoàn thành. Trang thái sẽ thành true
        /// </summary>
        /// <param name="numberOfOrder"></param>
        /// <returns></returns>
        public ActionResult OrderSuccess(string numberOfOrder)
        {
            donHang order = db.donHangs.FirstOrDefault(m => m.soDH.Equals(numberOfOrder));
            order.trangThai = true;
            db.SaveChanges();
            UpdateInterface(null);
            return View("OrdersList");
        }
        /// <summary>
        /// Action này dùng để hủy đơn hàng và quyền của action này chỉ danh cho tài khoản quản trị
        /// </summary>
        /// <param name="numberOfOrder"></param>
        /// <returns></returns>
        [CustomAuthorize(Roles = "Quản trị")]
        public ActionResult CancelOrder(string numberOfOrder)
        {
            donHang order = db.donHangs.FirstOrDefault(m => m.soDH.Equals(numberOfOrder));
            if (order != null)
            {
                db.donHangs.Remove(order);
                List<chiTietDonHang> DetailsOrder = db.chiTietDonHangs.Where(m => m.soDH.Contains(numberOfOrder)).ToList();
                foreach (var item in DetailsOrder)
                {
                    db.chiTietDonHangs.Remove(item);
                }
                db.SaveChanges();
            }
            UpdateInterface(null);
            return View("OrdersList");
        }
        public ActionResult FindOrderByNumber(string numberOfOrder)
        {
            UpdateInterface(numberOfOrder);
            return View("OrdersList");
        }
        /// <summary>
        /// Hàm này dùng để cập nhật lại danh sách đơn hàng
        /// </summary>
        /// <param name="check"></param>
        private void UpdateInterface(string numberOfOrder)
        {
            ViewData["listOrders"] = db.donHangs.Where(m=>m.trangThai == checkEnableTheOrder && (string.IsNullOrEmpty(numberOfOrder) || m.soDH.Contains(numberOfOrder))).ToList();
        }
    }
}