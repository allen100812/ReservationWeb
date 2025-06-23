using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // ← 確保引入這個命名空間
using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{
    public enum NewsTag
    {
        新聞 = 1,
        優惠 = 2,
        活動 = 3,
        公告 = 4,
        重要 = 5
    }

    public class NewList
    {
        public int? NewId { get; set; }

        [Required(ErrorMessage = "標題是必要的")]
        public string? Title { get; set; }

        public string? Content { get; set; }

        [Required(ErrorMessage = "作者是必要的")]
        public string? Author { get; set; }

        [Required(ErrorMessage = "日期是必要的")]
        public DateTime? PublishDate { get; set; }

        [Required(ErrorMessage = "狀態是必要的")]
        public int? Status { get; set; }

        public string? Category { get; set; }

        public int? Tag { get; set; }

        public DateTime? TopTime { get; set; } // 置頂時間（可排序）

        public List<IFormFile> Photos { get; set; } = new(); // 上傳圖檔
        public List<byte[]> PhotoList { get; set; } = new(); // 儲存圖檔內容

    }
}
