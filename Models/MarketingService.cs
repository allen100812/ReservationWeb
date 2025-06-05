using Dapper;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Web0524.Models
{
    public interface IMarketingService
    {
        // 取得會員可用優惠券
        List<Coupon> GetAvailableCoupons(string memberId);

        // 會員兌換優惠券碼
        bool RedeemCoupon(string memberId, string code);

        // 查詢會員累積點數
        int GetMemberPoints(string memberId);

        // 增加點數
        void AddPoints(string memberId, int points, string action);

        // 扣除點數
        bool DeductPoints(string memberId, int points, string action);

        // 取得會員等級
        string GetMemberLevel(string memberId);

        // 查詢點數紀錄
        List<PointLog> GetPointHistory(string memberId);

        // 發送生日優惠券
        void DistributeBirthdayCoupon(string memberId);

        // 標記優惠券已使用
        bool MarkCouponUsed(string memberId, int couponId);

        // 新增優惠券（後台）
        Coupon AddSystemCoupon(Coupon coupon);

        bool UpdateCoupon(Coupon coupon);

        bool DeleteCoupon(int couponId);

    }

    public class MarketingService : IMarketingService
    {
        private readonly IDbConnection _db;

        public MarketingService(IDbConnection dbConnection)
        {
            _db = dbConnection;
        }

        public List<Coupon> GetAvailableCoupons(string memberId)
        {
            var sql = @"
            SELECT c.* 
            FROM CouponTB c
            JOIN MemberCouponTB mc ON c.CouponId = mc.CouponId
            WHERE mc.MemberId = @memberId AND mc.IsUsed = 0 AND c.IsActive = 1
              AND GETDATE() BETWEEN c.ValidFrom AND c.ValidTo";
            return _db.Query<Coupon>(sql, new { memberId }).ToList();
        }

        public bool RedeemCoupon(string memberId, string code)
        {
            var coupon = _db.QueryFirstOrDefault<Coupon>(
                "SELECT * FROM CouponTB WHERE Code = @code AND IsActive = 1 AND ValidTo >= GETDATE()",
                new { code });

            if (coupon == null) return false;

            var exists = _db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM MemberCouponTB WHERE MemberId = @memberId AND CouponId = @couponId",
                new { memberId, couponId = coupon.CouponId });

            if (exists > 0) return false;

            var insertSql = @"
            INSERT INTO MemberCouponTB (MemberId, CouponId, IsUsed)
            VALUES (@memberId, @couponId, 0)";
            return _db.Execute(insertSql, new { memberId, couponId = coupon.CouponId }) > 0;
        }

        public int GetMemberPoints(string memberId)
        {
            return _db.ExecuteScalar<int>(
                "SELECT ISNULL(SUM(Points), 0) FROM PointLogTB WHERE MemberId = @memberId",
                new { memberId });
        }

        public void AddPoints(string memberId, int points, string action)
        {
            var sql = @"
            INSERT INTO PointLogTB (MemberId, Points, Action, CreateTime, Remark)
            VALUES (@memberId, @points, @action, GETDATE(), '')";
            _db.Execute(sql, new { memberId, points, action });
        }

        public bool DeductPoints(string memberId, int points, string action)
        {
            if (GetMemberPoints(memberId) < points) return false;
            AddPoints(memberId, -points, action);
            return true;
        }

        public string GetMemberLevel(string memberId)
        {
            int points = GetMemberPoints(memberId);
            if (points >= 300) return "VIP";
            if (points >= 150) return "金級";
            if (points >= 50) return "銀級";
            return "銅級";
        }

        public List<PointLog> GetPointHistory(string memberId)
        {
            return _db.Query<PointLog>(
                "SELECT * FROM PointLogTB WHERE MemberId = @memberId ORDER BY CreateTime DESC",
                new { memberId }).ToList();
        }

        public void DistributeBirthdayCoupon(string memberId)
        {
            var user = _db.QueryFirstOrDefault<User>(
                "SELECT * FROM UserTB WHERE Id = @memberId", new { memberId });
            if (user == null || user.Birthday == null) return;

            var today = DateTime.Today;
            if (user.Birthday?.Month != today.Month || user.Birthday?.Day != today.Day)
                return;

            var coupon = _db.QueryFirstOrDefault<Coupon>(
                "SELECT TOP 1 * FROM CouponTB WHERE Title LIKE '%生日%' AND IsActive = 1");

            if (coupon != null)
            {
                var exists = _db.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM MemberCouponTB WHERE MemberId = @memberId AND CouponId = @couponId",
                    new { memberId, couponId = coupon.CouponId });

                if (exists == 0)
                {
                    _db.Execute(
                        "INSERT INTO MemberCouponTB (MemberId, CouponId, IsUsed) VALUES (@memberId, @couponId, 0)",
                        new { memberId, couponId = coupon.CouponId });
                }
            }
        }

        public bool MarkCouponUsed(string memberId, int couponId)
        {
            var updated = _db.Execute(@"
            UPDATE MemberCouponTB
            SET IsUsed = 1, UsedDate = GETDATE()
            WHERE MemberId = @memberId AND CouponId = @couponId AND IsUsed = 0",
                new { memberId, couponId });

            return updated > 0;
        }

        public Coupon AddSystemCoupon(Coupon coupon)
        {
            var sql = @"
        INSERT INTO CouponTB 
        (Title, Code, DiscountAmount, MinAmount, ValidFrom, ValidTo, ForFirstTimeUser, IsActive)
        VALUES 
        (@Title, @Code, @DiscountAmount, @MinAmount, @ValidFrom, @ValidTo, @ForFirstTimeUser, @IsActive);
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int newId = _db.ExecuteScalar<int>(sql, coupon);
            coupon.CouponId = newId;
            return coupon;
        }

        public bool UpdateCoupon(Coupon coupon)
        {
            var sql = @"
        UPDATE CouponTB
        SET Title = @Title,
            Code = @Code,
            DiscountAmount = @DiscountAmount,
            MinAmount = @MinAmount,
            ValidFrom = @ValidFrom,
            ValidTo = @ValidTo,
            ForFirstTimeUser = @ForFirstTimeUser,
            IsActive = @IsActive
        WHERE CouponId = @CouponId";

            return _db.Execute(sql, coupon) > 0;
        }


        public bool DeleteCoupon(int couponId)
        {
            var sql = "UPDATE CouponTB SET IsActive = 0 WHERE CouponId = @couponId";
            return _db.Execute(sql, new { couponId }) > 0;
        }

    }


}
