using Dapper;
using Newtonsoft.Json;
using System.Data;



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

        IEnumerable<User> GetUserTB();
        User? GetUserById(string id);
        bool CreateUser(User user);
        bool UpdateUser(User user);
        bool DeleteUser(string id);
        User UserLogin(string id,string pwd);

        bool RestoreUser(string id);


        User? GetUserByLineUserId(string lineUserId);



    }

    public class UserService : IUserService
    {
        private readonly IDbConnection _dbConnection;
        public UserService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public IEnumerable<User> GetUserTB()
        {
            var sql = @"
SELECT Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
       OrderNum, CancelNum, Remark, Birthday, LineUserId, Role AS RoleString, Permissions AS PermissionsJson, IsDeleted
FROM UserTB
WHERE UserType <> 9 AND IsDeleted = 0
ORDER BY UserType ASC";

            var users = _dbConnection.Query<User>(sql).ToList();
            foreach (var user in users)
                MapUserPermissions(user);
            return users;
        }


        public User? GetUserById(string id)
        {
            var sql = @"
SELECT Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
       OrderNum, CancelNum, Remark, Birthday, LineUserId, Role AS RoleString, Permissions AS PermissionsJson, IsDeleted
FROM UserTB
WHERE Id = @Id AND IsDeleted = 0";

            var user = _dbConnection.QueryFirstOrDefault<User>(sql, new { Id = id });
            return MapUserPermissions(user);
        }

        public User? GetUserByLineUserId(string lineUserId)
        {
            var sql = @"
SELECT Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
       OrderNum, CancelNum, Remark, Birthday, LineUserId, Role AS RoleString, Permissions AS PermissionsJson, IsDeleted
FROM UserTB
WHERE LineUserId = @lineUserId AND IsDeleted = 0";

            var user = _dbConnection.QueryFirstOrDefault<User>(sql, new { lineUserId });
            return MapUserPermissions(user);
        }

        public bool CreateUser(User user)
        {
            var sql = @"
        INSERT INTO UserTB
        (Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
         OrderNum, CancelNum, Remark, Birthday, LineUserId, Role, Permissions, IsDeleted)
        VALUES
        (@Id, @Name, @Password, @UserType, @Address, @Phone, @Email, @Line, @Photo,
         @OrderNum, @CancelNum, @Remark, @Birthday, @LineUserId, @RoleString, @PermissionsJson, 0)";

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
                RoleString = user.Role.ToString(),
                PermissionsJson = JsonConvert.SerializeObject(user.Permissions)
            };

            return _dbConnection.Execute(sql, param) > 0;
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
            Role = @RoleString,
            Permissions = @PermissionsJson
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
                RoleString = user.Role.ToString(),
                PermissionsJson = JsonConvert.SerializeObject(user.Permissions)
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
       OrderNum, CancelNum, Remark, Birthday, LineUserId, Role AS RoleString, Permissions AS PermissionsJson, IsDeleted
FROM UserTB
WHERE Id = @Id AND Password = @Password AND UserType <> 9 AND IsDeleted = 0";

            var user = _dbConnection.QueryFirstOrDefault<User>(sql, new { Id = id, Password = pwd });
            return MapUserPermissions(user);
        }


        public bool RestoreUser(string id)
        {
            var sql = "UPDATE UserTB SET IsDeleted = 0 WHERE Id = @Id";
            return _dbConnection.Execute(sql, new { Id = id }) > 0;
        }



        private User? MapUserPermissions(User? user)
        {
            if (user == null) return null;

            // 先宣告 role 變數
            UserRole role;

            if (!string.IsNullOrEmpty(user.RoleString))
                Enum.TryParse(user.RoleString, out role);
            else
                role = UserRole.Member;

            user.Role = role;

            if (!string.IsNullOrEmpty(user.PermissionsJson))
            {
                try
                {
                    user.Permissions = JsonConvert.DeserializeObject<Permission>(user.PermissionsJson) ?? new Permission();
                }
                catch
                {
                    user.Permissions = new Permission();
                }
            }

            return user;
        }


    }

}
