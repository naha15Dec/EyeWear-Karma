namespace MatKinh.Models
{
    public static class BlogStatusConstants
    {
        public const int DRAFT = 1;       // Chờ duyệt / nháp
        public const int PUBLISHED = 2;   // Đã đăng
        public const int HIDDEN = 3;      // Đã ẩn

        public static string GetName(int status)
        {
            switch (status)
            {
                case DRAFT:
                    return "Chờ duyệt";

                case PUBLISHED:
                    return "Đã đăng";

                case HIDDEN:
                    return "Ẩn";

                default:
                    return "Không xác định";
            }
        }
    }
}