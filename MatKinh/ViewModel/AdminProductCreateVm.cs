using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace MatKinh.ViewModel
{
    public class AdminProductCreateVm
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        public string TenSanPham { get; set; }

        public string MoTaNgan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết")]
        public string MoTaChiTiet { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá gốc không hợp lệ")]
        public decimal GiaGoc { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá bán không hợp lệ")]
        public decimal GiaBan { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn không hợp lệ")]
        public int SoLuongTon { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
        public int ThuongHieuId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại sản phẩm")]
        public int LoaiSanPhamId { get; set; }

        public int TrangThai { get; set; }
        public bool IsFeatured { get; set; }

        public List<SelectListItem> Brands { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    }
}