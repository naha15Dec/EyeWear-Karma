using System;
using System.Collections.Generic;

namespace MatKinh.ViewModel
{
    public class ShipperReturnRequestIndexVm
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public int AssignedCount { get; set; }
        public int PickingUpCount { get; set; }
        public int PickedUpCount { get; set; }
        public int HandedOverCount { get; set; }

        public bool HasPreviousPage
        {
            get { return CurrentPage > 1; }
        }

        public bool HasNextPage
        {
            get { return CurrentPage < TotalPages; }
        }

        public List<ShipperReturnRequestListItemVm> Requests { get; set; } = new List<ShipperReturnRequestListItemVm>();
    }

    public class ShipperReturnRequestListItemVm
    {
        public int YeuCauTraHangId { get; set; }
        public string MaYeuCau { get; set; }

        public int DonHangId { get; set; }
        public string MaDonHang { get; set; }

        public int KhachHangId { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoaiKhachHang { get; set; }
        public string EmailKhachHang { get; set; }

        public string HoTenNguoiNhan { get; set; }
        public string SoDienThoaiNguoiNhan { get; set; }
        public string DiaChiNhanHang { get; set; }

        public string LyDo { get; set; }
        public string GhiChuKhachHang { get; set; }
        public string GhiChuAdmin { get; set; }
        public string GhiChuShipper { get; set; }

        public int TrangThai { get; set; }
        public int TrangThaiHoanTien { get; set; }

        public DateTime NgayYeuCau { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public DateTime? NgayGanShipper { get; set; }
        public DateTime? NgayShipperBatDauLay { get; set; }
        public DateTime? NgayShipperLayHang { get; set; }
        public DateTime? NgayBanGiaoVeCuaHang { get; set; }

        public decimal TongTienHoanDuKien { get; set; }

        public int TotalProductLines { get; set; }
        public int TotalReturnQuantity { get; set; }
        public string ProductSummary { get; set; }

        public bool CanStartPickup { get; set; }
        public bool CanConfirmPickedUp { get; set; }
        public bool CanHandOver { get; set; }
    }
}