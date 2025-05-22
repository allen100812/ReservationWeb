using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace Web0524.Models
{
    public interface IOrdersetService
    {

        int AddOrderset(Orderset orderset);
        bool UpdateOrderset(Orderset orderset);
        bool UpdateOrdersetEvent(string sid, string Event);
        bool DeleteOrderset(string Sid);
        bool UpdateExpired();
        Orderset GetOrdersetById(string Sid);

        int GetOrdersetByPlaceCount(string Placeid);
        IEnumerable<Orderset> GetOrdersetByDate(DateTime Date,string Sid);

        IEnumerable<Orderset> GetOrdersetInDate(DateTime DateStart, DateTime DateEnd, string Sid);
        IEnumerable<Orderset> GetOrdersetByPid(string Pid);
        IEnumerable<Orderset> GetOrdersetByUid(string Uid);

        int GetOrdersetByUidCountToday(string Uid, DateTime NowTime);

        bool GetOrdersetByUidInMinute(string Uid, DateTime NowTime);
        IEnumerable<Orderset> GetOrdersetByKey(string key);
        IEnumerable<Orderset> GetOrdersetTB();
    }
    public class OrdersetService: IOrdersetService
    {
        private readonly IDbConnection _dbConnection;
        public OrdersetService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public Orderset GetOrdersetById(string Sid)
        {
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, c.Phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrdersetTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.id = c.id";
            sql = sql + " WHERE a.Sid = @Sid";
            return _dbConnection.QueryFirstOrDefault<Orderset>(sql, new { Sid = Sid });

        }
        public int GetOrdersetByPlaceCount(string Placeid)
        {
            var sql = "SELECT count(*) FROM OrdersetTB where Placeid=@Placeid";
            return _dbConnection.QueryFirstOrDefault<int>(sql, new { Placeid = Placeid });

        }
        public IEnumerable<Orderset> GetOrdersetByDate(DateTime Date,string Sid)
        {
            string DateStr = Date.ToString("yyyy-MM-dd");
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, b.SpendTime as SpendTime, c.Phone AS phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrdersetTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.id = c.id";
            sql = sql + " WHERE a.Date = @DateStr AND a.Sid <> @Sid";
            return _dbConnection.Query<Orderset>(sql, new { DateStr = DateStr, Sid = Sid });
        }
        public IEnumerable<Orderset> GetOrdersetInDate(DateTime DateStart, DateTime DateEnd, string Sid)
        {
            string DateStrStart = DateStart.ToString("yyyy-MM-dd");
            string DateStrEnd = DateEnd.ToString("yyyy-MM-dd");
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, b.SpendTime as SpendTime, c.Phone AS phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrdersetTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.id = c.id";
            sql = sql + " WHERE a.Date BETWEEN @DateStrStart AND @DateStrEnd AND a.Sid <> @Sid";
            return _dbConnection.Query<Orderset>(sql, new { DateStrStart = DateStrStart, DateStrEnd = DateStrEnd, Sid = Sid });
        }
        public IEnumerable<Orderset> GetOrdersetByPid(string Pid)
        {
            var sql = "Select a.Sid,a.Pid,b.Photo as Photo,b.Name as Pname, b.SpendTime as SpendTime,c.Phone,c.Line,c.Email,b.Price,Isnull(b.Unit,'') as Unit,a.Date,a.Time,a.Event as Event,isnull(c.id,'') as Uid,isnull(c.Name,'無') as Uname a.Placeid,a.Orderdate From OrdersetTB a";
            sql = sql + " Left Join ProductTB b on a.Pid=b.Pid ";
            sql = sql + " Left Join UserTB c on a.id=c.id ";
            sql = sql + " WHERE Pid = @Pid";
            return _dbConnection.Query<Orderset>(sql, new { Pid = Pid });
        }
        public IEnumerable<Orderset> GetOrdersetByUid(string Uid)
        {
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, b.SpendTime as SpendTime, c.Phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.Id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrdersetTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.Id = c.Id";
            sql = sql + " WHERE a.Id = @Uid";
            return _dbConnection.Query<Orderset>(sql, new { Uid = Uid });
        }
        public int GetOrdersetByUidCountToday(string Uid, DateTime NowTime)
        {
            string NowTimeStr = NowTime.ToString("yyyy/MM/dd");
            var sql = "SELECT count(*) FROM OrdersetTB Where id=@Uid and FORMAT(Orderdate, 'yyyy/MM/dd')=@NowTimeStr";
            var count = _dbConnection.ExecuteScalar<int>(sql, new { Uid = Uid, NowTimeStr = NowTimeStr });
            return count;
        }
        public IEnumerable<Orderset> GetOrdersetByKey(string key)
        {
            var sql = "Select a.Sid,a.Pid,b.Photo as Photo,b.Name as Pname, b.SpendTime as SpendTime,c.Phone,c.Line,c.Email,b.Price,Isnull(b.Unit,'') as Unit,a.Date,a.Time,a.Event as Event,isnull(c.id,'') as Uid,isnull(c.Name,'無') as Uname ,a.Placeid,a.Orderdate From OrdersetTB a";
            sql = sql + " Left Join ProductTB b on a.Pid=b.Pid ";
            sql = sql + " Left Join UserTB c on a.id=c.id ";
            sql += " WHERE CONCAT('S' + RIGHT('000000' + CAST(a.Sid AS VARCHAR(6)), 6),b.Name, FORMAT(a.Date, 'yyyy/M/d'), FORMAT(a.Time, 'H:m'), (CASE a.Event WHEN 0 THEN '待接受' WHEN 1 THEN '已接受' WHEN 9 THEN '已取消' WHEN 10 THEN '未接受' WHEN 11 THEN '已完成' WHEN 19 THEN '已取消' END), ISNULL(c.Name, '無')) LIKE CONCAT('%', @Key, '%')";
            return _dbConnection.Query<Orderset>(sql, new { Key = key });
        }
        public IEnumerable<Orderset> GetOrdersetTB()
        {
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, b.SpendTime as SpendTime, c.Phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrdersetTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.id = c.id";
            return _dbConnection.Query<Orderset>(sql);

        }

        public bool GetOrdersetByUidInMinute(string Uid,DateTime NowTime)
        {
            string NowTimeStr = NowTime.ToString("yyyy/MM/dd HH:mm");
            var sql = "SELECT count(*) FROM OrdersetTB Where id=@Uid and FORMAT(Orderdate, 'yyyy/MM/dd HH:mm')=@NowTimeStr";
            var count = _dbConnection.ExecuteScalar<int>(sql, new { Uid = Uid, NowTimeStr = NowTimeStr });
            return count > 0;
        }
        public int AddOrderset(Orderset orderset)
        {
            var sql = "INSERT INTO OrdersetTB (Pid, Date, Time, Event, id, Placeid, Orderdate) VALUES (@Pid, @Date, @Time, 0, @Uid, @Placeid, (SELECT CONVERT(DATETIMEOFFSET, SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time'))); SELECT CAST(SCOPE_IDENTITY() as int);";
            var insertedId = _dbConnection.ExecuteScalar<int>(sql, orderset);
            return insertedId;
        }
        public bool UpdateOrderset(Orderset orderset)
        {
            var sql = "UPDATE OrdersetTB SET Pid=@Pid,Date=@Date,Time=@Time,Event=@Event,id=@id,Placeid=@Placeid WHERE Sid = @Sid";
            var affectedRows = _dbConnection.Execute(sql, orderset);
            return affectedRows > 0;
        }
        public bool UpdateOrdersetEvent(string sid,string Event)
        {
            var sql = "UPDATE OrdersetTB set Event=@Event WHERE Sid = @Sid";
            var parameters = new { Event = Event, Sid = sid };
            var affectedRows = _dbConnection.Execute(sql, parameters);
            return affectedRows > 0;
        }
        public bool DeleteOrderset(string Sid)
        {
            var sql = "DELETE FROM OrdersetTB WHERE Sid= @Sid";
            var affectedRows = _dbConnection.Execute(sql, new { Sid = Sid });
            return affectedRows > 0;
        }
        public bool UpdateExpired()
        {
            var sql = "update  OrdersetTB set [Event] = 10 Where Date < DATEADD(DAY, DATEDIFF(DAY, 0, dateadd(hh,8,getdate())), '00:00') and Event = 0";
            sql = sql+ " update  OrdersetTB set [Event] = 11 Where Date < DATEADD(DAY, DATEDIFF(DAY, 0, dateadd(hh,8,getdate())), '00:00') and Event = 1 ";
            sql = sql + " update  OrdersetTB set [Event] = 19 Where Date < DATEADD(DAY, DATEDIFF(DAY, 0, dateadd(hh,8,getdate())), '00:00') and Event = 9 ";
            var affectedRows = _dbConnection.Execute(sql);
            return affectedRows > 0;
        }
    }
}
