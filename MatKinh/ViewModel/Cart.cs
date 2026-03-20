using System;

namespace MatKinh.ViewModel
{
    public class Cart
    {
        public int SanPhamId { get; set; }

        public string TenSanPham { get; set; }

        public string HinhAnh { get; set; }

        public decimal DonGia { get; set; }

        public decimal GiamGia { get; set; }

        public int SoLuong { get; set; }

        public decimal ThanhTien
        {
            get { return (DonGia - GiamGia) * SoLuong; }
        }
    }
}