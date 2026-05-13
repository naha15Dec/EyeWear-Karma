using System;
using System.Collections.Generic;
using System.Data.Entity;
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
        private const int MaxQuantityPerItem = 10;

        public ActionResult Cart()
        {
            int? khachHangId = GetCurrentKhachHangId();

            if (!khachHangId.HasValue)
            {
                TempData["NotificationLogin"] = "Bạn cần đăng nhập để xem giỏ hàng.";
                return RedirectToAction("LoginAccount", "Account");
            }

            var cart = GetCartByCustomer(khachHangId.Value);
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddItemToCart(int sanPhamId, string returnUrl)
        {
            int? khachHangId = GetCurrentKhachHangId();

            if (!khachHangId.HasValue)
            {
                TempData["NotificationLogin"] = "Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng.";
                return RedirectToAction("LoginAccount", "Account");
            }

            var product = db.SanPhams
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .FirstOrDefault(x => x.SanPhamId == sanPhamId);

            if (product == null)
            {
                TempData["CartError"] = "Sản phẩm không tồn tại.";
                return RedirectToSafeReturnUrl(returnUrl);
            }

            if (!IsProductAvailable(product))
            {
                TempData["CartError"] = "Sản phẩm hiện không khả dụng.";
                return RedirectToSafeReturnUrl(returnUrl);
            }

            if (product.SoLuongTon <= 0)
            {
                TempData["CartError"] = "Sản phẩm đã hết hàng.";
                return RedirectToSafeReturnUrl(returnUrl);
            }

            var cartItem = db.GioHangChiTiets
                .FirstOrDefault(x =>
                    x.KhachHangId == khachHangId.Value &&
                    x.SanPhamId == sanPhamId);

            if (cartItem == null)
            {
                cartItem = new GioHangChiTiet
                {
                    KhachHangId = khachHangId.Value,
                    SanPhamId = product.SanPhamId,
                    SoLuong = 1,
                    CreatedAt = DateTime.Now
                };

                db.GioHangChiTiets.Add(cartItem);
            }
            else
            {
                int newQuantity = cartItem.SoLuong + 1;

                if (newQuantity > product.SoLuongTon)
                {
                    TempData["CartError"] = $"Sản phẩm '{product.TenSanPham}' chỉ còn {product.SoLuongTon} sản phẩm trong kho.";
                    return RedirectToSafeReturnUrl(returnUrl);
                }

                if (newQuantity > MaxQuantityPerItem)
                {
                    TempData["CartError"] = $"Bạn chỉ có thể mua tối đa {MaxQuantityPerItem} sản phẩm cho mỗi mẫu kính.";
                    return RedirectToSafeReturnUrl(returnUrl);
                }

                cartItem.SoLuong = newQuantity;
                cartItem.UpdatedAt = DateTime.Now;
            }

            db.SaveChanges();

            try
            {
                UserBehaviorLogger.Log(
                    db,
                    Session,
                    product.SanPhamId,
                    UserBehaviorConstants.ADD_TO_CART,
                    UserBehaviorConstants.ADD_TO_CART_WEIGHT,
                    "CART",
                    null
                );

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            TempData["CartSuccess"] = "Đã thêm sản phẩm vào giỏ hàng.";
            return RedirectToSafeReturnUrl(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveItem(int sanPhamId)
        {
            int? khachHangId = GetCurrentKhachHangId();

            if (!khachHangId.HasValue)
            {
                TempData["NotificationLogin"] = "Bạn cần đăng nhập để thao tác giỏ hàng.";
                return RedirectToAction("LoginAccount", "Account");
            }

            var cartItem = db.GioHangChiTiets
                .FirstOrDefault(x =>
                    x.KhachHangId == khachHangId.Value &&
                    x.SanPhamId == sanPhamId);

            if (cartItem != null)
            {
                db.GioHangChiTiets.Remove(cartItem);
                db.SaveChanges();
                TempData["CartSuccess"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
            }

            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateQuantityItem(int sanPhamId, int soLuong)
        {
            int? khachHangId = GetCurrentKhachHangId();

            if (!khachHangId.HasValue)
            {
                TempData["NotificationLogin"] = "Bạn cần đăng nhập để thao tác giỏ hàng.";
                return RedirectToAction("LoginAccount", "Account");
            }

            var cartItem = db.GioHangChiTiets
                .FirstOrDefault(x =>
                    x.KhachHangId == khachHangId.Value &&
                    x.SanPhamId == sanPhamId);

            if (cartItem == null)
            {
                TempData["CartError"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                return RedirectToAction("Cart");
            }

            var product = db.SanPhams
                .Include(x => x.LoaiSanPham)
                .Include(x => x.ThuongHieu)
                .FirstOrDefault(x => x.SanPhamId == sanPhamId);

            if (product == null)
            {
                db.GioHangChiTiets.Remove(cartItem);
                db.SaveChanges();

                TempData["CartError"] = "Sản phẩm không còn tồn tại nên đã được xóa khỏi giỏ hàng.";
                return RedirectToAction("Cart");
            }

            if (!IsProductAvailable(product))
            {
                db.GioHangChiTiets.Remove(cartItem);
                db.SaveChanges();

                TempData["CartError"] = "Sản phẩm hiện không khả dụng nên đã được xóa khỏi giỏ hàng.";
                return RedirectToAction("Cart");
            }

            if (soLuong <= 0)
            {
                db.GioHangChiTiets.Remove(cartItem);
                db.SaveChanges();

                TempData["CartSuccess"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
                return RedirectToAction("Cart");
            }

            if (soLuong > product.SoLuongTon)
            {
                TempData["CartError"] = $"Sản phẩm '{product.TenSanPham}' chỉ còn {product.SoLuongTon} sản phẩm trong kho.";
                return RedirectToAction("Cart");
            }

            if (soLuong > MaxQuantityPerItem)
            {
                TempData["CartError"] = $"Bạn chỉ có thể mua tối đa {MaxQuantityPerItem} sản phẩm cho mỗi mẫu kính.";
                return RedirectToAction("Cart");
            }

            cartItem.SoLuong = soLuong;
            cartItem.UpdatedAt = DateTime.Now;

            db.SaveChanges();

            TempData["CartSuccess"] = "Đã cập nhật số lượng sản phẩm.";
            return RedirectToAction("Cart");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearCart()
        {
            int? khachHangId = GetCurrentKhachHangId();

            if (!khachHangId.HasValue)
            {
                TempData["NotificationLogin"] = "Bạn cần đăng nhập để thao tác giỏ hàng.";
                return RedirectToAction("LoginAccount", "Account");
            }

            var cartItems = db.GioHangChiTiets
                .Where(x => x.KhachHangId == khachHangId.Value)
                .ToList();

            if (cartItems.Any())
            {
                db.GioHangChiTiets.RemoveRange(cartItems);
                db.SaveChanges();
            }

            TempData["CartSuccess"] = "Đã xóa toàn bộ giỏ hàng.";
            return RedirectToAction("Cart");
        }

        [ChildActionOnly]
        public ActionResult CartCount()
        {
            int count = 0;
            int? khachHangId = GetCurrentKhachHangId();

            if (khachHangId.HasValue)
            {
                count = db.GioHangChiTiets
                    .Where(x => x.KhachHangId == khachHangId.Value)
                    .Select(x => (int?)x.SoLuong)
                    .Sum() ?? 0;
            }

            return PartialView("_CartCount", count);
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

            var customer = db.KhachHangs
                .FirstOrDefault(x =>
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

            return db.TaiKhoans
                .FirstOrDefault(x =>
                    x.TaiKhoanId == account.TaiKhoanId &&
                    x.IsActive);
        }

        private ActionResult RedirectToSafeReturnUrl(string returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Product");
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