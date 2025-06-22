using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web0524.Models
{
    public enum UserRoleEnum
    {
        Developer = 0,
        SuperAdmin = 1,
        Admin = 2,
        Editor = 3,
        VipMember = 4,
        Member = 5
    }

    public enum UserTypeEnum
    {
        Email = 0,
        Line = 1
    }

    public static class UserHelper
    {
        public static string GetRoleName(int roleInt)
        {
            return Enum.IsDefined(typeof(UserRoleEnum), roleInt)
                ? Enum.GetName(typeof(UserRoleEnum), roleInt)!
                : "未知角色";
        }

        public static string GetUserTypeName(int typeInt)
        {
            return Enum.IsDefined(typeof(UserTypeEnum), typeInt)
                ? Enum.GetName(typeof(UserTypeEnum), typeInt)!
                : "未知來源";
        }
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
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public int CouponPointManagement { get; set; }    // 0 or 1
        public int CreateCoupon { get; set; }             // 0 or 1
        public int DesignerManagement { get; set; }       // 0 or 1
        public int DesignerShiftManagement { get; set; }  // 0 or 1
        public int ManageReservation { get; set; }        // 0 or 1
        public int NewsManagement { get; set; }           // 0 or 1
        public int PGManagement { get; set; }             // 0 or 1
        public int PortfolioManagement { get; set; }      // 0 or 1
        public int ProductManagement { get; set; }        // 0 or 1
        public int UseCoupon { get; set; }                // 0 or 1
        public int UserManagement { get; set; }           // 0 or 1
    }


    public class User
    {
        [Required(ErrorMessage = "帳號必填")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "姓名必填")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "密碼必填")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "密碼需為至少8位英數混合")]
        public string Password { get; set; } = string.Empty;


        [Required(ErrorMessage = "使用者類型必填")]
        public int UserType { get; set; }

        public string Address { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Line { get; set; } = string.Empty;

        public byte[] Photo { get; set; } = Array.Empty<byte>();

        public string Remark { get; set; } = string.Empty;

        public DateTime? Birthday { get; set; } = null;

        public string LineUserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "訂單數量必填")]
        public int OrderNum { get; set; } = 0;

        [Required(ErrorMessage = "取消訂單數量必填")]
        public int CancelNum { get; set; } = 0;

        [Required(ErrorMessage = "用戶帳號狀態")]
        public bool IsDeleted { get; set; } = false;

        [Required(ErrorMessage = "角色必填")]
        public int Role { get; set; } = (int)UserRoleEnum.Member;
        [Required(ErrorMessage = "權限必填")]
        public int PermissionSetId { get; set; } = 5;

    }
}
