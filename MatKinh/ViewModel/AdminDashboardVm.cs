namespace MatKinh.ViewModel
{
    public class AdminDashboardVm
    {
        public string RoleCode { get; set; }
        public string RoleName { get; set; }
        public string DisplayName { get; set; }

        // Orders
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int PreparingOrders { get; set; }
        public int AssignedOrders { get; set; }
        public int DeliveringOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int FailedOrders { get; set; }
        public int CancelledOrders { get; set; }

        // Revenue
        public decimal TotalRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal TodayRevenue { get; set; }

        // Products
        public int ActiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int MyProducts { get; set; }

        // Blogs
        public int PendingBlogs { get; set; }
        public int PublishedBlogs { get; set; }
        public int MyBlogs { get; set; }

        // Accounts
        public int ActiveAccounts { get; set; }
        public int StaffAccounts { get; set; }
        public int ShipperAccounts { get; set; }

        // Shipper
        public int MyAssignedOrders { get; set; }
        public int MyDeliveringOrders { get; set; }
        public int MyDeliveredOrders { get; set; }
        public int MyFailedOrders { get; set; }
    }
}