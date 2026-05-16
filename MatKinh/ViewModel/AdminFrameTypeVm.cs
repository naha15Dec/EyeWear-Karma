using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MatKinh.ViewModel
{
    public class AdminFrameTypeIndexVm
    {
        public AdminFrameTypeIndexVm()
        {
            FrameTypes = new List<AdminFrameTypeListItemVm>();
            Form = new AdminFrameTypeEditVm
            {
                IsActive = true
            };

            CurrentPage = 1;
            PageSize = 10;
        }

        public List<AdminFrameTypeListItemVm> FrameTypes { get; set; }

        public AdminFrameTypeEditVm Form { get; set; }

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
    }

    public class AdminFrameTypeListItemVm
    {
        public int KieuGongId { get; set; }

        public string MaKieuGong { get; set; }

        public string TenKieuGong { get; set; }

        public string MoTa { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int SoSanPham { get; set; }

        public int SoRule { get; set; }
    }

    public class AdminFrameTypeEditVm
    {
        public int? KieuGongId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã kiểu gọng.")]
        [StringLength(30, ErrorMessage = "Mã kiểu gọng tối đa 30 ký tự.")]
        public string MaKieuGong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên kiểu gọng.")]
        [StringLength(100, ErrorMessage = "Tên kiểu gọng tối đa 100 ký tự.")]
        public string TenKieuGong { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        public string MoTa { get; set; }

        public bool IsActive { get; set; }
    }
}