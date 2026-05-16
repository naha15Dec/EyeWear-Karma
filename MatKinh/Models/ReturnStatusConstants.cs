namespace MatKinh.Models
{
    public static class ReturnStatusConstants
    {
        public const int PENDING = 1;           // Chờ duyệt
        public const int APPROVED = 2;          // Đã duyệt, chờ shipper lấy
        public const int SHIPPER_PICKING = 3;   // Shipper đang lấy hàng
        public const int RECEIVED = 4;          // Đã nhận hàng về kho
        public const int REJECTED = 5;          // Từ chối

        public static string GetName(int status)
        {
            switch (status)
            {
                case PENDING: return "Chờ duyệt";
                case APPROVED: return "Đã duyệt - Chờ shipper lấy";
                case SHIPPER_PICKING: return "Shipper đang lấy hàng";
                case RECEIVED: return "Đã nhận hàng về";
                case REJECTED: return "Từ chối";
                default: return "Không xác định";
            }
        }
    }
}
