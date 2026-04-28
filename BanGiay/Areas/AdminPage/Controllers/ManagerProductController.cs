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
    [CustomAuthorize(Roles = "Quản trị")]
    public class ManagerProductController : Controller
    {
        static DoAnLTW2Entities db = new DoAnLTW2Entities();
        static bool checkStatus;

        // ================== DANH SÁCH SẢN PHẨM (CÒN / HẾT HÀNG) ==================
        public ActionResult Index(string statusProduct)
        {
            // Nếu statusProduct null → mặc định lấy còn hàng
            checkStatus = string.Equals(statusProduct, "stock", StringComparison.OrdinalIgnoreCase);

            UpdateInterface(checkStatus);
            return View();
        }

        /// <summary>
        /// Dùng để tìm kiếm sản phẩm bằng mã sản phẩm
        /// </summary>
        public ActionResult FindProductByID(string idProduct)
        {
            ViewBag.HeaderManagerProduct = (checkStatus ? "Danh sách còn hàng" : "Danh sách hết hàng");
            ViewData["listCategories"] = db.loaiSPs.ToList();
            ViewData["listBrand"] = db.thuongHieux.ToList();

            ViewData["listProduct"] = db.sanPhams
                .Where(m =>
                    m.trangThai == checkStatus &&
                    (string.IsNullOrEmpty(idProduct) || m.maSP.Contains(idProduct)))
                .ToList();

            return View("Index");
        }

        // ================== CẬP NHẬT SẢN PHẨM ==================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(string idProduct, ProductVM pvm, IEnumerable<HttpPostedFileBase> images, HttpPostedFileBase imageAvatar)
        {
            var sp = db.sanPhams.FirstOrDefault(m => m.maSP == idProduct);
            if (sp == null)
            {
                UpdateInterface(checkStatus);
                return View("Index");
            }

            // Nếu có dùng DataAnnotations trong ProductVM thì nên check ModelState
            if (!ModelState.IsValid)
            {
                UpdateInterface(checkStatus);
                return View("Index");
            }

            sp.tenSP = pvm.NameProduct;
            sp.moTaTomTat = pvm.SummaryDescription;
            sp.moTa = pvm.Description;
            sp.giaBan = pvm.Price;
            sp.giamGia = pvm.Discount;
            // Không dùng kích thước nữa vì đã chuyển sang bán mắt kính
            // Giữ nguyên kích thước cũ nếu có trong DB
            // sp.kichThuoc = sp.kichThuoc;

            sp.maLoai = pvm.IDTypeProduct;
            sp.maThuongHieu = pvm.IDBrand;
            sp.trangThai = pvm.StatusProduct;
            sp.gioiTinh = (pvm.Sex != null && pvm.Sex.Equals("Nam"));

            // Cập nhật danh sách ảnh chi tiết nếu có upload mới
            if (images != null && images.Any(f => f != null && f.ContentLength > 0))
            {
                // Xóa ảnh cũ trong DB (nếu bạn muốn xóa cả file trên ổ đĩa thì bổ sung thêm)
                DeleteImgInDatabase(idProduct);

                foreach (var image in images)
                {
                    if (image != null && image.ContentLength > 0)
                    {
                        db.danhSachHinhs.Add(ImageProduct(image, sp));
                    }
                }
            }

            // Cập nhật ảnh đại diện nếu có
            if (imageAvatar != null && imageAvatar.ContentLength > 0)
            {
                sp.hinhDD = ImageProduct(imageAvatar, sp).hinhSP;
            }

            db.SaveChanges();
            UpdateInterface(checkStatus);
            return View("Index");
        }

        // ================== XÓA ẢNH TRONG DB ==================

        /// <summary>
        /// Hàm này dùng để xóa tất cả hình ảnh (record) trong bảng DanhSachHinhs theo mã sản phẩm
        /// </summary>
        private void DeleteImgInDatabase(string id)
        {
            var list = db.danhSachHinhs.Where(m => m.maSP == id).ToList();
            foreach (var rmImg in list)
            {
                db.danhSachHinhs.Remove(rmImg);
            }
        }

        // ================== LƯU ẢNH SẢN PHẨM ==================

        /// <summary>
        /// Hàm này dùng để thêm ảnh vào sản phẩm và trả về record danhSachHinh
        /// </summary>
        public danhSachHinh ImageProduct(HttpPostedFileBase image, sanPham sp)
        {
            if (image != null && sp != null && image.ContentLength > 0)
            {
                string virtualPath = "/Asset/SaveImgProduct/";
                string fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);

                string physicalPath = Server.MapPath("~" + virtualPath);
                if (!Directory.Exists(physicalPath))
                {
                    Directory.CreateDirectory(physicalPath);
                }

                string fullPath = Path.Combine(physicalPath, fileName);
                image.SaveAs(fullPath);

                return new danhSachHinh
                {
                    hinhSP = virtualPath + fileName,
                    maSP = sp.maSP
                };
            }

            // Trường hợp không có ảnh
            return new danhSachHinh
            {
                hinhSP = "",
                maSP = sp?.maSP
            };
        }

        // ================== XÓA SẢN PHẨM ==================

        public ActionResult Delete(string idProduct)
        {
            var pro = db.sanPhams.FirstOrDefault(m => m.maSP == idProduct);
            var detailOrder = db.chiTietDonHangs.Where(m => m.maSP == idProduct).ToList();

            if (pro != null)
            {
                if (detailOrder.Count > 0)
                {
                    foreach (var item in detailOrder)
                    {
                        db.chiTietDonHangs.Remove(item);
                    }
                }

                DeleteImgInDatabase(pro.maSP);
                db.sanPhams.Remove(pro);
                db.SaveChanges();
            }

            UpdateInterface(checkStatus);
            return View("Index");
        }

        // ================== CẬP NHẬT DỮ LIỆU HIỂN THỊ ==================

        /// <summary>
        /// Cập nhật lại các thông tin trong danh sách như danh sách loại sản phẩm, thương hiệu, sản phẩm
        /// </summary>
        private void UpdateInterface(bool check)
        {
            ViewBag.HeaderManagerProduct = (check ? "Danh sách còn hàng" : "Danh sách hết hàng");
            ViewData["listCategories"] = db.loaiSPs.ToList();
            ViewData["listBrand"] = db.thuongHieux.ToList();
            ViewData["listProduct"] = db.sanPhams.Where(m => m.trangThai == check).ToList();
        }
    }
}
