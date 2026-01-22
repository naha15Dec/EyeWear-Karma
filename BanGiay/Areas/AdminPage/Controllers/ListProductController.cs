using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BanGiay.Models;
using BanGiay.ViewModel;

namespace BanGiay.Areas.AdminPage.Controllers
{
    [CustomAuthentication]
    [CustomAuthorize(Roles = "Quản trị, Nhân viên")]
    public class ListProductController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        private const string VIRTUAL_IMG_FOLDER = "/Asset/SaveImgProduct";

        // ========== HÀM HỖ TRỢ ==========

        /// <summary>
        /// Bỏ phần ?v=... trong đường dẫn ảnh (dùng cho chống cache)
        /// </summary>
        private static string StripVersion(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            var q = path.IndexOf('?');
            return q > -1 ? path.Substring(0, q) : path;
        }

        // ========== VIEW HIỂN THỊ ==========

        public ActionResult ProductsList()
        {
            UpdateInterface(null);
            return View();
        }

        public ActionResult AddProduct()
        {
            UpdateInterface(null);
            return View(new ProductVM());
        }

        // ========== THÊM SẢN PHẨM ==========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddProduct(
            ProductVM pvm,
            IEnumerable<HttpPostedFileBase> images,
            HttpPostedFileBase imageAvatar)
        {
            // Kiểm tra đăng nhập session
            var login = Session["LoginInformation"] as taiKhoanThanhVien;
            if (login == null)
            {
                // Nếu vì lý do gì đó session mất, yêu cầu đăng nhập lại
                return RedirectToAction("LoginAccount", "Account", new { area = "" });
            }

            // Kiểm tra trùng mô tả (nếu bạn muốn đảm bảo mô tả chi tiết là duy nhất)
            var spTrung = db.sanPhams.FirstOrDefault(m => m.moTa == pvm.Description);
            if (spTrung != null)
            {
                ModelState.AddModelError("Description", "Mô tả chi tiết sản phẩm này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                UpdateInterface(null);
                return View(pvm);
            }

            var thuongHieu = db.thuongHieux.FirstOrDefault(m => m.maThuongHieu == pvm.IDBrand);

            var themSP = new sanPham
            {
                maSP = string.Format("{0:MMmmddss}", DateTime.Now),
                tenSP = pvm.NameProduct,
                gioiTinh = pvm.Sex != null && pvm.Sex.Equals("Nam"),
                moTaTomTat = pvm.SummaryDescription,
                moTa = pvm.Description,
                ngayDang = DateTime.Now,
                giaBan = pvm.Price,
                giamGia = pvm.Discount,
                // Web chuyển sang bán mắt kính nên không dùng kích thước nữa
                // Nếu trong DB còn cột kichThuoc thì cho rỗng:
                kichThuoc = "",
                trangThai = pvm.StatusProduct,
                taiKhoan = login.taiKhoan,
                maThuongHieu = pvm.IDBrand,
                thuongHieu = thuongHieu,
                maLoai = pvm.IDTypeProduct
            };

            // Lưu danh sách ảnh chi tiết
            if (images != null)
            {
                foreach (var image in images)
                {
                    if (image != null && image.ContentLength > 0)
                    {
                        var h = ImageProduct(image, themSP);
                        if (!string.IsNullOrEmpty(h.hinhSP))
                        {
                            db.danhSachHinhs.Add(h);
                        }
                    }
                }
            }

            // Lưu avatar
            if (imageAvatar != null && imageAvatar.ContentLength > 0)
            {
                var avt = ImageProduct(imageAvatar, themSP);
                themSP.hinhDD = avt.hinhSP;
            }

            db.sanPhams.Add(themSP);
            db.SaveChanges();

            return RedirectToAction("ProductsList");
        }

        // ========== CẬP NHẬT SẢN PHẨM ==========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(
            string idProduct,
            ProductVM pvm,
            IEnumerable<HttpPostedFileBase> images,
            HttpPostedFileBase imageAvatar)
        {
            var sp = db.sanPhams.FirstOrDefault(m => m.maSP == idProduct);
            if (sp == null)
            {
                return RedirectToAction("ProductsList");
            }

            // Áp dụng validate từ ProductVM
            if (!ModelState.IsValid)
            {
                UpdateInterface(null);
                // Ở đây tuỳ bạn dùng view riêng cho Edit hay edit dạng modal trên ProductsList
                // Tạm quay lại danh sách
                return RedirectToAction("ProductsList");
            }

            sp.tenSP = pvm.NameProduct;
            sp.moTaTomTat = pvm.SummaryDescription;
            sp.moTa = pvm.Description;
            sp.giaBan = pvm.Price;
            sp.giamGia = pvm.Discount;
            // Không dùng kích thước nữa cho mắt kính:
            // sp.kichThuoc = sp.kichThuoc;  // giữ nguyên hoặc rỗng nếu muốn
            sp.maLoai = pvm.IDTypeProduct;
            sp.maThuongHieu = pvm.IDBrand;
            sp.trangThai = pvm.StatusProduct;
            sp.gioiTinh = pvm.Sex != null && pvm.Sex.Equals("Nam");

            // Cập nhật navigation Brand (nếu cần thiết)
            sp.thuongHieu = db.thuongHieux.FirstOrDefault(m => m.maThuongHieu == pvm.IDBrand);

            // Nếu có upload ảnh mới → xóa ảnh cũ + thêm lại
            if (images != null && images.Any(i => i != null && i.ContentLength > 0))
            {
                DeleteImgFilesOnDisk(idProduct);
                DeleteImgInDatabase(idProduct);

                foreach (var image in images)
                {
                    if (image != null && image.ContentLength > 0)
                    {
                        var h = ImageProduct(image, sp);
                        if (!string.IsNullOrEmpty(h.hinhSP))
                        {
                            db.danhSachHinhs.Add(h);
                        }
                    }
                }
            }

            // Cập nhật avatar
            if (imageAvatar != null && imageAvatar.ContentLength > 0)
            {
                // Xóa avatar cũ nếu có
                var oldAvt = StripVersion(sp.hinhDD);
                if (!string.IsNullOrEmpty(oldAvt))
                {
                    var fileName = Path.GetFileName(oldAvt);
                    var filePath = Path.Combine(Server.MapPath("~" + VIRTUAL_IMG_FOLDER), fileName);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                var avt = ImageProduct(imageAvatar, sp);
                sp.hinhDD = avt.hinhSP;
            }

            db.SaveChanges();
            return RedirectToAction("ProductsList");
        }

        // ========== XÓA SẢN PHẨM ==========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string idProduct)
        {
            try
            {
                var sp = db.sanPhams.FirstOrDefault(m => m.maSP == idProduct);
                if (sp == null)
                    return RedirectToAction("ProductsList");

                DeleteImgFilesOnDisk(idProduct);
                DeleteImgInDatabase(idProduct);
                db.sanPhams.Remove(sp);
                db.SaveChanges();

                return RedirectToAction("ProductsList");
            }
            catch
            {
                // Có lỗi cũng quay về list, tránh crash
                return RedirectToAction("ProductsList");
            }
        }

        // ========== TÌM SẢN PHẨM ==========

        public ActionResult FindProductByName(string nameProduct)
        {
            UpdateInterface(nameProduct);
            return View("ProductsList");
        }

        // ========== CẬP NHẬT GIAO DIỆN ==========

        private void UpdateInterface(string namePro)
        {
            ViewData["Categories"] = db.loaiSPs.ToList();
            ViewData["Brands"] = db.thuongHieux.ToList();

            var tk = Session["LoginInformation"] as taiKhoanThanhVien;
            if (tk == null)
            {
                ViewData["listProduct"] = new List<sanPham>();
                return;
            }

            ViewData["listProduct"] = db.sanPhams
                .Where(m => m.taiKhoan == tk.taiKhoan
                    && (string.IsNullOrEmpty(namePro) || m.tenSP.Contains(namePro)))
                .ToList();
        }

        // ========== HÀM XÓA ẢNH TRONG DB ==========

        private void DeleteImgInDatabase(string id)
        {
            var list = db.danhSachHinhs.Where(m => m.maSP == id).ToList();
            foreach (var rmImg in list)
            {
                db.danhSachHinhs.Remove(rmImg);
            }
        }

        // ========== HÀM XÓA ẢNH TRÊN Ổ ĐĨA ==========

        private void DeleteImgFilesOnDisk(string id)
        {
            var physicalFolder = Server.MapPath("~" + VIRTUAL_IMG_FOLDER);

            var list = db.danhSachHinhs.Where(m => m.maSP == id).ToList();
            foreach (var img in list)
            {
                var vp = StripVersion(img.hinhSP);
                if (!string.IsNullOrEmpty(vp))
                {
                    var fileName = Path.GetFileName(vp);
                    var filePath = Path.Combine(physicalFolder, fileName);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
            }
        }

        // ========== HÀM LƯU ẢNH (ĐÃ CHỐNG CACHE) ==========

        public danhSachHinh ImageProduct(HttpPostedFileBase image, sanPham sp)
        {
            if (image == null || sp == null || image.ContentLength <= 0)
                return new danhSachHinh { hinhSP = "", maSP = sp?.maSP };

            var ext = Path.GetExtension(image.FileName)?.ToLowerInvariant();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".gif", ".webp"
            };

            if (!allowed.Contains(ext))
                throw new InvalidOperationException("Định dạng ảnh không hợp lệ.");

            var physicalFolder = Server.MapPath("~" + VIRTUAL_IMG_FOLDER);
            Directory.CreateDirectory(physicalFolder);

            var fileName = Guid.NewGuid().ToString("N") + ext;
            var physicalPath = Path.Combine(physicalFolder, fileName);
            image.SaveAs(physicalPath);

            var virtualPath = $"{VIRTUAL_IMG_FOLDER}/{fileName}";
            var version = DateTime.UtcNow.Ticks;

            return new danhSachHinh
            {
                hinhSP = $"{virtualPath}?v={version}",
                maSP = sp.maSP
            };
        }
    }
}
