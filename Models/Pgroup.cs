using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{ 
    public class Pgroup
    {
        public int? Pgid { get; set; }
        [BindProperty]
        [Required(ErrorMessage = "分類名稱字段是必需的")]
        public string? Pgname { get; set; }
        public string? Pgcontent { get; set; }
        public string? Pgorder { get; set; }
    }
}
