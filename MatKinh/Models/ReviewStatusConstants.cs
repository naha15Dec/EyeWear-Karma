namespace MatKinh.Models
{
    public static class ReviewStatusConstants
    {
        public const int PENDING = 1;   // Chờ duyệt
        public const int APPROVED = 2;  // Đã duyệt
        public const int REJECTED = 3;  // Từ chối

        public static string GetName(int status)
        {
            switch (status)
            {
                case PENDING: return "Chờ duyệt";
                case APPROVED: return "Đã duyệt";
                case REJECTED: return "Từ chối";
                default: return "Không xác định";
            }
        }
    }
}
