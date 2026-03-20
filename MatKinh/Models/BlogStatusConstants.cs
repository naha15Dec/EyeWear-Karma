namespace MatKinh.Models
{
    public static class BlogStatusConstants
    {
        public const int DRAFT = 1;
        public const int PUBLISHED = 2;
        public const int HIDDEN = 3;

        public static string GetName(int status)
        {
            switch (status)
            {
                case DRAFT: return "Nháp";
                case PUBLISHED: return "Đã đăng";
                case HIDDEN: return "Ẩn";
                default: return "Không xác định";
            }
        }
    }
}