using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Linq;
namespace Web0524.Models
{
    public class Order
    {
        public int? OrderId { get; set; } //原本的public int OrderId { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending; //訂單狀態
        public int DesignerId { get; set; } //設計師名稱

        [Required(ErrorMessage = "為選擇產品")]
        [DataType(DataType.Text)]
        public int? ProductId { get; set; } //原本的public int ServiceId { get; set; } //服務項目

        [BindNever] // 設定 Photo 屬性在模型繫結時不會受到影響
        //public byte[] Photo { get; set; }
        public string? Pname { get; set; }
        public double? Price { get; set; }
        public string? Unit { get; set; }
        [Required(ErrorMessage = "未選擇日期")]
        [DataType(DataType.Text)]
        public DateTime ReservationDateTime { get; set; } //原本的public DateTime ReservationDateTime { get; set; } //預約時間
        [Required(ErrorMessage = "未選擇時間")]
        [DataType(DataType.Text)]
        //public TimeSpan? Time { get; set; }
        //public string? Event { get; set; }
        public string? Uid { get; set; }
        public string? Remark { get; set; }
        public DateTime? Orderdate { get; set; }

        public string? CustomerReviews { get; set; } //客戶反饋
        
        public string? Source { get; set; } //預約來源

    }
    public enum OrderStatus
    {
        Pending,    // 下單但尚未確認
        Confirmed,  // 成立
        Cancelled,  // 取消
        Completed   // 完成
    }
}
