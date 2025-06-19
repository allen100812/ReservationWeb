using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{

    public class CouponDispatchRecord
    {
        public int RecordId { get; set; }                       // 派發記錄主鍵
        public int CouponId { get; set; }                       // 優惠券 ID
        public string MemberId { get; set; } = string.Empty;    // 使用者 ID
        public DateTime DispatchDate { get; set; }              // 派發日期
        public bool IsDispatched { get; set; } = false;         // 是否已被訂單使用
        public int? OrderId { get; set; }                       // 使用該券的訂單 ID（如有）
        public string? Note { get; set; }                       // 備註（來源、條件說明）
        public DateTime CreateTime { get; set; } = DateTime.Now; // 建立時間
    }

    // 優惠券主檔
    public enum DiscountTypeEnum
    {
        FixedAmount = 0,   // 固定金額折抵，如 NT$100
        Percentage = 1     // 百分比折扣，如 10%
    }

    public enum CouponSourceEnum
    {
        Manual = 0,        // 手動發放（管理者操作）
        Register = 1,      // 註冊禮
        FirstPurchase = 2, // 首購禮
        Birthday = 3,      // 生日券 
        Referral = 4,      // 推薦獎勵
        Campaign = 5       // 行銷活動
    }
    public enum AutoAssignRuleEnum
    {
        None = 0,               // 不指定
        RegisterNow = 1,        // 新會員註冊當天
        BeforeBirthday20 = 2,   // 生日前20天
        SpecificDate = 3        // 指定日期
    }

    public class Coupon
    {
        public int CouponId { get; set; }

        [Required(ErrorMessage = "請輸入優惠券標題")]
        [StringLength(100, ErrorMessage = "標題長度不可超過 100 字")]
        public string Title { get; set; }

        [Required(ErrorMessage = "系統未產生優惠券代碼")]
        public string Code { get; set; }

        [Required(ErrorMessage = "請選擇優惠券類型")]
        public DiscountTypeEnum DiscountType { get; set; } = DiscountTypeEnum.FixedAmount;

        [Range(0.01, 99999, ErrorMessage = "折扣值需大於 0")]
        public decimal DiscountAmount { get; set; }

        [Range(0.01, 99999, ErrorMessage = "固定折扣金額需大於 0")]
        public decimal? FixedDiscountAmount { get; set; }

        [Range(0, 999999, ErrorMessage = "門檻金額不可為負數")]
        public decimal MinAmount { get; set; }

        [Required(ErrorMessage = "請選擇起始日期")]
        public DateTime ValidFrom { get; set; }

        [Required(ErrorMessage = "請選擇結束日期")]
        public DateTime ValidTo { get; set; }

        public bool ForFirstTimeUser { get; set; }

        public bool IsWelcome { get; set; }

        public bool IsActive { get; set; }

        public CouponSourceEnum CouponSource { get; set; }

        public string? CategoryLimit { get; set; }

        public bool AutoAssign { get; set; }

        [Required(ErrorMessage = "請選擇自動派發邏輯")]
        public AutoAssignRuleEnum AutoAssignRule { get; set; }
        public DateTime? AutoDate { get; set; }



        [StringLength(500, ErrorMessage = "備註內容過長（最多 500 字）")]
        public string? Remark { get; set; }
    }



    // 點數紀錄
    public class PointLog
    {
        public int Id { get; set; }

        public string MemberId { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty; // 範例：註冊、推薦成功、消費回饋

        public int Points { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;

        public string Remark { get; set; } = string.Empty;

        public string? SourceOrderId { get; set; } // 可記錄扣/加點來源，例如哪張訂單
    }

}
