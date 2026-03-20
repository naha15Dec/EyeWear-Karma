namespace MatKinh.Models
{
    public static class OrderStatusConstants
    {
        public const int PENDING = 1;               // Chờ xác nhận
        public const int CONFIRMED = 2;             // Đã xác nhận
        public const int PREPARING = 3;             // Đang chuẩn bị
        public const int ASSIGNED_TO_SHIPPER = 4;   // Đã giao shipper
        public const int DELIVERING = 5;            // Đang giao
        public const int DELIVERED = 6;             // Giao thành công
        public const int DELIVERY_FAILED = 7;       // Giao thất bại
        public const int CANCELLED = 8;             // Đã hủy

        public static string GetName(int status)
        {
            switch (status)
            {
                case PENDING: return "Chờ xác nhận";
                case CONFIRMED: return "Đã xác nhận";
                case PREPARING: return "Đang chuẩn bị";
                case ASSIGNED_TO_SHIPPER: return "Đã giao shipper";
                case DELIVERING: return "Đang giao";
                case DELIVERED: return "Giao thành công";
                case DELIVERY_FAILED: return "Giao thất bại";
                case CANCELLED: return "Đã hủy";
                default: return "Không xác định";
            }
        }
    }
}