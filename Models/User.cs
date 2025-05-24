using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{
    public class User
    {
        [Required(ErrorMessage = "帳號必填")]
        [DataType(DataType.Text)]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "姓名必填")]
        [DataType(DataType.Text)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "密碼必填")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "使用者類型必填")]
        [DataType(DataType.Text)]
        public string UserType { get; set; } = string.Empty;

        [DataType(DataType.MultilineText)]
        public string Address { get; set; } = string.Empty;

        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; } = string.Empty;

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Text)]
        public string Line { get; set; } = string.Empty;

        public byte[] Photo { get; set; } = Array.Empty<byte>();

        [DataType(DataType.Text)]
        public string Remark { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; } = null;

        [DataType(DataType.Text)]
        public string LineUserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "訂單數必填")]
        public int OrderNum { get; set; } = 0;

        [Required(ErrorMessage = "取消數必填")]
        public int CancelNum { get; set; } = 0;

        public bool IsDeleted { get; set; } = false; // 假刪除欄位
    }
}