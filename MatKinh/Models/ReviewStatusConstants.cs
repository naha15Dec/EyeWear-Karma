namespace MatKinh.Models
{
    public static class ReviewStatusConstants
    {
        public const int PENDING = 1;   // Dự phòng
        public const int APPROVED = 2;  // Đang hiển thị
        public const int HIDDEN = 3;    // Đã bị admin/cửa hàng ẩn

        // Dùng cho phía khách hàng
        public static string GetName(int status)
        {
            switch (status)
            {
                case PENDING:
                    return "Chờ xử lý";

                case APPROVED:
                    return "Đã đánh giá";

                case HIDDEN:
                    return "Đã bị cửa hàng ẩn";

                default:
                    return "Không xác định";
            }
        }

        // Dùng cho phía admin
        public static string GetAdminName(int status)
        {
            switch (status)
            {
                case PENDING:
                    return "Chờ xử lý";

                case APPROVED:
                    return "Đang hiển thị";

                case HIDDEN:
                    return "Đã ẩn";

                default:
                    return "Không xác định";
            }
        }
    }
}