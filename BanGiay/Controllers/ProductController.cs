using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;               // <-- để dùng AsNoTracking(), Include()
using BanGiay.Models;
using BanGiay.ViewModel;

namespace BanGiay.Controllers
{
    public class ProductController : Controller
    {
        // KHÔNG dùng static DbContext
        private readonly DoAnLTW2Entities _db = new DoAnLTW2Entities();

        // Trang danh sách sản phẩm
        public ActionResult Index(int page = 1, int pageSize = 9)
        {
            _db.Database.CommandTimeout = 30; // tránh treo khi DB chậm

            var query = _db.sanPhams
                           .AsNoTracking()
                           .OrderBy(x => x.maSP);

            int total = query.Count();

            var items = query
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();

            PaginationProductPage(total, page, pageSize);
            ViewData["listProduct"] = items;

            UpdateInterface();
            return View();
        }

        /// <summary>Chi tiết sản phẩm</summary>
        public ActionResult DetailProduct(string idProduct)
        {
            if (string.IsNullOrWhiteSpace(idProduct))
                return RedirectToAction("Index", "Home");

            _db.Database.CommandTimeout = 30;

            var product = _db.sanPhams
                             .AsNoTracking()
                             .Include(p => p.danhSachHinhs)
                             .Include(p => p.loaiSP)
                             .Include(p => p.thuongHieu)
                             .FirstOrDefault(m => m.maSP == idProduct);

            if (product == null) return RedirectToAction("Index", "Home");
            return View(product);
        }

        /// <summary>Lọc sản phẩm (dùng chung cho GET và POST)</summary>
        public ActionResult CheckFilters(FiltersProduct fd, int page = 1, int pageSize = 9)
        {
            _db.Database.CommandTimeout = 30;

            int price1 = 0, price2 = int.MaxValue;

            // lưu lại lựa chọn để hiển thị
            ViewData["selectedBrand"] = fd?.SelectedBrand;
            ViewData["selectedTypeProduct"] = fd?.SelectedTypeProduct;
            ViewData["selectedPrice"] = fd?.SelectedPrice;

            // Xử lý khoảng giá
            if (!string.IsNullOrEmpty(fd?.SelectedPrice))
            {
                int price = int.Parse(fd.SelectedPrice);
                if (price == 500000) { price1 = 0; price2 = 500000; }
                else if (price == 3000000) { price1 = 500000; price2 = 3000000; }
                else if (price == 5000000) { price1 = 3000000; price2 = 5000000; }
                else if (price >= 10000000) { price1 = 10000000; price2 = 50000000; }
            }

            var query = _db.sanPhams
                           .AsNoTracking()
                           .Include(p => p.loaiSP)
                           .Include(p => p.thuongHieu)
                           .Where(m => m.giaBan >= price1 && m.giaBan <= price2);

            // Thương hiệu
            if (!string.IsNullOrWhiteSpace(fd?.SelectedBrand))
                query = query.Where(m => m.thuongHieu.tenThuongHieu.Contains(fd.SelectedBrand));

            // Loại sản phẩm
            if (!string.IsNullOrWhiteSpace(fd?.SelectedTypeProduct))
                query = query.Where(m => m.loaiSP.tenLoai.Contains(fd.SelectedTypeProduct));

            // Tên sản phẩm
            if (!string.IsNullOrWhiteSpace(fd?.NameProduct))
                query = query.Where(m => m.tenSP.Contains(fd.NameProduct));

            int total = query.Count();

            var items = query.OrderBy(x => x.maSP)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToList();

            PaginationProductPage(total, page, pageSize);
            ViewData["listProduct"] = items;

            UpdateInterface();
            return View("Index");
        }

        /// <summary>Tìm nhanh theo mã</summary>
        public ActionResult FindProductById(string idProduct)
        {
            return RedirectToAction("DetailProduct", "Product", new { idProduct });
        }

        /// <summary>
        /// Tính thông số phân trang (không kéo toàn bộ list về)
        /// </summary>
        private void PaginationProductPage(int totalCount, int page, int pageSize)
        {
            int noOfPages = (int)Math.Ceiling((double)totalCount / pageSize);
            int displayStart = (page < 5 ? 0 : (((page - 1) >= (noOfPages - 5)) ? (noOfPages - 5) : (page - 1)));
            int noOfPagesToShow = (page >= 5)
                                  ? ((page + 4 > noOfPages) ? noOfPages : (page + 4))
                                  : (page >= 5 ? 5 : noOfPages);

            ViewBag.Page = page;
            ViewBag.NoOfPages = noOfPagesToShow;
            ViewBag.DisplayPage = displayStart;
        }

        /// <summary>Cập nhật danh mục/ thương hiệu cho sidebar</summary>
        private void UpdateInterface()
        {
            ViewData["brandList"] = _db.thuongHieux.AsNoTracking().ToList();
            ViewData["typeProductList"] = _db.loaiSPs.AsNoTracking().ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
