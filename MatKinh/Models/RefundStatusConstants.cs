namespace MatKinh.Models
{
    public static class RefundStatusConstants
    {
        public const int NOT_REFUNDED = 1;       // Chưa hoàn
        public const int MANUAL_REQUIRED = 2;    // Cần hoàn thủ công
        public const int REFUNDED = 3;           // Đã hoàn
        public const int NO_REFUND = 4;          // Không hoàn

        public static string GetName(int status)
        {
            switch (status)
            {
                case NOT_REFUNDED:
                    return "Chưa hoàn tiền";

                case MANUAL_REQUIRED:
                    return "Cần hoàn thủ công";

                case REFUNDED:
                    return "Đã hoàn tiền";

                case NO_REFUND:
                    return "Không hoàn tiền";

                default:
                    return "Không xác định";
            }
        }
    }
}