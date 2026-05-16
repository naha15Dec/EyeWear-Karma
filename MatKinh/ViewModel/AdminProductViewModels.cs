using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    public class AdminProductIndexVm
    {
        public string HeaderTitle { get; set; }
        public string StatusFilter { get; set; }
        public string Keyword { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public int TotalStock { get; set; }
        public int TotalFeatured { get; set; }

        public bool HasPreviousPage
        {
            get { return CurrentPage > 1; }
        }

        public bool HasNextPage
        {
            get { return CurrentPage < TotalPages; }
        }

        public List<AdminProductListItemVm> Products { get; set; } = new List<AdminProductListItemVm>();
        public List<SelectListItem> Brands { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> FrameTypes { get; set; } = new List<SelectListItem>();
    }

    public class AdminProductListItemVm
    {
        public int SanPhamId { get; set; }
        public string MaSanPham { get; set; }
        public string TenSanPham { get; set; }

        public int ThuongHieuId { get; set; }
        public string ThuongHieuTen { get; set; }

        public int LoaiSanPhamId { get; set; }
        public string LoaiSanPhamTen { get; set; }

        public int? KieuGongId { get; set; }
        public string MaKieuGong { get; set; }
        public string TenKieuGong { get; set; }

        public decimal GiaGoc { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongTon { get; set; }

        public string HinhAnhChinh { get; set; }
        public string MoTaNgan { get; set; }
        public string MoTaChiTiet { get; set; }

        public int TrangThai { get; set; }
        public bool IsFeatured { get; set; }
        public string NguoiTao { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AdminProductEditVm
    {
        public int SanPhamId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã sản phẩm")]
        public string MaSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string TenSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
        public int ThuongHieuId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại sản phẩm")]
        public int LoaiSanPhamId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kiểu gọng")]
        public int? KieuGongId { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá gốc không hợp lệ")]
        public decimal GiaGoc { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá bán không hợp lệ")]
        public decimal GiaBan { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn không hợp lệ")]
        public int SoLuongTon { get; set; }

        public string MoTaNgan { get; set; }
        public string MoTaChiTiet { get; set; }

        public int TrangThai { get; set; }
        public bool IsFeatured { get; set; }

        public string StatusFilter { get; set; }
        public string Keyword { get; set; }
        public int Page { get; set; }
    }
}