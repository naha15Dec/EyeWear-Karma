using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace BanGiay.ViewModel
{
    public class ProductVM
    {
        // TÊN SẢN PHẨM
        [Required(ErrorMessage = "Không được bỏ trống tên sản phẩm")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không quá 100 ký tự")]
        public string NameProduct { get; set; }

        // GIÁ BÁN
        [Required(ErrorMessage = "Không được bỏ trống giá tiền")]
        [Range(0, int.MaxValue, ErrorMessage = "Giá tiền phải là số không âm")]
        public int Price { get; set; }

        // NGÀY ĐĂNG (hệ thống set, không bắt buộc người dùng nhập)
        public DateTime? DateProduct { get; set; }

        // GIẢM GIÁ
        [Range(0, int.MaxValue, ErrorMessage = "Giảm giá phải là số không âm")]
        public int Discount { get; set; }

        // MÔ TẢ TÓM TẮT
        [Required(ErrorMessage = "Không được bỏ trống mô tả tóm tắt")]
        [StringLength(1500, ErrorMessage = "Mô tả tóm tắt không quá 1500 ký tự")]
        public string SummaryDescription { get; set; }

        // MÔ TẢ CHI TIẾT
        [Required(ErrorMessage = "Không được bỏ trống mô tả chi tiết")]
        [StringLength(3500, ErrorMessage = "Nội dung sản phẩm không quá 3500 ký tự")]
        [AllowHtml]
        public string Description { get; set; }

        // ẢNH ĐẠI DIỆN (đường dẫn sau khi lưu)
        public string Image { get; set; }

        // TÀI KHOẢN ĐĂNG SẢN PHẨM (set từ Session, không cho nhập)
        public string Account { get; set; }

        // TRẠNG THÁI SẢN PHẨM (đang bán / ngừng bán)
        [Display(Name = "Trạng thái")]
        public bool StatusProduct { get; set; }

        // LOẠI SẢN PHẨM (gọng, kính râm, kính cận,…)
        [Required(ErrorMessage = "Vui lòng chọn loại sản phẩm")]
        [Display(Name = "Loại sản phẩm")]
        public int IDTypeProduct { get; set; }

        // THƯƠNG HIỆU (Karma, RayBan,…)
        [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
        [Display(Name = "Thương hiệu")]
        public int IDBrand { get; set; }

        // GIỚI TÍNH (Nam / Nữ / Unisex)
        [Required(ErrorMessage = "Vui lòng chọn giới tính phù hợp của sản phẩm")]
        [Display(Name = "Giới tính")]
        public string Sex { get; set; }
    }
}
