using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web0524.Models
{

    public static class PermissionHelper
    {
        public static Permission GetPermissionsForRole(UserRole role)
        {
            return role switch
            {
                UserRole.Developer => new Permission { CanView = true, CanEdit = true, CanDelete = true, CanApprove = true, CanManageUsers = true },
                UserRole.SuperAdmin => new Permission { CanView = true, CanEdit = true, CanDelete = true, CanApprove = true, CanManageUsers = true },
                UserRole.Admin => new Permission { CanView = true, CanEdit = true, CanDelete = true, CanManageUsers = true },
                UserRole.Editor => new Permission { CanView = true, CanEdit = true },
                UserRole.VipMember => new Permission { CanView = true },
                UserRole.Member => new Permission { CanView = true },
                _ => new Permission()
            };
        }
    }

    public enum UserRole
    {
        Developer,       // 開發者
        SuperAdmin,      // 最高管理員
        Admin,           // 管理員
        Editor,          // 小編
        VipMember,       // VIP會員
        Member           // 一般會員
    }

    public class Permission
    {
        public bool CanView { get; set; } = false;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public bool CanApprove { get; set; } = false;
        public bool CanManageUsers { get; set; } = false;
    }

    public class PermissionSet
    {
        public int Id { get; set; }  // 權限ID
        public string Name { get; set; } = "";  // 權限名稱（例如「管理員權限」）

        public bool CanView { get; set; } = false;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public bool CanApprove { get; set; } = false;
        public bool CanManageUsers { get; set; } = false;
    }

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

        public bool IsDeleted { get; set; } = false;

        // 新增角色與權限
        public string Role { get; set; } = "Member";
        //public string RoleString { get; set; } = "Member";
        public int PermissionSetId { get; set; } = 0;
    }
}
