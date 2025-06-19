namespace Web0524.Models
{
    // 以下為 DTO 範例，實際可依需求調整欄位內容
    public class ReservationSummaryDto
    {
        public DateTime Date { get; set; }
        public int TotalReservations { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class DesignerReservationStatsDto
    {
        public int DesignerId { get; set; }
        public string DesignerName { get; set; }
        public int TotalReservations { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
    }

    public class PeakHourStatsDto
    {
        public int Hour { get; set; }
        public int ReservationCount { get; set; }
    }

    public class RevenueSummaryDto
    {
        public DateTime Date { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class DesignerRevenueDto
    {
        public int DesignerId { get; set; }
        public string DesignerName { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PaymentMethodStatsDto
    {
        public int PaymentMethod { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class MemberActivityDto
    {
        public string MemberId { get; set; }
        public string MemberName { get; set; }
        public int ReservationCount { get; set; }
        public DateTime? LastReservationDate { get; set; }
    }

    public class PointUsageDto
    {
        public string MemberId { get; set; }
        public int PointsEarned { get; set; }
        public int PointsUsed { get; set; }
    }

    public class TopMemberDto
    {
        public string MemberId { get; set; }
        public string MemberName { get; set; }
        public decimal TotalSpent { get; set; }
        public int ReservationCount { get; set; }
    }

    public class CouponUsageDto
    {
        public int CouponId { get; set; }
        public string Title { get; set; }
        public int UsageCount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
    }

    public class ReservationDetailDto
    {
        public int OrderId { get; set; }
        public DateTime ReservationDateTime { get; set; }
        public string DesignerName { get; set; }
        public string MemberName { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
    }
}
