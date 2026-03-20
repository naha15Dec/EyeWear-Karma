using System.Collections.Generic;
using MatKinh.Models;

namespace MatKinh.ViewModel
{
    public class AdminWebsiteSettingVm
    {
        public ThongTinCuaHang CurrentInfo { get; set; }
        public List<ThongTinCuaHang> History { get; set; }

        public string TenCuaHang { get; set; }
        public string Hotline { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
        public string MoTaNgan { get; set; }
        public string GioiThieu { get; set; }
        public string Logo { get; set; }
        public string Banner { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string ZaloUrl { get; set; }
        public bool IsActive { get; set; }
    }
}