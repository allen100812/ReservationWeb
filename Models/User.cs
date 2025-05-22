using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{
    public class User
    {
        [Required(ErrorMessage = "帳號必填")]
        [DataType(DataType.Text)]
        public string? Id { get; set; }

        public string? Name { get; set; }

        [Required(ErrorMessage = "密碼必填")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        public string? UserType { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Line { get; set; }

        public byte[] Photo { get; set; }
        public int? OrderNum { get; set; }
        public int? CancelNum { get; set; }
        public string? Remark { get; set; }
        public string? LineUserId { get; set; }
        public DateTime? Birthday { get; set; }
    }
}