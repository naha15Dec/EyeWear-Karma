using System;
using System.Collections.Generic;

namespace MatKinh.ViewModel
{
    // ViewModel để khách hàng gửi đánh giá
    public class SubmitReviewVm
    {
        public int ChiTietDonHangId { get; set; }
        public string MaDonHang { get; set; }
        public byte SoSao { get; set; }
        public string NoiDung { get; set; }
    }

    // Hiển thị đánh giá trên trang chi tiết sản phẩm (đã duyệt)
    public class ProductReviewItemVm
    {
        public int DanhGiaId { get; set; }
        public string TenKhachHang { get; set; }
        public byte SoSao { get; set; }
        public string NoiDung { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Admin: danh sách đánh giá chờ duyệt / đã duyệt
    public class AdminReviewIndexVm
    {
        public List<AdminReviewItemVm> Reviews { get; set; }
        public int StatusFilter { get; set; }
        public string Keyword { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class AdminReviewItemVm
    {
        public int DanhGiaId { get; set; }
        public int ChiTietDonHangId { get; set; }
        public int SanPhamId { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnhChinh { get; set; }
        public string TenKhachHang { get; set; }
        public byte SoSao { get; set; }
        public string NoiDung { get; set; }
        public int TrangThai { get; set; }
        public string TrangThaiText { get; set; }
        public string LyDoTuChoi { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string DuyetBoiName { get; set; }
    }

    // Admin: duyệt / từ chối đánh giá
    public class AdminReviewActionVm
    {
        public int DanhGiaId { get; set; }
        public string LyDoTuChoi { get; set; }
    }
}
