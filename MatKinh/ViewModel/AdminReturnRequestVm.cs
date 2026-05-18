using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    public class AdminReturnRequestIndexVm
    {
        public string Keyword { get; set; }
        public int? Status { get; set; }

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ProcessingRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int RejectedRequests { get; set; }

        public decimal TotalExpectedRefund { get; set; }

        public bool HasPreviousPage
        {
            get { return CurrentPage > 1; }
        }

        public bool HasNextPage
        {
            get { return CurrentPage < TotalPages; }
        }

        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ShipperOptions { get; set; } = new List<SelectListItem>();
        public List<AdminReturnRequestListItemVm> Requests { get; set; } = new List<AdminReturnRequestListItemVm>();
    }

    public class AdminReturnRequestListItemVm
    {
        public int YeuCauTraHangId { get; set; }
        public string MaYeuCau { get; set; }

        public int DonHangId { get; set; }
        public string MaDonHang { get; set; }

        public int KhachHangId { get; set; }
        public string TenKhachHang { get; set; }
        public string SoDienThoaiKhachHang { get; set; }
        public string EmailKhachHang { get; set; }

        public string LyDo { get; set; }
        public string GhiChuKhachHang { get; set; }
        public string GhiChuAdmin { get; set; }
        public string GhiChuShipper { get; set; }

        public int TrangThai { get; set; }
        public int TrangThaiHoanTien { get; set; }

        public int? ShipperId { get; set; }
        public string ShipperName { get; set; }

        public int? DuyetBoiId { get; set; }
        public string DuyetBoiName { get; set; }

        public DateTime NgayYeuCau { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public DateTime? NgayGanShipper { get; set; }
        public DateTime? NgayShipperBatDauLay { get; set; }
        public DateTime? NgayShipperLayHang { get; set; }
        public DateTime? NgayBanGiaoVeCuaHang { get; set; }
        public DateTime? NgayNhanHangVe { get; set; }
        public DateTime? NgayHoanTien { get; set; }

        public decimal TongTienHoanDuKien { get; set; }
        public decimal? TongTienHoanThucTe { get; set; }

        public int TotalReturnQuantity { get; set; }
        public int TotalProductLines { get; set; }

        public string ProductSummary { get; set; }

        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanAssignShipper { get; set; }
        public bool CanReceive { get; set; }
        public bool CanRefund { get; set; }
    }
}