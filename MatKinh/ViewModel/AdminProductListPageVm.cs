using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    public class AdminProductListPageVm
    {
        public string Keyword { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage
        {
            get { return CurrentPage > 1; }
        }

        public bool HasNextPage
        {
            get { return CurrentPage < TotalPages; }
        }

        public List<AdminProductListPageItemVm> Products { get; set; } = new List<AdminProductListPageItemVm>();
        public List<SelectListItem> Brands { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> FrameTypes { get; set; } = new List<SelectListItem>();
    }

    public class AdminProductListPageItemVm
    {
        public int SanPhamId { get; set; }
        public string MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public string MoTaNgan { get; set; }
        public string MoTaChiTiet { get; set; }
        public string HinhAnhChinh { get; set; }

        public decimal GiaGoc { get; set; }
        public decimal GiaBan { get; set; }
        public int SoLuongTon { get; set; }

        public int ThuongHieuId { get; set; }
        public string ThuongHieuTen { get; set; }

        public int LoaiSanPhamId { get; set; }
        public string LoaiSanPhamTen { get; set; }

        public int TrangThai { get; set; }
        public bool IsFeatured { get; set; }

        public string NguoiTao { get; set; }

        public int? KieuGongId { get; set; }
        public string MaKieuGong { get; set; }
        public string TenKieuGong { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}