using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Linq;
namespace Web0524.Models
{
    public class Order
    {
        [Required(ErrorMessage = "未提供訂單編號")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "未設定訂單狀態")]
        public OrderStatus Status { get; set; }

        [Required(ErrorMessage = "未選擇設計師")]
        public int DesignerId { get; set; }

        [Required(ErrorMessage = "未選擇產品")]
        [DataType(DataType.Text)]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "未設定金額")]
        [DataType(DataType.Currency)]
        public double Price { get; set; }

        [Required(ErrorMessage = "未設定支付模式")]
        [DataType(DataType.Currency)]
        public int PaymentMethod { get; set; }

        [Required(ErrorMessage = "未選擇預約日期")]
        [DataType(DataType.Date)]
        public DateTime ReservationDateTime { get; set; }

        [Required(ErrorMessage = "未指定用戶")]
        [DataType(DataType.Text)]
        public string Uid { get; set; } 

        [DataType(DataType.MultilineText)]
        public string Remark { get; set; } = string.Empty;

        [Required(ErrorMessage = "下單時間不正確")]
        [DataType(DataType.DateTime)]
        public DateTime Orderdate { get; set; }
    }
    public enum OrderStatus
    {
        Pending,    // 下單但尚未確認
        Confirmed,  // 成立
        Cancelled,  // 取消
        Completed   // 完成
    }
}
