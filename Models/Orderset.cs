using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.Linq;
namespace Web0524.Models
{
    public class Orderset
    {
        public int? Sid { get; set; }
        [Required(ErrorMessage = "為選擇產品")]
        [DataType(DataType.Text)]
        public string? Pid { get; set; }

        [BindNever] // 設定 Photo 屬性在模型繫結時不會受到影響
        public byte[] Photo { get; set; }
        public string? Pname { get; set; }
        public double? Price { get; set; }
        public string? Unit { get; set; }
        [Required(ErrorMessage = "未選擇日期")]
        [DataType(DataType.Text)]
        public DateTime? Date { get; set; }
        [Required(ErrorMessage = "未選擇時間")]
        [DataType(DataType.Text)]
        public TimeSpan? Time { get; set; }
        public string? Event { get; set; }
        public string? Uid { get; set; }
        public string? Uname { get; set; }
        public string? Line { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        [Required(ErrorMessage = "未選擇地點")]
        [DataType(DataType.Text)]
        public int? Placeid { get; set; }

        public double SpendTime { get; set; }
        public DateTime? Orderdate { get; set; }
    }
}
