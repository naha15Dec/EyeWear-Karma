using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
using BanGiay.ViewModel;

namespace BanGiay.Controllers
{
    public class ShoppingCartController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();

        public ActionResult Cart()
        {
            return View();
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng
        /// YÊU CẦU: phải đăng nhập trước
        /// </summary>
        public ActionResult AddItemToCart(string idProduct)
        {
            // 1. Kiểm tra đăng nhập
            var account = Session["LoginInformation"] as taiKhoanThanhVien;
            if (account == null)
            {
                // Thông báo + chuyển qua trang đăng nhập
                TempData["NotificationLogin"] = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng.";
                // Có thể truyền returnUrl nếu muốn quay lại sau khi login
                return RedirectToAction("LoginAccount", "Account");
            }

            // 2. Kiểm tra sản phẩm tồn tại
            sanPham product = db.sanPhams.FirstOrDefault(m => m.maSP == idProduct);
            if (product == null)
            {
                TempData["NotificationLogin"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Cart");
            }

            // 3. Lấy giỏ hàng từ Session (nếu null thì tạo mới)
            List<Cart> cart = (Session["ShoppingCart"] as List<Cart>) ?? new List<Cart>();

            // 4. Kiểm tra sản phẩm đã có trong giỏ chưa
            Cart item = cart.FirstOrDefault(m => m.IDProduct == idProduct);
            if (item == null)
            {
                Cart cartItem = new Cart
                {
                    IDProduct = idProduct,
                    NameProduct = product.tenSP,
                    ImageProduct = product.hinhDD,
                    Price = (int)product.giaBan,
                    Discount = (int)product.giamGia,
                    Total = (int)(product.giaBan - product.giamGia),
                    Quantity = 1
                };
                cart.Add(cartItem);
            }
            else
            {
                item.Quantity += 1;
                item.Total = (int)((product.giaBan * item.Quantity) - (product.giamGia * item.Quantity));
            }

            // 5. Lưu lại giỏ vào Session
            Session["ShoppingCart"] = cart;

            // Trả về trang giỏ hàng
            return RedirectToAction("Cart");
        }

        /// <summary>
        /// Xóa sản phẩm khỏi giỏ hàng
        /// </summary>
        public ActionResult RemoveItem(string idProduct)
        {
            List<Cart> cart = (Session["ShoppingCart"] as List<Cart>) ?? new List<Cart>();
            Cart item = cart.FirstOrDefault(m => m.IDProduct == idProduct);
            if (item != null)
            {
                cart.Remove(item);
            }
            Session["ShoppingCart"] = cart;
            return RedirectToAction("Cart");
        }

        /// <summary>
        /// Cập nhật số lượng
        /// </summary>
        public ActionResult UpdateQuantityItem(string idProduct, int quantity)
        {
            List<Cart> cart = (Session["ShoppingCart"] as List<Cart>) ?? new List<Cart>();
            Cart c = cart.FirstOrDefault(m => m.IDProduct == idProduct);
            if (c != null && quantity > 0)
            {
                c.Quantity = quantity;
                c.Total = (c.Price * quantity - c.Discount * quantity);
            }
            Session["ShoppingCart"] = cart;
            return RedirectToAction("Cart");
        }
    }
}
