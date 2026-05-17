using System;
using System.Collections.Generic;

namespace MatKinh.ViewModel
{
    public class AdminReviewIndexVm
    {
        public string Keyword { get; set; }
        public int? Rating { get; set; }
        public int? Status { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public int TotalReviews { get; set; }
        public int VisibleReviews { get; set; }
        public int HiddenReviews { get; set; }
        public decimal AverageRating { get; set; }

        public bool HasPreviousPage
        {
            get { return CurrentPage > 1; }
        }

        public bool HasNextPage
        {
            get { return CurrentPage < TotalPages; }
        }

        public List<AdminReviewListItemVm> Reviews { get; set; } = new List<AdminReviewListItemVm>();
    }

    public class AdminReviewListItemVm
    {
        public int DanhGiaId { get; set; }

        public int SanPhamId { get; set; }
        public string MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnhChinh { get; set; }

        public int KhachHangId { get; set; }
        public string TenKhachHang { get; set; }
        public string EmailKhachHang { get; set; }

        public byte SoSao { get; set; }
        public string NoiDung { get; set; }

        public int TrangThai { get; set; }
        public string PhanHoiAdmin { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? NgayDuyet { get; set; }

        public string NguoiXuLy { get; set; }
    }
}