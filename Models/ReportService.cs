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
        // 1. 總預約統計：每日 / 每月 / 區間
        Task<IEnumerable<ReservationSummaryDto>> GetReservationSummaryAsync(DateTime? startDate, DateTime? endDate);

        // 2. 設計師接單報表
        Task<IEnumerable<DesignerReservationStatsDto>> GetDesignerReservationStatsAsync(DateTime? startDate, DateTime? endDate);

        // 3. 高峰時段分析
        Task<IEnumerable<PeakHourStatsDto>> GetPeakHourStatsAsync(DateTime? startDate, DateTime? endDate);

        // 4. 營收統計報表（日/月）
        Task<IEnumerable<RevenueSummaryDto>> GetRevenueSummaryAsync(DateTime? startDate, DateTime? endDate);

        // 5. 設計師營收報表
        Task<IEnumerable<DesignerRevenueDto>> GetDesignerRevenueAsync(DateTime? startDate, DateTime? endDate);

        // 6. 支付方式分析
        Task<IEnumerable<PaymentMethodStatsDto>> GetPaymentMethodStatsAsync(DateTime? startDate, DateTime? endDate);

        // 7. 會員活躍度分析
        Task<IEnumerable<MemberActivityDto>> GetMemberActivityAsync(DateTime? startDate, DateTime? endDate);

        // 8. 點數使用分析
        Task<IEnumerable<PointUsageDto>> GetPointUsageSummaryAsync(DateTime? startDate, DateTime? endDate);

        // 9. 高貢獻會員排行
        Task<IEnumerable<TopMemberDto>> GetTopMembersAsync(DateTime? startDate, DateTime? endDate, int topN = 20);

        // 10. 優惠券使用分析
        Task<IEnumerable<CouponUsageDto>> GetCouponUsageSummaryAsync(DateTime? startDate, DateTime? endDate);

        // 11. 預約明細匯出
        Task<IEnumerable<ReservationDetailDto>> GetReservationDetailsAsync(DateTime? startDate, DateTime? endDate, int? designerId = null);
    }

    public class ReportService : IReportService
    {
        private readonly IDbConnection _db;

        public ReportService(IDbConnection dbConnection)
        {
            _db = dbConnection;
        }

        public async Task<IEnumerable<ReservationSummaryDto>> GetReservationSummaryAsync(DateTime? startDate, DateTime? endDate)
        {
            var sql = @"
                SELECT
                    DATE(Orderdate) AS Date,
                    COUNT(*) AS TotalReservations,
                    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS CompletedCount,
                    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS CancelledCount
                FROM OrderTB
                WHERE (@startDate IS NULL OR Orderdate >= @startDate)
                  AND (@endDate IS NULL OR Orderdate <= @endDate)
                GROUP BY DATE(Orderdate)
                ORDER BY Date
            ";
            return await _db.QueryAsync<ReservationSummaryDto>(sql, new { startDate, endDate });
        }

        public async Task<IEnumerable<DesignerReservationStatsDto>> GetDesignerReservationStatsAsync(DateTime? startDate, DateTime? endDate)
        {
            var sql = @"
                SELECT
                    d.DesignerId,
                    d.Name AS DesignerName,
                    COUNT(o.OrderId) AS TotalReservations,
                    SUM(CASE WHEN o.Status = 1 THEN 1 ELSE 0 END) AS Completed,
                    SUM(CASE WHEN o.Status = 2 THEN 1 ELSE 0 END) AS Cancelled
                FROM OrderTB o
                JOIN DesignerTB d ON o.DesignerId = d.DesignerId
                WHERE (@startDate IS NULL OR o.Orderdate >= @startDate)
                  AND (@endDate IS NULL OR o.Orderdate <= @endDate)
                GROUP BY d.DesignerId, d.Name
                ORDER BY TotalReservations DESC
            ";
            return await _db.QueryAsync<DesignerReservationStatsDto>(sql, new { startDate, endDate });
        }

        public async Task<IEnumerable<PeakHourStatsDto>> GetPeakHourStatsAsync(DateTime? startDate, DateTime? endDate)
        {
            var sql = @"
                SELECT
                    HOUR(ReservationDateTime) AS Hour,
                    COUNT(*) AS ReservationCount
                FROM OrderTB
                WHERE (@startDate IS NULL OR Orderdate >= @startDate)
                  AND (@endDate IS NULL OR Orderdate <= @endDate)
                GROUP BY HOUR(ReservationDateTime)
                ORDER BY Hour
            ";
            return await _db.QueryAsync<PeakHourStatsDto>(sql, new { startDate, endDate });
        }

        public async Task<IEnumerable<RevenueSummaryDto>> GetRevenueSummaryAsync(DateTime? startDate, DateTime? endDate)
        {
            var sql = @"
                SELECT
                    DATE(Orderdate) AS Date,
                    SUM(Price) AS TotalRevenue,
                    SUM(IFNULL(DiscountAmount, 0)) AS DiscountAmount
                FROM OrderTB
                WHERE (@startDate IS NULL OR Orderdate >= @startDate)
                  AND (@endDate IS NULL OR Orderdate <= @endDate)
                  AND Status = 1
                GROUP BY DATE(Orderdate)
                ORDER BY Date
            ";
            return await _db.QueryAsync<RevenueSummaryDto>(sql, new { startDate, endDate });
        }

        public async Task<IEnumerable<DesignerRevenueDto>> GetDesignerRevenueAsync(DateTime? startDate, DateTime? endDate)
        {
            var sql = @"
                SELECT
                    d.DesignerId,
                    d.Name AS DesignerName,
                    SUM(o.Price) AS TotalRevenue
                FROM OrderTB o
                JOIN DesignerTB d ON o.DesignerId = d.DesignerId
                WHERE (@startDate IS NULL OR o.Orderdate >= @startDate)
                  AND (@endDate IS NULL OR o.Orderdate <= @endDate)
                  AND o.Status = 1
                GROUP BY d.DesignerId, d.Name
                ORDER BY TotalRevenue DESC
            ";
            return await _db.QueryAsync<DesignerRevenueDto>(sql, new { startDate, endDate });
        }

        public async Task<IEnumerable<PaymentMethodStatsDto>> GetPaymentMethodStatsAsync(DateTime? startDate, DateTime? endDate)
        {
            var sql = @"
                SELECT
                    PaymentMethod,
                    COUNT(*) AS Count,
                    SUM(Price) AS TotalAmount
                FROM OrderTB
                WHERE (@startDate IS NULL OR Orderdate >= @startDate)
                  AND (@endDate IS NULL OR Orderdate <= @endDate)
                  AND Status = 1
                GROUP BY PaymentMethod
            ";
            return await _db.QueryAsync<PaymentMethodStatsDto>(sql, new { startDate, endDate });
        }

        public async Task<IEnumerable<MemberActivityDto>> GetMemberActivityAsync(DateTime? startDate, DateTime? endDate)
        {
            var sql = @"
                SELECT
                    u.Id AS MemberId,
                    u.Name AS MemberName,
                    COUNT(o.OrderId) AS ReservationCount,
                    MAX(o.Orderdate) AS LastReservationDate
                FROM OrderTB o
                JOIN UserTB u ON o.Uid = u.Id
                WHERE (@startDate IS NULL OR o.Orderdate >= @startDate)
                  AND (@endDate IS NULL OR o.Orderdate <= @endDate)
                GROUP BY u.Id, u.Name
                ORDER BY ReservationCount DESC
            ";
            return await _db.QueryAsync<MemberActivityDto>(sql, new { startDate, endDate });
        }

        public async Task<IEnumerable<PointUsageDto>> GetPointUsageSummaryAsync(DateTime? startDate, DateTime? endDate)
        {
            var sql = @"
                SELECT
                    MemberId,
                    SUM(CASE WHEN Points > 0 THEN Points ELSE 0 END) AS PointsEarned,
                    SUM(CASE WHEN Points < 0 THEN -Points ELSE 0 END) AS PointsUsed
                FROM PointLogTB
                WHERE (@startDate IS NULL OR CreateTime >= @startDate)
                  AND (@endDate IS NULL OR CreateTime <= @endDate)
                GROUP BY MemberId
            ";
            return await _db.QueryAsync<PointUsageDto>(sql, new { startDate, endDate });
        }

        public async Task<IEnumerable<TopMemberDto>> GetTopMembersAsync(DateTime? startDate, DateTime? endDate, int topN = 20)
        {
            var sql = $@"
                SELECT
                    u.Id AS MemberId,
                    u.Name AS MemberName,
                    SUM(o.Price) AS TotalSpent,
                    COUNT(o.OrderId) AS ReservationCount
                FROM OrderTB o
                JOIN UserTB u ON o.Uid = u.Id
                WHERE (@startDate IS NULL OR o.Orderdate >= @startDate)
                  AND (@endDate IS NULL OR o.Orderdate <= @endDate)
                  AND o.Status = 1
                GROUP BY u.Id, u.Name
                ORDER BY TotalSpent DESC
                LIMIT @topN
            ";
            return await _db.QueryAsync<TopMemberDto>(sql, new { startDate, endDate, topN });
        }

        public async Task<IEnumerable<CouponUsageDto>> GetCouponUsageSummaryAsync(DateTime? startDate, DateTime? endDate)
        {
            var sql = @"
                SELECT
                    c.CouponId,
                    c.Title,
                    COUNT(d.RecordId) AS UsageCount,
                    SUM(IFNULL(o.DiscountAmount, 0)) AS TotalDiscountAmount
                FROM CouponDispatchRecordTB d
                JOIN CouponTB c ON d.CouponId = c.CouponId
                LEFT JOIN OrderTB o ON d.OrderId = o.OrderId
                WHERE d.IsDispatched = 1
                  AND (@startDate IS NULL OR d.DispatchDate >= @startDate)
                  AND (@endDate IS NULL OR d.DispatchDate <= @endDate)
                GROUP BY c.CouponId, c.Title
            ";
            return await _db.QueryAsync<CouponUsageDto>(sql, new { startDate, endDate });
        }

        public async Task<IEnumerable<ReservationDetailDto>> GetReservationDetailsAsync(DateTime? startDate, DateTime? endDate, int? designerId = null)
        {
            var sql = @"
                SELECT
                    o.OrderId,
                    o.ReservationDateTime,
                    d.Name AS DesignerName,
                    u.Name AS MemberName,
                    p.Name AS ServiceName,
                    o.Price,
                    CASE o.Status
                        WHEN 0 THEN '未處理'
                        WHEN 1 THEN '已完成'
                        WHEN 2 THEN '已取消'
                        ELSE '其他'
                    END AS Status
                FROM OrderTB o
                LEFT JOIN DesignerTB d ON o.DesignerId = d.DesignerId
                LEFT JOIN UserTB u ON o.Uid = u.Id
                LEFT JOIN ProductTB p ON o.ProductId = p.ProductId
                WHERE (@startDate IS NULL OR o.Orderdate >= @startDate)
                  AND (@endDate IS NULL OR o.Orderdate <= @endDate)
                  AND (@designerId IS NULL OR o.DesignerId = @designerId)
                ORDER BY o.ReservationDateTime DESC
            ";
            return await _db.QueryAsync<ReservationDetailDto>(sql, new { startDate, endDate, designerId });
        }
    }
}
