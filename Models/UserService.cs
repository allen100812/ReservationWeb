using Dapper;
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
        SELECT * FROM UserTB
        WHERE UserType <> 9 AND IsDeleted = 0
        ORDER BY UserType ASC";
            return _dbConnection.Query<User>(sql);
        }

        public User? GetUserById(string id)
        {
            var sql = "SELECT * FROM UserTB WHERE Id = @Id AND IsDeleted = 0";
            return _dbConnection.QueryFirstOrDefault<User>(sql, new { Id = id });
        }

        public bool CreateUser(User user)
        {
            var sql = @"
        INSERT INTO UserTB
        (Id, Name, Password, UserType, Address, Phone, Email, Line, Photo,
         OrderNum, CancelNum, Remark, Birthday, LineUserId, IsDeleted)
        VALUES
        (@Id, @Name, @Password, @UserType, @Address, @Phone, @Email, @Line, @Photo,
         @OrderNum, @CancelNum, @Remark, @Birthday, @LineUserId, 0)";
            return _dbConnection.Execute(sql, user) > 0;
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
            LineUserId = @LineUserId
        WHERE Id = @Id AND IsDeleted = 0";
            return _dbConnection.Execute(sql, user) > 0;
        }

        public bool DeleteUser(string id)
        {
            var sql = "UPDATE UserTB SET IsDeleted = 1 WHERE Id = @Id";
            return _dbConnection.Execute(sql, new { Id = id }) > 0;
        }

        public User UserLogin(string id, string pwd)
        {
            var sql = "SELECT * FROM UserTB WHERE Id = @Id AND Password = @Password AND UserType <> 9 AND IsDeleted = 0";
            return _dbConnection.QueryFirstOrDefault<User>(sql, new { Id = id, Password = pwd });
        }

        public bool RestoreUser(string id)
        {
            var sql = "UPDATE UserTB SET IsDeleted = 0 WHERE Id = @Id";
            return _dbConnection.Execute(sql, new { Id = id }) > 0;
        }

    }

}
