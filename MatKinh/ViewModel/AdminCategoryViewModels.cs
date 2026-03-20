using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MatKinh.ViewModel
{
    public class AdminCategoryIndexVm
    {
        public List<AdminCategoryListItemVm> Categories { get; set; } = new List<AdminCategoryListItemVm>();
        public AdminCategoryEditVm Form { get; set; } = new AdminCategoryEditVm();
    }

    public class AdminCategoryListItemVm
    {
        public int LoaiSanPhamId { get; set; }
        public string MaLoaiSanPham { get; set; }
        public string TenLoaiSanPham { get; set; }
        public string MoTa { get; set; }
        public bool IsActive { get; set; }
        public int SoSanPham { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AdminCategoryEditVm
    {
        public int? LoaiSanPhamId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã loại sản phẩm")]
        [StringLength(50)]
        public string MaLoaiSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên loại sản phẩm")]
        [StringLength(200)]
        public string TenLoaiSanPham { get; set; }

        public string MoTa { get; set; }
        public bool IsActive { get; set; } = true;
    }
}