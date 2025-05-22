using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{
    public class Product
    {
        [BindProperty]
        [Required(ErrorMessage = "Pid字段是必需的")]
        public int ProductId { get; set; } // public int ServiceId { get; set; }

        public int? Pgid { get; set; }
        public int? ProductState { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Pname字段是必需的")]
        public string? Name { get; set; }
        public float? Price { get; set; }
        public string? Content { get; set; }
        public byte[] Photo { get; set; }
        public string? ProductOrder { get; set; }


    }
}
