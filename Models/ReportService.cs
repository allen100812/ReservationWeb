
// ReportService.cs（完整補齊 + 中文註解）
using System;
using System.Collections.Generic;
using System.Linq;
using Web0524.Models;
using static Web0524.Models.Marketing;
namespace Web0524.Models
{


    // 報表服務介面，定義系統中各類報表方法
    public interface IReportService
    {
        // 系統總覽：會員總數、當月活躍會員、預約數、使用過優惠券數、總點數
        ReportSummary GetOverallSummary();
        // 月預約報表：依年份統計每月預約數與營收
        List<MonthlyOrderReport> GetMonthlyOrderStats(int year);
        // 點數排行榜：累積點數最多的會員
        List<User> GetTopPointUsers(int topN);
        // 使用最多的優惠券排行
        List<Coupon> GetMostUsedCoupons(int topN);
        // 設計師績效：各設計師的預約數與營收
        List<DesignerPerformance> GetDesignerPerformance();
        // 熱門服務：被預約最多次的服務
        List<ServicePopularity> GetPopularServices(int topN);

        // 活躍會員：依總金額排序的會員
        List<User> GetActiveMembers(int topN);
        // 當日/當月壽星會員名單
        List<User> GetBirthdayMembers(bool todayOnly);
        // 首次預約會員統計
        int GetFirstTimeUserCount(DateTime startDate, DateTime endDate);

        // 單日預約明細（可配合設計師/服務條件過濾）
        List<Order> GetDailyOrders(DateTime date);
        // 預約取消率：設計師與服務的取消率統計
        List<(string serviceOrDesigner, double cancelRate)> GetCancelRateReport();
        // 熱門時段：分析每天哪個時段預約最多
        List<(string hour, int count)> GetPeakHours();

        // 各優惠券的發放數與使用率統計
        List<(string code, int totalIssued, int usedCount)> GetCouponUsageStats();
        // 單月營收總額
        double GetMonthlyRevenue(int year, int month);
        // 顧客終身價值（LTV）：累積消費金額
        List<(string uid, double ltv)> GetUserLTV();
        // 流失會員清單：在指定天數內無預約者
        List<User> GetInactiveMembers(int inactiveDays);


    }

    public class ReportService : IReportService
    {
        private readonly List<User> Users;
        private readonly List<Order> Orders;
        private readonly List<PointLog> PointLogs;
        private readonly List<MemberCoupon> MemberCoupons;
        private readonly List<Coupon> Coupons;
        private readonly List<Designer> Designers;
        private readonly List<Product> Services;

        public ReportService(List<User> users, List<Order> orders, List<PointLog> pointLogs,
                             List<MemberCoupon> memberCoupons, List<Coupon> coupons,
                             List<Designer> designers, List<Product> services)
        {
            Users = users;
            Orders = orders;
            PointLogs = pointLogs;
            MemberCoupons = memberCoupons;
            Coupons = coupons;
            Designers = designers;
            Services = services;
        }

        // 系統總覽：會員總數、當月活躍會員、預約數、使用過優惠券數、總點數
        public ReportSummary GetOverallSummary()
        {
            var now = DateTime.Now;
            return new ReportSummary
            {
                TotalUsers = Users.Count,
                ActiveUsersThisMonth = Orders.Where(o => o.ReservationDateTime.Year == now.Year && o.ReservationDateTime.Month == now.Month).Select(o => o.Uid).Distinct().Count(),
                TotalOrders = Orders.Count,
                UsedCoupons = MemberCoupons.Count(c => c.IsUsed),
                TotalPoints = PointLogs.Sum(p => p.Points)
            };
        }

        // 月預約報表：依年份統計每月預約數與營收
        public List<MonthlyOrderReport> GetMonthlyOrderStats(int year)
        {
            return Enumerable.Range(1, 12)
                .Select(month => new MonthlyOrderReport
                {
                    Year = year,
                    Month = month,
                    OrderCount = Orders.Count(o => o.ReservationDateTime.Year == year && o.ReservationDateTime.Month == month),
                    TotalRevenue = Orders.Where(o => o.ReservationDateTime.Year == year && o.ReservationDateTime.Month == month).Sum(o => o.Price)
                }).ToList();
        }

        // 點數排行榜：累積點數最多的會員
        public List<User> GetTopPointUsers(int topN)
        {
            return PointLogs.GroupBy(p => p.MemberId)
                .Select(g => new { MemberId = g.Key, Total = g.Sum(p => p.Points) })
                .OrderByDescending(g => g.Total)
                .Take(topN)
                .Join(Users, g => g.MemberId, u => u.Id, (g, u) => u)
                .ToList();
        }

        // 使用最多的優惠券排行
        public List<Coupon> GetMostUsedCoupons(int topN)
        {
            return MemberCoupons
                .Where(c => c.IsUsed)
                .GroupBy(c => c.CouponId)
                .OrderByDescending(g => g.Count())
                .Take(topN)
                .Select(g => Coupons.FirstOrDefault(c => c.CouponId == g.Key))
                .Where(c => c != null)
                .ToList();
        }

        // 設計師績效：各設計師的預約數與營收
        public List<DesignerPerformance> GetDesignerPerformance()
        {
            return Orders.GroupBy(o => o.DesignerId)
                .Select(g =>
                {
                    var designerName = Designers.FirstOrDefault(d => d.DesignerId == g.Key)?.Name ?? "未知";
                    return new DesignerPerformance
                    {
                        DesignerName = designerName,
                        OrderCount = g.Count(),
                        TotalRevenue = g.Sum(o => o.Price)
                    };
                }).ToList();
        }

        // 熱門服務：被預約最多次的服務
        public List<ServicePopularity> GetPopularServices(int topN)
        {
            return Orders.GroupBy(o => o.ProductId)
                .OrderByDescending(g => g.Count())
                .Take(topN)
                .Select(g =>
                {
                    var name = Services.FirstOrDefault(s => s.ProductId == g.Key)?.Name ?? "未知";
                    return new ServicePopularity
                    {
                        ServiceName = name,
                        OrderCount = g.Count()
                    };
                }).ToList();
        }

        // 活躍會員：依總金額排序的會員
        public List<User> GetActiveMembers(int topN) =>
            Orders.GroupBy(o => o.Uid)
                .Select(g => new { Uid = g.Key, Total = g.Sum(o => o.Price) })
                .OrderByDescending(g => g.Total)
                .Take(topN)
                .Join(Users, g => g.Uid, u => u.Id, (g, u) => u)
                .ToList();

        // 當日/當月壽星會員名單
        public List<User> GetBirthdayMembers(bool todayOnly)
        {
            var today = DateTime.Today;
            return Users.Where(u => u.Birthday != null &&
                (todayOnly
                    ? u.Birthday.Value.Month == today.Month && u.Birthday.Value.Day == today.Day
                    : u.Birthday.Value.Month == today.Month))
                .ToList();
        }

        // 首次預約會員統計
        public int GetFirstTimeUserCount(DateTime startDate, DateTime endDate)
        {
            var firstOrders = Orders.GroupBy(o => o.Uid).Select(g => g.Min(o => o.ReservationDateTime));
            return firstOrders.Count(t => t >= startDate && t <= endDate);
        }

        // 單日預約明細（可配合設計師/服務條件過濾）
        public List<Order> GetDailyOrders(DateTime date) =>
            Orders.Where(o => o.ReservationDateTime.Date == date.Date).ToList();

        // 預約取消率：設計師與服務的取消率統計
        public List<(string serviceOrDesigner, double cancelRate)> GetCancelRateReport()
        {
            var designerRates = Orders
                .GroupBy(o => o.DesignerId)
                .Select(g => (Designer: Designers.FirstOrDefault(d => d.DesignerId == g.Key)?.Name ?? "未知",
                              Rate: (double)g.Count(o => o.Status == OrderStatus.Cancelled) / g.Count()))
                .ToList();

            var serviceRates = Orders
                .GroupBy(o => o.ProductId)
                .Select(g => (Service: Services.FirstOrDefault(s => s.ProductId == g.Key)?.Name ?? "未知",
                              Rate: (double)g.Count(o => o.Status == OrderStatus.Cancelled) / g.Count()))
                .ToList();

            return designerRates.Concat(serviceRates).ToList();
        }

        // 熱門時段：分析每天哪個時段預約最多
        public List<(string hour, int count)> GetPeakHours()
        {
            return Orders
                .GroupBy(o => o.ReservationDateTime.ToString("HH:00"))
                .Select(g => (Hour: g.Key, Count: g.Count()))
                .OrderByDescending(g => g.Count)
                .ToList();
        }

        // 各優惠券的發放數與使用率統計
        public List<(string code, int totalIssued, int usedCount)> GetCouponUsageStats()
        {
            return MemberCoupons
                .GroupBy(mc => mc.CouponId)
                .Select(g =>
                {
                    var coupon = Coupons.FirstOrDefault(c => c.CouponId == g.Key);
                    return (coupon?.Code ?? "", g.Count(), g.Count(mc => mc.IsUsed));
                }).ToList();
        }

        // 單月營收總額
        public double GetMonthlyRevenue(int year, int month) =>
            Orders.Where(o => o.ReservationDateTime.Year == year && o.ReservationDateTime.Month == month)
                  .Sum(o => o.Price);

        // 顧客終身價值（LTV）：累積消費金額
        public List<(string uid, double ltv)> GetUserLTV() =>
            Orders.GroupBy(o => o.Uid)
                  .Select(g => (uid: g.Key, ltv: g.Sum(o => o.Price)))
                  .ToList();


        // 流失會員清單：在指定天數內無預約者
        public List<User> GetInactiveMembers(int inactiveDays)
        {
            var cutoff = DateTime.Today.AddDays(-inactiveDays);
            var activeIds = Orders.Where(o => o.ReservationDateTime > cutoff).Select(o => o.Uid).Distinct().ToHashSet();
            return Users.Where(u => !activeIds.Contains(u.Id)).ToList();
        }


    }
}
