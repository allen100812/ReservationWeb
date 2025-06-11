using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class UserManagementModel : PageModel
    {
        private readonly IUserService _userService;

        public UserManagementModel(IUserService userService)
        {
            _userService = userService;
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnGetList(string? keyword)
        {
            var users = _userService.GetUserTB();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim().ToLower();
                users = users.Where(u =>
                    u.Id.ToLower().Contains(keyword) ||
                    u.Name.ToLower().Contains(keyword) ||
                    u.Email.ToLower().Contains(keyword) ||
                    u.Phone.ToLower().Contains(keyword)
                );
                
            }

            return new JsonResult(users.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                phone = u.Phone,
                orderNum = u.OrderNum,
                cancelNum = u.CancelNum,
                remark = u.Remark,
                role = PermissionHelper.GetRoleName(u.Role),
                permission = $"P{u.PermissionSetId}", // 或你可改抓名稱
                userType = PermissionHelper.GetUserTypeName(u.UserType),
                isDeleted = u.IsDeleted
            }));
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostUpdateRemark(string id, string remark)
        {
            var user = _userService.GetUserById(id);
            if (user == null)
                return new JsonResult(new { success = false, message = "找不到用戶" });

            user.Remark = remark;
            var success = _userService.UpdateUser(user);
            return new JsonResult(new { success, message = success ? "備註已更新" : "更新失敗" });
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostResetPassword(string id, string pwd)
        {
            if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 8)
                return new JsonResult(new { success = false, message = "密碼格式錯誤" });

            var user = _userService.GetUserById(id);
            if (user == null)
                return new JsonResult(new { success = false, message = "找不到用戶" });

            user.Password = pwd;
            var success = _userService.UpdateUser(user);
            return new JsonResult(new { success, message = success ? "密碼已更新" : "密碼更新失敗" });
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostDisableUser(string id)
        {
            var success = _userService.DeleteUser(id);
            return new JsonResult(new { success, message = success ? "已停權該用戶" : "停權失敗" });
        }
    }
}
