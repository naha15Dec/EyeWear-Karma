namespace MatKinh.ViewModel
{
    public class AdminRevenueVm
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalMonthRevenue { get; set; }
        public decimal TotalTodayRevenue { get; set; }

        public int TotalProductSoldToday { get; set; }
        public int TotalProductSoldMonth { get; set; }
        public int TotalProductSoldAll { get; set; }

        public int TotalOrderSuccess { get; set; }
        public int TotalOrderToday { get; set; }
        public int TotalOrderMonth { get; set; }

        public int TotalOrderCod { get; set; }
        public int TotalOrderVnpay { get; set; }

        public decimal TotalRevenueCod { get; set; }
        public decimal TotalRevenueVnpay { get; set; }

        public int TotalPaymentFailed { get; set; }
    }
}