namespace Web0524.Models
{
    public class Marketing
    {

        // 優惠券主檔
        public class Coupon
        {
            public int CouponId { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public decimal DiscountAmount { get; set; }
            public decimal MinAmount { get; set; }
            public DateTime ValidFrom { get; set; }
            public DateTime ValidTo { get; set; }
            public bool ForFirstTimeUser { get; set; }
            public bool IsActive { get; set; }
        }

        // 會員擁有的優惠券
        public class MemberCoupon
        {
            public int Id { get; set; }
            public string MemberId { get; set; } = string.Empty;
            public int CouponId { get; set; }
            public bool IsUsed { get; set; }
            public DateTime? UsedDate { get; set; }
        }

        // 點數紀錄
        public class PointLog
        {
            public int Id { get; set; }
            public string MemberId { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public int Points { get; set; }
            public DateTime CreateTime { get; set; }
            public string Remark { get; set; } = string.Empty;
        }
    }


}
