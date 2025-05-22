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

        IEnumerable<User> GetUserTB();//不可取得密碼
        IEnumerable<User> GetUserByType(int UserType);//不可取得密碼
        User UserLogin(string id,string pwd);
        User GetUserById(string id);
        bool UpdateUser_Password(string id, string Password);
        bool UpdateUser_Remark(String Id, String Remark);
        bool UpdateUser_LineUserId(String Id, String LineUserId);
        bool AddUser(User user);
        bool UpdateUser(User user);
        bool DeleteUser(string id);

        bool OrderNumAdd1(String Id);

        bool CancelNumAdd1(String Id);

        List<UserPointStr> GetUserPointStr();

        List<(string uid, double point)> GetUserPointTB();
        List<(string uid, string Rank)> GetUserRank();
        void Iplog_save(string ip, string uid, int eid);
        int Iplog_CheckByIp(string ip, int eid);
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
            var sql = "SELECT id,Name,'' as 'Password',UserType,Address,Phone,Email,Line,Phone,Email,Line,Photo,OrderNum,CancelNum,Remark,LineUserId,Birthday FROM UserTB Where UserType <> 9 Order by UserType ASC";
            return _dbConnection.Query<User>(sql);
        }
        public List<(string uid, string Rank)> GetUserRank()
        {
            var sql = "SELECT id,CASE ";
            sql = sql + " WHEN (CASE WHEN CancelNum > 0 THEN CAST(CancelNum AS float) / OrderNum ELSE (CASE WHEN OrderNum > 0 THEN 0 ELSE 9 END) END) BETWEEN 0 AND 0.2 THEN 'A'";
            sql = sql + " WHEN (CASE WHEN CancelNum > 0 THEN CAST(CancelNum AS float) / OrderNum ELSE (CASE WHEN OrderNum > 0 THEN 0 ELSE 9 END) END) BETWEEN 0.2 AND 0.4 THEN 'B'";
            sql = sql + " ELSE 'C' END AS Rank";
            sql = sql + " FROM UserTB Where UserType <> 9 and case when CancelNum>0 then cast(CancelNum as float)/OrderNum else (case when OrderNum >0 then 0 else 9 end) end <2";
            sql = sql + " Order by case when CancelNum>0 then cast(CancelNum as float)/OrderNum else (case when OrderNum >0 then 0 else 9 end) end ASC,OrderNum DESC";
            return _dbConnection.Query<(string uid, string Rank)>(sql).ToList();
        }
        public List<(string uid, double point)> GetUserPointTB()
        {
            var sql = "SELECT id,case when OrderNum=0 then 0 when CancelNum=0 then 2 else cast(CancelNum as float)/OrderNum end as 'point' FROM UserTB WHERE UserType <> 9";
            return _dbConnection.Query<(string uid, double point)>(sql).ToList();
        }
        public List<UserPointStr> GetUserPointStr()
        {
            //
            List <UserPointStr>  userPointStr = new List<UserPointStr>
            {
                new UserPointStr(2,2.1, "冠軍", "不曾取消預約"),
                new UserPointStr(0,0.1, "優質", "很少取消預約"),
                new UserPointStr(0.1,0.3, "普通", "偶而取消預約"),
                new UserPointStr(0.3,0.5, "不良", "經常取消預約"),
                new UserPointStr(0.5,0.75, "差勁", "屢次取消預約"),
                new UserPointStr(0.75,1.1, "惡意", "惡意取消預約")
            };
            return userPointStr;
        }
        public IEnumerable<User> GetUserByType(int UserType)
        {
            var sql = "SELECT id,Name,'' as 'Password',UserType,Address,Phone,Email,Line,Phone,Email,Line,Photo,OrderNum,CancelNum,Remark,LineUserId,Birthday FROM UserTB WHERE UserType <= @UserType";
            return _dbConnection.Query<User>(sql, new { UserType = UserType });
        }

        public User GetUserById(string id)
        {
            var sql = "SELECT * FROM UserTB WHERE Id = @Id";
            return _dbConnection.QueryFirstOrDefault<User>(sql, new { Id = id });

        }
        public bool AddUser(User user)
        {
            //檢測是否有重複帳號
            if(GetUserById(user.Id) == null)
            {
                var sql = "INSERT INTO UserTB (Id,Password,Name,UserType,Address,Phone,Email,Line,Photo,OrderNum,CancelNum,Remark,LineUserId,Birthday) VALUES (@Id,@Password,@Name,@UserType,@Address,@Phone,@Email,@Line,@Photo,0,0,@Remark,@LineUserId,@Birthday)";
                var affectedRows = _dbConnection.Execute(sql, user);
                return affectedRows > 0;
            }
            else
            {
                return false;
            }

        }

        public bool UpdateUser(User user)
        {
            var sql = "UPDATE UserTB SET  Password = @Password,Name = @Name, UserType = @UserType,Address=@Address,Phone=@Phone,Email=@Email,Line=@Line,Photo=CASE WHEN @Photo IS NOT NULL THEN @Photo ELSE Photo END,Remark=@Remark,Birthday=@Birthday WHERE Id = @Id";
            var affectedRows = _dbConnection.Execute(sql, user);
            return affectedRows > 0;
        }

        public bool UpdateUser_Password(String Id,String Password)
        {
            User User = GetUserById(Id);
            if (User != null)
            {
                User.Password= Password;
                var sql = "UPDATE UserTB SET  Password = @Password WHERE Id = @Id";
                var affectedRows = _dbConnection.Execute(sql, User);
                return affectedRows > 0;
            }else { return false; }

        }
        public bool UpdateUser_Remark(String Id, String Remark)
        {
            User User = GetUserById(Id);
            if (User != null)
            {
                User.Remark = Remark;
                var sql = "UPDATE UserTB SET  Remark = @Remark WHERE Id = @Id";
                var affectedRows = _dbConnection.Execute(sql, User);
                return affectedRows > 0;
            }
            else { return false; }

        }
        public bool UpdateUser_LineUserId(String Id, String LineUserId)
        {
            User User = GetUserById(Id);
            if (User != null)
            {
                User.LineUserId = LineUserId;
                var sql = "UPDATE UserTB SET  LineUserId = @LineUserId WHERE Id = @Id";
                var affectedRows = _dbConnection.Execute(sql, User);
                return affectedRows > 0;
            }
            else { return false; }

        }
        public bool OrderNumAdd1(String Id)
        {
            User User = GetUserById(Id);
            if (User != null)
            {
                var sql = "UPDATE UserTB SET OrderNum = OrderNum+1 WHERE Id = @Id";
                var affectedRows = _dbConnection.Execute(sql);
                return affectedRows > 0;
            }
            else { return false; }

        }
        public bool CancelNumAdd1(String Id)
        {
            User User = GetUserById(Id);
            if (User != null)
            {
                var sql = "UPDATE UserTB SET CancelNum = CancelNum+1 WHERE Id = @Id";
                var affectedRows = _dbConnection.Execute(sql);
                return affectedRows > 0;
            }
            else { return false; }

        }
        public bool DeleteUser(string id)
        {
            var sql = "UPDATE UserTB SET Name='(Del)'+Name,UserType = 9 WHERE Id = @Id";
            var affectedRows = _dbConnection.Execute(sql, new { Id = id });
            return affectedRows > 0;
        }
        public User UserLogin(string id, string pwd)
        {
            var sql = "SELECT * FROM UserTB WHERE Id = @Id and PassWord=@PassWord and UserType <> 9";
            return _dbConnection.QueryFirstOrDefault<User>(sql, new { Id = id, PassWord = pwd });
        }

        public void Iplog_save(string ip,string uid,int eid)
        {
            //eid: 1=註冊 2=預約
            var sql = "INSERT INTO Iplog (Time,Ip,Eid,Uid) VALUES ((SELECT CONVERT(DATETIMEOFFSET, SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time')),@ip,@eid,@uid)";
            _dbConnection.Execute(sql, new { ip = ip,uid=uid,eid=eid });
        }
        public int Iplog_CheckByIp(string ip,int eid)
        {
            var sql = "select count(*) from Iplog where Ip=@ip and Eid=@eid AND CAST(TODATETIMEOFFSET(Time, '+08:00') AS DATE) = CAST(GETDATE() AS DATE);";
            return _dbConnection.QueryFirstOrDefault<int>(sql, new { ip = ip, eid = eid });
        }

    }

}
