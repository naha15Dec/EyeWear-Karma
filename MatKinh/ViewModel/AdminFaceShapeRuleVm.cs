using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    public class AdminFaceShapeRuleIndexVm
    {
        public AdminFaceShapeRuleIndexVm()
        {
            Rules = new List<AdminFaceShapeRuleListItemVm>();
            FaceShapeOptions = new List<SelectListItem>();
            FrameTypeOptions = new List<SelectListItem>();
            Form = new AdminFaceShapeRuleEditVm
            {
                IsActive = true,
                DiemPhuHop = 90
            };

            CurrentPage = 1;
            PageSize = 10;
        }

        public List<AdminFaceShapeRuleListItemVm> Rules { get; set; }

        public List<SelectListItem> FaceShapeOptions { get; set; }

        public List<SelectListItem> FrameTypeOptions { get; set; }

        public AdminFaceShapeRuleEditVm Form { get; set; }

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

    public class AdminFaceShapeRuleListItemVm
    {
        public int RuleId { get; set; }

        public string MaHinhDangMat { get; set; }

        public string TenHinhDangMat { get; set; }

        public int KieuGongId { get; set; }

        public string MaKieuGong { get; set; }

        public string TenKieuGong { get; set; }

        public int DiemPhuHop { get; set; }

        public string GiaiThich { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public class AdminFaceShapeRuleEditVm
    {
        public int? RuleId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn dáng mặt.")]
        [StringLength(20, ErrorMessage = "Mã dáng mặt tối đa 20 ký tự.")]
        public string MaHinhDangMat { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn kiểu gọng.")]
        public int? KieuGongId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập điểm phù hợp.")]
        [Range(0, 100, ErrorMessage = "Điểm phù hợp phải từ 0 đến 100.")]
        public int DiemPhuHop { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giải thích.")]
        [StringLength(500, ErrorMessage = "Giải thích tối đa 500 ký tự.")]
        public string GiaiThich { get; set; }

        public bool IsActive { get; set; }
    }
}