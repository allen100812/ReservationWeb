using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{
    public class Product
    {
        [BindProperty]
        [Required(ErrorMessage = "產品編號 (ProductId) 是必填")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "缺少產品群組編號")]
        [DataType(DataType.Text)]
        public int PGid { get; set; }

        [Required(ErrorMessage = "缺少產品狀態")]
        [DataType(DataType.Text)]
        public int ProductState { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "產品名稱 (Name) 是必填")]
        [DataType(DataType.Text)]
        public string Name { get; set; }

        [Required(ErrorMessage = "缺少產品售價")]
        [DataType(DataType.Currency)]
        public float Price { get; set; }

        [DataType(DataType.MultilineText)]
        public string Content { get; set; } = string.Empty;

        [DataType(DataType.Upload)]
        public byte[] Photo { get; set; } = Array.Empty<byte>();

        [DataType(DataType.Text)]
        public string ProductOrder { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

    }
}
