using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
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
        public IActionResult OnGet()
        {
            var check = _userService.CheckCurrentUserPermission(this);
            if (check != null) return check;

            return Page();
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnGetList(string? keyword)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }




            var users = _userService.GetUserTB();
            var currentUser = _userService.GetUserById(userId); // 根據登入帳號
            int currentRole = currentUser?.Role ?? 5;
            
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

            var result = users.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                email = u.Email,
                phone = u.Phone,
                orderNum = u.OrderNum,
                cancelNum = u.CancelNum,
                remark = u.Remark,
                role = UserHelper.GetRoleName(u.Role),
                roleValue = u.Role,
                permission = $"P{u.PermissionSetId}",
                permissionValue = u.PermissionSetId,
                userType = UserHelper.GetUserTypeName(u.UserType),
                isDeleted = u.IsDeleted
            }).ToList();

            return new JsonResult(new { users = result, currentRole });
        }


        [IgnoreAntiforgeryToken]
        public JsonResult OnPostUpdateRemark(string id, string remark)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }

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
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
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
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            Console.WriteLine("id:" + id);
            var success = _userService.DeleteUser(id);
            return new JsonResult(new { success, message = success ? "已停權該用戶" : "停權失敗" });
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostEnableUser(string id)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            
            var success = _userService.RestoreUser(id);
            return new JsonResult(new { success, message = success ? "已恢復該用戶" : "恢復失敗" });
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostEditUser(UserEditDto userEdit)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            var user = _userService.GetUserById(userEdit.Id);
            if (user == null)
                return new JsonResult(new { success = false, message = "找不到用戶" });

            user.Name = userEdit.Name?.Trim();
            user.Email = userEdit.Email?.Trim();
            user.Phone = userEdit.Phone?.Trim();
            user.Role = userEdit.Role;
            user.PermissionSetId = userEdit.Permission;
            user.Remark = userEdit.Remark?.Trim();

            var success = _userService.UpdateUser(user);
            return new JsonResult(new { success, message = success ? "會員資料已更新" : "更新失敗" });
        }

        public class UserEditDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public int Role { get; set; }
            public int Permission { get; set; }
            public string Remark { get; set; }
        }

    }
}
