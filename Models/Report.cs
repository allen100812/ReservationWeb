namespace Web0524.Models
{
    public class Report
    {
        public class ReportSummary
        {
            public int TotalUsers { get; set; }
            public int ActiveUsersThisMonth { get; set; }
            public int TotalOrders { get; set; }
            public int TotalPoints { get; set; }
            public int UsedCoupons { get; set; }
        }

        public class MonthlyOrderReport
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public int OrderCount { get; set; }
            public double TotalRevenue { get; set; }
        }

        public class DesignerPerformance
        {
            public string DesignerName { get; set; }
            public int OrderCount { get; set; }
            public double TotalRevenue { get; set; }
            public double AvgRating { get; set; }  // 若有評分資料
        }

        public class ServicePopularity
        {
            public string ServiceName { get; set; }
            public int OrderCount { get; set; }
        }

    }
}
