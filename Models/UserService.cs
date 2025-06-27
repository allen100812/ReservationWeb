using Dapper;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Web0524.Models.Marketing;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Web0524.Models
{
    public class UserPointStr
    {
        public double Rate_s { get; set; }
        public double Rate_e { get; set; }
        public string Str1 { get; set; }
        public string Str2 { get; set; }

        public UserPointStr(double rate_s, double rate_e, string str1, string str2)
        {
            Rate_s = rate_s;
            Rate_e = rate_e;
            Str1 = str1;
            Str2 = str2;
        }
    }

    public interface IUserService
    {
        IActionResult CheckCurrentUserPermission(PageModel page);

        bool HasPagePermissionByName(string userId, string pageName);
        IEnumerable<User> GetUserTB();
        User? GetUserById(string id);
        bool CreateUser(User user);
        bool UpdateUser(User user);
        bool DeleteUser(string id);
        User UserLogin(string id, string pwd);
        bool RestoreUser(string id);
        User? GetUserByLineUserId(string lineUserId);
        bool UpdateUserEmail(string userId, string newEmail);
    }

    public class UserService : IUserService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IMarketingService _marketingService;

        public UserService(IDbConnection dbConnection, IMarketingService marketingService)
        {
            _dbConnection = dbConnection;
            _marketingService = marketingService;
        }

        public IActionResult CheckCurrentUserPermission(PageModel page)
        {
            // 從當前頁面的 HttpContext 中取出登入使用者 ID（SID）
            var userId = page.User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return page.RedirectToPage("/Login"); // 尚未登入就導去登入頁
            }

            var user = GetUserById(userId);
            if (user == null)
            {
                return page.RedirectToPage("/Login"); // 使用者不存在
            }

            // 自動比對頁面名稱（類別名）對應權限欄位
            var pageName = page.GetType().Name.Replace("Model", "");
            if (!HasPagePermissionByName(user.Id, pageName))
            {
                return page.RedirectToPage("/AccessDenied"); // 沒有該頁面權限
            }

            return null; // ✅ 通過驗證
        }

        public bool HasPagePermissionByName(string userId, string pageName)
        {
            var sql = "SELECT * FROM UserTB WHERE Id = @Id AND IsDeleted = 0";
            var user = _dbConnection.QueryFirstOrDefault<User>(sql, new { Id = userId });
            if (user == null) return false;

            var psql = "SELECT * FROM PermissionSetTB WHERE Id = @Id";
            var permission = _dbConnection.QueryFirstOrDefault<PermissionSet>(psql, new { Id = user.PermissionSetId });
            if (permission == null) return false;

            // 透過反射依欄位名稱取得對應屬性值
            var prop = typeof(PermissionSet).GetProperty(pageName);
            if (prop == null) return false;

            var value = prop.GetValue(permission);
            return value is int intVal && intVal == 1;
        }

        public IEnumerable<User> GetUserTB()
        {
            var sql = @"
SELECT Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
       OrderNum, CancelNum, Remark, Birthday, LineUserId, Role, PermissionSetId, IsDeleted
FROM UserTB
WHERE UserType <> 9
ORDER BY UserType ASC";

            return _dbConnection.Query<User>(sql).ToList();
        }

        public User? GetUserById(string id)
        {
            var sql = @"
SELECT Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
       OrderNum, CancelNum, Remark, Birthday, LineUserId, Role, PermissionSetId, IsDeleted
FROM UserTB
WHERE Id = @Id AND IsDeleted = 0";

            return _dbConnection.QueryFirstOrDefault<User>(sql, new { Id = id });
        }

        public User? GetUserByLineUserId(string lineUserId)
        {
            var sql = @"
SELECT Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
       OrderNum, CancelNum, Remark, Birthday, LineUserId, Role, PermissionSetId, IsDeleted
FROM UserTB
WHERE LineUserId = @lineUserId AND IsDeleted = 0";

            return _dbConnection.QueryFirstOrDefault<User>(sql, new { lineUserId });
        }

        public bool CreateUser(User user)
        {
            var sql = @"
INSERT INTO UserTB
(Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
 OrderNum, CancelNum, Remark, Birthday, LineUserId, Role, PermissionSetId, IsDeleted)
VALUES
(@Id, @Name, @Password, @UserType, @Address, @Phone, @Email, @Line, @Photo,
 @OrderNum, @CancelNum, @Remark, @Birthday, @LineUserId, @Role, @PermissionSetId, 0)";

            var param = new
            {
                user.Id,
                user.Name,
                user.Password,
                user.UserType,
                user.Address,
                user.Phone,
                user.Email,
                user.Line,
                user.Photo,
                user.OrderNum,
                user.CancelNum,
                user.Remark,
                user.Birthday,
                user.LineUserId,
                user.Role,
                user.PermissionSetId
            };

            var success = _dbConnection.Execute(sql, param) > 0;


            if (success)
            {
                // 呼叫派發優惠券邏輯
                _marketingService.AssignRegisterCoupons(user.Id);
            }

            return success;
        }

        public bool UpdateUser(User user)
        {
            var sql = @"
UPDATE UserTB SET
    Name = @Name,
    Password = @Password,
    UserType = @UserType,
    Address = @Address,
    Phone = @Phone,
    Email = @Email,
    Line = @Line,
    Photo = @Photo,
    OrderNum = @OrderNum,
    CancelNum = @CancelNum,
    Remark = @Remark,
    Birthday = @Birthday,
    LineUserId = @LineUserId,
    Role = @Role,
    PermissionSetId = @PermissionSetId
WHERE Id = @Id AND IsDeleted = 0";

            var param = new
            {
                user.Id,
                user.Name,
                user.Password,
                user.UserType,
                user.Address,
                user.Phone,
                user.Email,
                user.Line,
                user.Photo,
                user.OrderNum,
                user.CancelNum,
                user.Remark,
                user.Birthday,
                user.LineUserId,
                user.Role,
                user.PermissionSetId
            };

            return _dbConnection.Execute(sql, param) > 0;
        }

        public bool DeleteUser(string id)
        {
            var sql = "UPDATE UserTB SET IsDeleted = 1 WHERE Id = @Id";
            return _dbConnection.Execute(sql, new { Id = id }) > 0;
        }

        public User UserLogin(string id, string pwd)
        {
            var sql = @"
SELECT Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
       OrderNum, CancelNum, Remark, Birthday, LineUserId, Role, PermissionSetId, IsDeleted
FROM UserTB
WHERE Id = @Id AND Password = @Password AND UserType <> 9 AND IsDeleted = 0";

            return _dbConnection.QueryFirstOrDefault<User>(sql, new { Id = id, Password = pwd });
        }

        public bool RestoreUser(string id)
        {
            var sql = "UPDATE UserTB SET IsDeleted = 0 WHERE Id = @Id";
            return _dbConnection.Execute(sql, new { Id = id }) > 0;
        }


        public bool UpdateUserEmail(string userId, string newEmail)
        {
            if (_dbConnection.State != ConnectionState.Open)
                _dbConnection.Open();

            using var tran = _dbConnection.BeginTransaction();
            try
            {
                // 取得舊信箱
                var oldEmail = _dbConnection.ExecuteScalar<string>(
                    "SELECT Email FROM UserTB WHERE Id = @Id",
                    new { Id = userId }, tran);

                if (string.IsNullOrWhiteSpace(oldEmail))
                    return false;

                // 更新 UserTB 的 Email
                _dbConnection.Execute(
                    "UPDATE UserTB SET Email = @NewEmail , Id = @NewEmail WHERE Id = @Id",
                    new { Id = userId, NewEmail = newEmail }, tran);

                // 更新所有以舊 Email 當作 Id 的訂單記錄（假設 OrderTB.Id 是舊 email）
                _dbConnection.Execute(
                    "UPDATE OrderTB SET Uid = @NewEmail WHERE Uid = @OldEmail",
                    new { OldEmail = oldEmail, NewEmail = newEmail }, tran);

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                return false;
            }
        }

    }
}
