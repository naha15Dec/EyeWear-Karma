using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MatKinh.Models;
using MatKinh.ViewModel;

namespace MatKinh.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly BanMatKinhEntities db = new BanMatKinhEntities();

        private const int ProductStatusActive = 1;

        public ActionResult Cart()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpGet]
        public ActionResult AddItemToCart(int sanPhamId)
        {
            TaiKhoan currentAccount = GetCurrentAccount();
            if (currentAccount == null)
            {
                TempData["NotificationLogin"] = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng.";
                return RedirectToAction("LoginAccount", "Account");
            }

            var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == sanPhamId);
            if (product == null)
            {
                TempData["CartError"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Cart");
            }

            if (product.TrangThai != ProductStatusActive)
            {
                TempData["CartError"] = "Sản phẩm hiện không khả dụng.";
                return RedirectToAction("Cart");
            }

            if (product.SoLuongTon <= 0)
            {
                TempData["CartError"] = "Sản phẩm đã hết hàng.";
                return RedirectToAction("Cart");
            }

            List<Cart> cart = GetCart();
            Cart item = cart.FirstOrDefault(x => x.SanPhamId == sanPhamId);

            decimal donGia = GetOriginalPrice(product);
            decimal giamGia = GetDiscountAmount(product);

            if (item == null)
            {
                item = new Cart
                {
                    SanPhamId = product.SanPhamId,
                    TenSanPham = product.TenSanPham,
                    HinhAnh = product.HinhAnhChinh,
                    DonGia = donGia,
                    GiamGia = giamGia,
                    SoLuong = 1
                };

                cart.Add(item);
            }
            else
            {
                if (item.SoLuong + 1 > product.SoLuongTon)
                {
                    TempData["CartError"] = $"Sản phẩm '{product.TenSanPham}' không đủ số lượng tồn kho.";
                    return RedirectToAction("Cart");
                }

                item.SoLuong += 1;

                item.DonGia = donGia;
                item.GiamGia = giamGia;
                item.TenSanPham = product.TenSanPham;
                item.HinhAnh = product.HinhAnhChinh;
            }

            SaveCart(cart);
            TempData["CartSuccess"] = "Đã thêm sản phẩm vào giỏ hàng.";

            return RedirectToAction("Cart");
        }

        [HttpGet]
        public ActionResult RemoveItem(int sanPhamId)
        {
            List<Cart> cart = GetCart();
            Cart item = cart.FirstOrDefault(x => x.SanPhamId == sanPhamId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
                TempData["CartSuccess"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }

            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantityItem(int sanPhamId, int soLuong)
        {
            List<Cart> cart = GetCart();
            Cart item = cart.FirstOrDefault(x => x.SanPhamId == sanPhamId);

            if (item == null)
            {
                TempData["CartError"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                return RedirectToAction("Cart");
            }

            var product = db.SanPhams.FirstOrDefault(x => x.SanPhamId == sanPhamId);
            if (product == null)
            {
                cart.Remove(item);
                SaveCart(cart);
                TempData["CartError"] = "Sản phẩm không còn tồn tại nên đã được xóa khỏi giỏ hàng.";
                return RedirectToAction("Cart");
            }

            if (product.TrangThai != ProductStatusActive)
            {
                cart.Remove(item);
                SaveCart(cart);
                TempData["CartError"] = "Sản phẩm hiện không còn kinh doanh nên đã được xóa khỏi giỏ hàng.";
                return RedirectToAction("Cart");
            }

            if (soLuong <= 0)
            {
                cart.Remove(item);
                SaveCart(cart);
                TempData["CartSuccess"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
                return RedirectToAction("Cart");
            }

            if (soLuong > product.SoLuongTon)
            {
                TempData["CartError"] = $"Sản phẩm '{product.TenSanPham}' chỉ còn {product.SoLuongTon} sản phẩm trong kho.";
                return RedirectToAction("Cart");
            }

            item.SoLuong = soLuong;
            item.DonGia = GetOriginalPrice(product);
            item.GiamGia = GetDiscountAmount(product);
            item.TenSanPham = product.TenSanPham;
            item.HinhAnh = product.HinhAnhChinh;

            SaveCart(cart);
            TempData["CartSuccess"] = "Đã cập nhật số lượng sản phẩm.";

            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            Session["ShoppingCart"] = new List<Cart>();
            TempData["CartSuccess"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToAction("Cart");
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

        private List<Cart> GetCart()
        {
            return Session["ShoppingCart"] as List<Cart> ?? new List<Cart>();
        }

        private void SaveCart(List<Cart> cart)
        {
            Session["ShoppingCart"] = cart;
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