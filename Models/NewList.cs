using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
namespace Web0524.Models
{
    public class NewList
    {
        public int? NewId { get; set; }
        [BindProperty]
        [Required(ErrorMessage = "標題是必要的")]
        public string? Title { get; set; }

        public string? Content { get; set; }
        [BindProperty]
        [Required(ErrorMessage = "作者是必要的")]
        public string? Author { get; set; }
        [BindProperty]
        [Required(ErrorMessage = "日期是必要的")]
        public DateTime? PublishDate { get; set; }
        [BindProperty]
        [Required(ErrorMessage = "狀態是必要的")]
        public int? Status { get; set; }
        //0=上架1=至頂2=下嫁
        public string? Category { get; set; }
        //0=新聞;1=優惠
        public string? Tags { get; set; }

        public byte[] Photo { get; set; }
    }
}
