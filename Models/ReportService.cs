// ReportService.cs（從 MSSQL 資料庫撈資料的完整版本）
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;

namespace Web0524.Models
{
    public interface IReportService
    {
        ReportSummary GetOverallSummary();
        List<MonthlyOrderReport> GetMonthlyOrderStats(int year);
        List<User> GetTopPointUsers(int topN);
        List<Coupon> GetMostUsedCoupons(int topN);
        List<DesignerPerformance> GetDesignerPerformance();
        List<ServicePopularity> GetPopularServices(int topN);
        List<User> GetActiveMembers(int topN);
        List<User> GetBirthdayMembers(bool todayOnly);
        int GetFirstTimeUserCount(DateTime startDate, DateTime endDate);
        List<Order> GetDailyOrders(DateTime date);
        List<(string serviceOrDesigner, double cancelRate)> GetCancelRateReport();
        List<(string hour, int count)> GetPeakHours();
        List<(string code, int totalIssued, int usedCount)> GetCouponUsageStats();
        double GetMonthlyRevenue(int year, int month);
        List<(string uid, double ltv)> GetUserLTV();
        List<User> GetInactiveMembers(int inactiveDays);
    }

    public class ReportService : IReportService
    {
        private readonly IDbConnection _db;

        public ReportService(IDbConnection dbConnection)
        {
            _db = dbConnection;
        }

        public ReportSummary GetOverallSummary()
        {
            var now = DateTime.Now;
            return new ReportSummary
            {
                TotalUsers = _db.ExecuteScalar<int>("SELECT COUNT(*) FROM UserTB"),
                ActiveUsersThisMonth = _db.ExecuteScalar<int>(@"
                    SELECT COUNT(DISTINCT Uid) FROM OrderTB 
                    WHERE YEAR(ReservationDateTime) = @Year AND MONTH(ReservationDateTime) = @Month",
                    new { Year = now.Year, Month = now.Month }),
                TotalOrders = _db.ExecuteScalar<int>("SELECT COUNT(*) FROM OrderTB"),
                UsedCoupons = _db.ExecuteScalar<int>("SELECT COUNT(*) FROM MemberCouponTB WHERE IsUsed = 1"),
                TotalPoints = _db.ExecuteScalar<int>("SELECT ISNULL(SUM(Points), 0) FROM PointLogTB")
            };
        }

        public List<MonthlyOrderReport> GetMonthlyOrderStats(int year)
        {
            var list = new List<MonthlyOrderReport>();
            for (int month = 1; month <= 12; month++)
            {
                var orderCount = _db.ExecuteScalar<int>("SELECT COUNT(*) FROM OrderTB WHERE YEAR(ReservationDateTime) = @Year AND MONTH(ReservationDateTime) = @Month", new { Year = year, Month = month });
                var revenue = _db.ExecuteScalar<decimal>("SELECT ISNULL(SUM(Price), 0) FROM OrderTB WHERE YEAR(ReservationDateTime) = @Year AND MONTH(ReservationDateTime) = @Month", new { Year = year, Month = month });
                list.Add(new MonthlyOrderReport { Year = year, Month = month, OrderCount = orderCount, TotalRevenue = (double)revenue });
            }
            return list;
        }

        public List<User> GetTopPointUsers(int topN)
        {
            var sql = @"
                SELECT TOP (@TopN) u.*
                FROM (
                    SELECT MemberId, SUM(Points) AS TotalPoints
                    FROM PointLogTB
                    GROUP BY MemberId
                ) AS p
                JOIN UserTB u ON u.Id = p.MemberId
                ORDER BY p.TotalPoints DESC";
            return _db.Query<User>(sql, new { TopN = topN }).ToList();
        }

        public List<Coupon> GetMostUsedCoupons(int topN)
        {
            var sql = @"
                SELECT TOP (@TopN) c.*
                FROM MemberCouponTB mc
                JOIN CouponTB c ON c.CouponId = mc.CouponId
                WHERE mc.IsUsed = 1
                GROUP BY c.CouponId, c.Title, c.Code, c.DiscountAmount, c.MinAmount, c.ValidFrom, c.ValidTo, c.ForFirstTimeUser, c.IsActive
                ORDER BY COUNT(*) DESC";
            return _db.Query<Coupon>(sql, new { TopN = topN }).ToList();
        }

        public List<DesignerPerformance> GetDesignerPerformance()
        {
            var sql = @"
                SELECT d.Name AS DesignerName, COUNT(o.OrderId) AS OrderCount, SUM(o.Price) AS TotalRevenue
                FROM OrderTB o
                JOIN DesignerTB d ON o.DesignerId = d.DesignerId
                GROUP BY d.Name";
            return _db.Query<DesignerPerformance>(sql).ToList();
        }

        public List<ServicePopularity> GetPopularServices(int topN)
        {
            var sql = @"
                SELECT TOP (@TopN) p.Name AS ServiceName, COUNT(o.OrderId) AS OrderCount
                FROM OrderTB o
                JOIN ProductTB p ON o.ProductId = p.ProductId
                GROUP BY p.Name
                ORDER BY COUNT(o.OrderId) DESC";
            return _db.Query<ServicePopularity>(sql, new { TopN = topN }).ToList();
        }

        public List<User> GetActiveMembers(int topN)
        {
            var sql = @"
                SELECT TOP (@TopN) u.*
                FROM (
                    SELECT Uid, SUM(Price) AS TotalAmount
                    FROM OrderTB
                    GROUP BY Uid
                ) AS t
                JOIN UserTB u ON u.Id = t.Uid
                ORDER BY t.TotalAmount DESC";
            return _db.Query<User>(sql, new { TopN = topN }).ToList();
        }

        public List<User> GetBirthdayMembers(bool todayOnly)
        {
            string sql = todayOnly
                ? "SELECT * FROM UserTB WHERE MONTH(Birthday) = @Month AND DAY(Birthday) = @Day"
                : "SELECT * FROM UserTB WHERE MONTH(Birthday) = @Month";

            var today = DateTime.Today;
            return _db.Query<User>(sql, new { Month = today.Month, Day = today.Day }).ToList();
        }

        public int GetFirstTimeUserCount(DateTime startDate, DateTime endDate)
        {
            var sql = @"
                SELECT COUNT(*) FROM (
                    SELECT Uid, MIN(ReservationDateTime) AS FirstTime
                    FROM OrderTB
                    GROUP BY Uid
                ) AS firstOrders
                WHERE FirstTime BETWEEN @StartDate AND @EndDate";
            return _db.ExecuteScalar<int>(sql, new { StartDate = startDate, EndDate = endDate });
        }

        public List<Order> GetDailyOrders(DateTime date)
        {
            var sql = "SELECT * FROM OrderTB WHERE CAST(ReservationDateTime AS DATE) = @Date";
            return _db.Query<Order>(sql, new { Date = date.Date }).ToList();
        }

        public List<(string serviceOrDesigner, double cancelRate)> GetCancelRateReport()
        {
            var result = new List<(string, double)>();

            var designerSql = @"
                SELECT d.Name AS Name, 
                       CAST(SUM(CASE WHEN o.Status = 2 THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) AS CancelRate
                FROM OrderTB o
                JOIN DesignerTB d ON o.DesignerId = d.DesignerId
                GROUP BY d.Name";
            result.AddRange(_db.Query<(string, double)>(designerSql));

            var serviceSql = @"
                SELECT p.Name AS Name, 
                       CAST(SUM(CASE WHEN o.Status = 2 THEN 1 ELSE 0 END) AS FLOAT) / COUNT(*) AS CancelRate
                FROM OrderTB o
                JOIN ProductTB p ON o.ProductId = p.ProductId
                GROUP BY p.Name";
            result.AddRange(_db.Query<(string, double)>(serviceSql));

            return result;
        }

        public List<(string hour, int count)> GetPeakHours()
        {
            var sql = @"
                SELECT FORMAT(ReservationDateTime, 'HH:00') AS Hour,
                       COUNT(*) AS Count
                FROM OrderTB
                GROUP BY FORMAT(ReservationDateTime, 'HH:00')
                ORDER BY Count DESC";
            return _db.Query<(string, int)>(sql).ToList();
        }

        public List<(string code, int totalIssued, int usedCount)> GetCouponUsageStats()
        {
            var sql = @"
                SELECT c.Code, COUNT(*) AS totalIssued,
                       SUM(CASE WHEN mc.IsUsed = 1 THEN 1 ELSE 0 END) AS usedCount
                FROM MemberCouponTB mc
                JOIN CouponTB c ON mc.CouponId = c.CouponId
                GROUP BY c.Code";
            return _db.Query<(string, int, int)>(sql).ToList();
        }

        public double GetMonthlyRevenue(int year, int month)
        {
            var sql = "SELECT ISNULL(SUM(Price), 0) FROM OrderTB WHERE YEAR(ReservationDateTime) = @Year AND MONTH(ReservationDateTime) = @Month";
            return _db.ExecuteScalar<double>(sql, new { Year = year, Month = month });
        }

        public List<(string uid, double ltv)> GetUserLTV()
        {
            var sql = @"
                SELECT Uid, SUM(Price) AS LTV
                FROM OrderTB
                GROUP BY Uid";
            return _db.Query<(string, double)>(sql).ToList();
        }

        public List<User> GetInactiveMembers(int inactiveDays)
        {
            var sql = @"
                SELECT * FROM UserTB
                WHERE Id NOT IN (
                    SELECT DISTINCT Uid FROM OrderTB WHERE ReservationDateTime > @Cutoff
                )";
            return _db.Query<User>(sql, new { Cutoff = DateTime.Today.AddDays(-inactiveDays) }).ToList();
        }
    }
}
