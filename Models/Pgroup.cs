using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{ 
    public class Pgroup
    {
        [Required(ErrorMessage = "分類編號必填")]
        [DataType(DataType.Text)]
        public int PGid { get; set; }
        [BindProperty]
        [Required(ErrorMessage = "分類名稱必填")]
        [DataType(DataType.Text)]
        public string PGname { get; set; }

        [DataType(DataType.MultilineText)]
        public string PGcontent { get; set; } = string.Empty;

        [DataType(DataType.Text)]
        public string PGorder { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;
    }
}
