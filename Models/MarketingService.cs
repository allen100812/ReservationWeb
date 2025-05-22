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
        void AddSystemCoupon(Coupon coupon);
    }

    public class MarketingService : IMarketingService
    {
        private List<Coupon> Coupons = new(); // 優惠券清單（模擬資料）
        private List<MemberCoupon> MemberCoupons = new(); // 會員擁有的優惠券
        private List<PointLog> PointLogs = new(); // 點數紀錄清單
        private List<User> Users = new(); // 使用者清單

        // 取得會員目前可用的所有優惠券
        public List<Coupon> GetAvailableCoupons(string memberId)
        {
            var now = DateTime.Now;
            var owned = MemberCoupons.Where(mc => mc.MemberId == memberId && !mc.IsUsed)
                                     .Select(mc => mc.CouponId)
                                     .ToList();
            return Coupons.Where(c => owned.Contains(c.CouponId) && c.IsActive && now >= c.ValidFrom && now <= c.ValidTo)
                          .ToList();
        }

        // 會員兌換優惠券碼
        public bool RedeemCoupon(string memberId, string code)
        {
            var coupon = Coupons.FirstOrDefault(c => c.Code == code && c.IsActive && DateTime.Now <= c.ValidTo);
            if (coupon == null) return false;

            if (MemberCoupons.Any(mc => mc.MemberId == memberId && mc.CouponId == coupon.CouponId))
                return false;

            MemberCoupons.Add(new MemberCoupon
            {
                Id = MemberCoupons.Count + 1,
                MemberId = memberId,
                CouponId = coupon.CouponId,
                IsUsed = false
            });
            return true;
        }

        // 取得會員目前累積點數
        public int GetMemberPoints(string memberId)
        {
            return PointLogs.Where(p => p.MemberId == memberId).Sum(p => p.Points);
        }

        // 增加點數
        public void AddPoints(string memberId, int points, string action)
        {
            PointLogs.Add(new PointLog
            {
                Id = PointLogs.Count + 1,
                MemberId = memberId,
                Points = points,
                Action = action,
                CreateTime = DateTime.Now,
                Remark = string.Empty
            });
        }

        // 扣除點數
        public bool DeductPoints(string memberId, int points, string action)
        {
            if (GetMemberPoints(memberId) < points) return false;

            PointLogs.Add(new PointLog
            {
                Id = PointLogs.Count + 1,
                MemberId = memberId,
                Points = -points,
                Action = action,
                CreateTime = DateTime.Now,
                Remark = string.Empty
            });
            return true;
        }

        // 判斷會員等級
        public string GetMemberLevel(string memberId)
        {
            int points = GetMemberPoints(memberId);
            if (points >= 300) return "VIP";
            if (points >= 150) return "金級";
            if (points >= 50) return "銀級";
            return "銅級";
        }

        // 查詢會員點數使用紀錄
        public List<PointLog> GetPointHistory(string memberId)
        {
            return PointLogs.Where(p => p.MemberId == memberId)
                            .OrderByDescending(p => p.CreateTime)
                            .ToList();
        }

        // 發送生日優惠券
        public void DistributeBirthdayCoupon(string memberId)
        {
            var user = Users.FirstOrDefault(u => u.Id == memberId);
            if (user == null || user.Birthday == null) return;

            if (user.Birthday?.Month != DateTime.Today.Month || user.Birthday?.Day != DateTime.Today.Day)
                return;

            var birthdayCoupon = Coupons.FirstOrDefault(c => c.Title.Contains("生日") && c.IsActive);

            if (birthdayCoupon != null &&
                !MemberCoupons.Any(mc => mc.MemberId == memberId && mc.CouponId == birthdayCoupon.CouponId))
            {
                MemberCoupons.Add(new MemberCoupon
                {
                    Id = MemberCoupons.Count + 1,
                    MemberId = memberId,
                    CouponId = birthdayCoupon.CouponId,
                    IsUsed = false
                });
            }
        }

        // 標記優惠券為已使用
        public bool MarkCouponUsed(string memberId, int couponId)
        {
            var mc = MemberCoupons.FirstOrDefault(m => m.MemberId == memberId && m.CouponId == couponId && !m.IsUsed);
            if (mc == null) return false;

            mc.IsUsed = true;
            mc.UsedDate = DateTime.Now;
            return true;
        }

        // 新增系統優惠券
        public void AddSystemCoupon(Coupon coupon)
        {
            coupon.CouponId = Coupons.Count + 1;
            Coupons.Add(coupon);
        }
    }


}
