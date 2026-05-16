using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    // Khách hàng: form gửi yêu cầu trả hàng
    public class SubmitReturnVm
    {
        public string MaDonHang { get; set; }
        public string LyDo { get; set; }
        public string GhiChuKhachHang { get; set; }
        public List<ReturnItemVm> Items { get; set; }
    }

    public class ReturnItemVm
    {
        public int ChiTietDonHangId { get; set; }
        public int SoLuongTra { get; set; }
        public string LyDoChiTiet { get; set; }
    }

    // Khách hàng: xem chi tiết yêu cầu trả hàng của mình
    public class UserReturnDetailVm
    {
        public int YeuCauId { get; set; }
        public string MaYeuCau { get; set; }
        public string MaDonHang { get; set; }
        public string LyDo { get; set; }
        public string GhiChuKhachHang { get; set; }
        public int TrangThai { get; set; }
        public string TrangThaiText { get; set; }
        public string GhiChuAdmin { get; set; }
        public DateTime NgayYeuCau { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public List<UserReturnItemDetailVm> Items { get; set; }
    }

    public class UserReturnItemDetailVm
    {
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public int SoLuongTra { get; set; }
        public string LyDoChiTiet { get; set; }
    }

    // Admin: danh sách yêu cầu trả hàng
    public class AdminReturnIndexVm
    {
        public List<AdminReturnItemVm> Returns { get; set; }
        public int? StatusFilter { get; set; }
        public string Keyword { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public List<SelectListItem> StatusOptions { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class AdminReturnItemVm
    {
        public int YeuCauId { get; set; }
        public string MaYeuCau { get; set; }
        public string MaDonHang { get; set; }
        public string TenKhachHang { get; set; }
        public string LyDo { get; set; }
        public int TrangThai { get; set; }
        public string TrangThaiText { get; set; }
        public DateTime NgayYeuCau { get; set; }
        public string ShipperName { get; set; }
    }

    // Admin: chi tiết yêu cầu trả hàng
    public class AdminReturnDetailVm
    {
        public int YeuCauId { get; set; }
        public string MaYeuCau { get; set; }
        public int DonHangId { get; set; }
        public string MaDonHang { get; set; }
        public string TenKhachHang { get; set; }
        public string LyDo { get; set; }
        public string GhiChuKhachHang { get; set; }
        public int TrangThai { get; set; }
        public string TrangThaiText { get; set; }
        public string GhiChuAdmin { get; set; }
        public int? ShipperId { get; set; }
        public string ShipperName { get; set; }
        public string DuyetBoiName { get; set; }
        public DateTime NgayYeuCau { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public DateTime? NgayShipperLay { get; set; }
        public DateTime? NgayNhanVe { get; set; }
        public bool IsVnPayPaid { get; set; }
        public List<AdminReturnItemDetailVm> Items { get; set; }
        public List<SelectListItem> Shippers { get; set; }
    }

    public class AdminReturnItemDetailVm
    {
        public int ChiTietTraHangId { get; set; }
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public int SoLuongTra { get; set; }
        public string LyDoChiTiet { get; set; }
    }

    // Admin: duyệt / từ chối / cập nhật trạng thái
    public class AdminReturnActionVm
    {
        public int YeuCauId { get; set; }
        public int TrangThaiMoi { get; set; }
        public int? ShipperId { get; set; }
        public string GhiChuAdmin { get; set; }
    }
}
