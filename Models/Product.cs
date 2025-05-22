using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{
    public class Product
    {
        [BindProperty]
        [Required(ErrorMessage = "Pid字段是必需的")]
        public string? Pid { get; set; }
        [BindProperty]
        [Required(ErrorMessage = "Pname字段是必需的")]
        public string? Name { get; set; }

        public float? Price { get; set; }
        public string? Unit { get; set; }
        public string? Content { get; set; }
        public byte[] Photo { get; set; }
        public int? OrderSw { get; set; }
        public int? Pgid { get; set; }
        [BindProperty]
        [Required(ErrorMessage = "Event字段是必需的")]
        public int? Event { get; set; }
        public string? Pgname { get; set; }
        public string? Pgcontent { get; set; }
        public string? Pgorder { get; set; }
        public string? Porder { get; set; }

        public double SpendTime { get; set; }
    }
}
