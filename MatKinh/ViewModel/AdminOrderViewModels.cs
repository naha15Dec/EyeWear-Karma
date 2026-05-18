using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    public class AdminOrderIndexVm
    {
        public string Keyword { get; set; }
        public int? StatusFilter { get; set; }
        public string HeaderTitle { get; set; }

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

        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public List<AdminOrderListItemVm> Orders { get; set; } = new List<AdminOrderListItemVm>();
    }

    public class AdminOrderListItemVm
    {
        public int DonHangId { get; set; }
        public string MaDonHang { get; set; }
        public string TenKhachHang { get; set; }
        public string HoTenNguoiNhan { get; set; }
        public string SoDienThoaiNguoiNhan { get; set; }
        public string DiaChiNhanHang { get; set; }

        public decimal TongTienHang { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongThanhToan { get; set; }

        public int TrangThai { get; set; }
        public string TrangThaiText { get; set; }

        public string PhuongThucThanhToan { get; set; }
        public string TrangThaiThanhToan { get; set; }
        public string MaGiaoDichThanhToan { get; set; }
        public DateTime? NgayThanhToan { get; set; }
        public bool CanRequireManualRefund { get; set; }

        public string NguoiTao { get; set; }
        public string NguoiXacNhan { get; set; }
        public string ShipperName { get; set; }

        public DateTime NgayDat { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public DateTime? NgayGiao { get; set; }
        public DateTime? NgayHoanTat { get; set; }
        public DateTime? NgayHuy { get; set; }

        public int SoLuongSanPham { get; set; }
    }

    public class AdminOrderDetailVm
    {
        public int DonHangId { get; set; }
        public string MaDonHang { get; set; }

        public int KhachHangId { get; set; }
        public string TenKhachHang { get; set; }

        public int? ShipperId { get; set; }
        public string ShipperName { get; set; }

        public int? ConfirmedById { get; set; }
        public string ConfirmedByName { get; set; }

        public int? CreatedById { get; set; }
        public string CreatedByName { get; set; }

        public string HoTenNguoiNhan { get; set; }
        public string SoDienThoaiNguoiNhan { get; set; }
        public string DiaChiNhanHang { get; set; }
        public string GhiChu { get; set; }

        public decimal TongTienHang { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongThanhToan { get; set; }

        public int TrangThai { get; set; }
        public string TrangThaiText { get; set; }

        public string PhuongThucThanhToan { get; set; }
        public string TrangThaiThanhToan { get; set; }
        public string MaGiaoDichThanhToan { get; set; }
        public DateTime? NgayThanhToan { get; set; }
        public bool CanRequireManualRefund { get; set; }

        public DateTime NgayDat { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public DateTime? NgayGiao { get; set; }
        public DateTime? NgayHoanTat { get; set; }
        public DateTime? NgayHuy { get; set; }

        public List<AdminOrderDetailItemVm> Items { get; set; } = new List<AdminOrderDetailItemVm>();
        public List<AdminOrderHistoryVm> Histories { get; set; } = new List<AdminOrderHistoryVm>();
        public List<SelectListItem> Shippers { get; set; } = new List<SelectListItem>();
    }

    public class AdminOrderDetailItemVm
    {
        public int ChiTietDonHangId { get; set; }
        public int SanPhamId { get; set; }
        public string TenSanPhamSnapshot { get; set; }
        public decimal DonGiaSnapshot { get; set; }
        public int SoLuong { get; set; }
        public decimal GiamGiaSnapshot { get; set; }
        public decimal ThanhTien { get; set; }
        public string HinhAnhChinh { get; set; }
        public string MaSanPham { get; set; }
    }

    public class AdminOrderHistoryVm
    {
        public int LichSuTrangThaiDonHangId { get; set; }
        public int TrangThaiCu { get; set; }
        public string TrangThaiCuText { get; set; }
        public int TrangThaiMoi { get; set; }
        public string TrangThaiMoiText { get; set; }
        public string GhiChu { get; set; }
        public string NguoiCapNhat { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminOrderUpdateStatusVm
    {
        [Required]
        public int DonHangId { get; set; }

        [Required]
        public int TrangThaiMoi { get; set; }

        public string GhiChu { get; set; }
    }

    public class AdminOrderAssignShipperVm
    {
        [Required]
        public int DonHangId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn shipper giao hàng.")]
        public int? ShipperId { get; set; }

        public string GhiChu { get; set; }
    }
}