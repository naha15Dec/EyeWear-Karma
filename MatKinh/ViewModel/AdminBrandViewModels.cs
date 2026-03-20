using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MatKinh.ViewModel
{
    public class AdminBrandIndexVm
    {
        public List<AdminBrandListItemVm> Brands { get; set; } = new List<AdminBrandListItemVm>();
        public AdminBrandEditVm Form { get; set; } = new AdminBrandEditVm();
    }

    public class AdminBrandListItemVm
    {
        public int ThuongHieuId { get; set; }
        public string MaThuongHieu { get; set; }
        public string TenThuongHieu { get; set; }
        public string MoTa { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int SoSanPham { get; set; }
    }

    public class AdminBrandEditVm
    {
        public int? ThuongHieuId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã thương hiệu")]
        [StringLength(50)]
        public string MaThuongHieu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu")]
        [StringLength(200)]
        public string TenThuongHieu { get; set; }

        public string MoTa { get; set; }
        public bool IsActive { get; set; } = true;
    }
}