namespace MatKinh.Models
{
    public static class ReturnRequestStatusConstants
    {
        public const int PENDING = 1;              // Chờ xử lý
        public const int APPROVED = 2;             // Đã duyệt
        public const int ASSIGNED_TO_SHIPPER = 3;  // Đã giao shipper
        public const int PICKING_UP = 4;           // Shipper đang lấy hàng
        public const int PICKED_UP = 5;            // Shipper đã lấy hàng từ khách
        public const int HANDED_OVER = 6;          // Đã bàn giao về cửa hàng
        public const int RECEIVED = 7;             // Admin đã xác nhận nhận hàng
        public const int REFUNDED = 8;             // Đã hoàn tiền
        public const int REJECTED = 9;             // Từ chối
        public const int CANCELLED = 10;           // Khách hủy yêu cầu

        public static string GetName(int status)
        {
            switch (status)
            {
                case PENDING:
                    return "Chờ xử lý";

                case APPROVED:
                    return "Đã duyệt";

                case ASSIGNED_TO_SHIPPER:
                    return "Đã giao shipper";

                case PICKING_UP:
                    return "Shipper đang lấy hàng";

                case PICKED_UP:
                    return "Shipper đã lấy hàng từ khách";

                case HANDED_OVER:
                    return "Đã bàn giao về cửa hàng";

                case RECEIVED:
                    return "Cửa hàng đã nhận hàng";

                case REFUNDED:
                    return "Đã hoàn tiền";

                case REJECTED:
                    return "Từ chối";

                case CANCELLED:
                    return "Khách hủy yêu cầu";

                default:
                    return "Không xác định";
            }
        }

        public static bool IsProcessing(int status)
        {
            return status == PENDING
                || status == APPROVED
                || status == ASSIGNED_TO_SHIPPER
                || status == PICKING_UP
                || status == PICKED_UP
                || status == HANDED_OVER
                || status == RECEIVED;
        }

        public static bool CanCustomerCancel(int status)
        {
            return status == PENDING;
        }

        public static bool CanAdminApprove(int status)
        {
            return status == PENDING;
        }

        public static bool CanAdminAssignShipper(int status)
        {
            return status == APPROVED;
        }

        public static bool CanShipperStartPickup(int status)
        {
            return status == ASSIGNED_TO_SHIPPER;
        }

        public static bool CanShipperConfirmPickedUp(int status)
        {
            return status == PICKING_UP;
        }

        public static bool CanShipperHandOver(int status)
        {
            return status == PICKED_UP;
        }

        public static bool CanAdminReceive(int status)
        {
            return status == HANDED_OVER;
        }

        public static bool CanAdminRefund(int status)
        {
            return status == RECEIVED;
        }
    }
}