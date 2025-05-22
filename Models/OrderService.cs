using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace Web0524.Models
{
    public interface IOrderService
    {

        int AddOrder(Order order);
        bool UpdateOrder(Order order);
        bool UpdateOrderEvent(string sid, string Event);
        bool DeleteOrder(string Sid);
        bool UpdateExpired();
        Order GetOrderById(string Sid);

        int GetOrderByPlaceCount(string Placeid);
        IEnumerable<Order> GetOrderByDate(DateTime Date,string Sid);

        IEnumerable<Order> GetOrderInDate(DateTime DateStart, DateTime DateEnd, string Sid);
        IEnumerable<Order> GetOrderByPid(string Pid);
        IEnumerable<Order> GetOrderByUid(string Uid);

        int GetOrderByUidCountToday(string Uid, DateTime NowTime);

        bool GetOrderByUidInMinute(string Uid, DateTime NowTime);
        IEnumerable<Order> GetOrderByKey(string key);
        IEnumerable<Order> GetOrderTB();
    }
    public class orderService: IOrderService
    {
        private readonly IDbConnection _dbConnection;
        public orderService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public Order GetOrderById(string Sid)
        {
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, c.Phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrderTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.id = c.id";
            sql = sql + " WHERE a.Sid = @Sid";
            return _dbConnection.QueryFirstOrDefault<Order>(sql, new { Sid = Sid });

        }
        public int GetOrderByPlaceCount(string Placeid)
        {
            var sql = "SELECT count(*) FROM OrderTB where Placeid=@Placeid";
            return _dbConnection.QueryFirstOrDefault<int>(sql, new { Placeid = Placeid });

        }
        public IEnumerable<Order> GetOrderByDate(DateTime Date,string Sid)
        {
            string DateStr = Date.ToString("yyyy-MM-dd");
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, b.SpendTime as SpendTime, c.Phone AS phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrderTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.id = c.id";
            sql = sql + " WHERE a.Date = @DateStr AND a.Sid <> @Sid";
            return _dbConnection.Query<Order>(sql, new { DateStr = DateStr, Sid = Sid });
        }
        public IEnumerable<Order> GetOrderInDate(DateTime DateStart, DateTime DateEnd, string Sid)
        {
            string DateStrStart = DateStart.ToString("yyyy-MM-dd");
            string DateStrEnd = DateEnd.ToString("yyyy-MM-dd");
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, b.SpendTime as SpendTime, c.Phone AS phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrderTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.id = c.id";
            sql = sql + " WHERE a.Date BETWEEN @DateStrStart AND @DateStrEnd AND a.Sid <> @Sid";
            return _dbConnection.Query<Order>(sql, new { DateStrStart = DateStrStart, DateStrEnd = DateStrEnd, Sid = Sid });
        }
        public IEnumerable<Order> GetOrderByPid(string Pid)
        {
            var sql = "Select a.Sid,a.Pid,b.Photo as Photo,b.Name as Pname, b.SpendTime as SpendTime,c.Phone,c.Line,c.Email,b.Price,Isnull(b.Unit,'') as Unit,a.Date,a.Time,a.Event as Event,isnull(c.id,'') as Uid,isnull(c.Name,'無') as Uname a.Placeid,a.Orderdate From OrderTB a";
            sql = sql + " Left Join ProductTB b on a.Pid=b.Pid ";
            sql = sql + " Left Join UserTB c on a.id=c.id ";
            sql = sql + " WHERE Pid = @Pid";
            return _dbConnection.Query<Order>(sql, new { Pid = Pid });
        }
        public IEnumerable<Order> GetOrderByUid(string Uid)
        {
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, b.SpendTime as SpendTime, c.Phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.Id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrderTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.Id = c.Id";
            sql = sql + " WHERE a.Id = @Uid";
            return _dbConnection.Query<Order>(sql, new { Uid = Uid });
        }
        public int GetOrderByUidCountToday(string Uid, DateTime NowTime)
        {
            string NowTimeStr = NowTime.ToString("yyyy/MM/dd");
            var sql = "SELECT count(*) FROM OrderTB Where id=@Uid and FORMAT(Orderdate, 'yyyy/MM/dd')=@NowTimeStr";
            var count = _dbConnection.ExecuteScalar<int>(sql, new { Uid = Uid, NowTimeStr = NowTimeStr });
            return count;
        }
        public IEnumerable<Order> GetOrderByKey(string key)
        {
            var sql = "Select a.Sid,a.Pid,b.Photo as Photo,b.Name as Pname, b.SpendTime as SpendTime,c.Phone,c.Line,c.Email,b.Price,Isnull(b.Unit,'') as Unit,a.Date,a.Time,a.Event as Event,isnull(c.id,'') as Uid,isnull(c.Name,'無') as Uname ,a.Placeid,a.Orderdate From OrderTB a";
            sql = sql + " Left Join ProductTB b on a.Pid=b.Pid ";
            sql = sql + " Left Join UserTB c on a.id=c.id ";
            sql += " WHERE CONCAT('S' + RIGHT('000000' + CAST(a.Sid AS VARCHAR(6)), 6),b.Name, FORMAT(a.Date, 'yyyy/M/d'), FORMAT(a.Time, 'H:m'), (CASE a.Event WHEN 0 THEN '待接受' WHEN 1 THEN '已接受' WHEN 9 THEN '已取消' WHEN 10 THEN '未接受' WHEN 11 THEN '已完成' WHEN 19 THEN '已取消' END), ISNULL(c.Name, '無')) LIKE CONCAT('%', @Key, '%')";
            return _dbConnection.Query<Order>(sql, new { Key = key });
        }
        public IEnumerable<Order> GetOrderTB()
        {
            var sql = "SELECT a.Sid, a.Pid, b.Photo AS Photo, b.Name AS Pname, b.SpendTime as SpendTime, c.Phone, c.Line, c.Email, b.Price, ISNULL(b.Unit, '') AS Unit, a.Date, a.Time, a.Event AS Event, ISNULL(c.id, '') AS Uid, ISNULL(c.Name, '無') AS Uname, a.Placeid,a.Orderdate FROM OrderTB a";
            sql = sql + " LEFT JOIN ProductTB b ON a.Pid = b.Pid";
            sql = sql + " LEFT JOIN UserTB c ON a.id = c.id";
            return _dbConnection.Query<Order>(sql);

        }

        public bool GetOrderByUidInMinute(string Uid,DateTime NowTime)
        {
            string NowTimeStr = NowTime.ToString("yyyy/MM/dd HH:mm");
            var sql = "SELECT count(*) FROM OrderTB Where id=@Uid and FORMAT(Orderdate, 'yyyy/MM/dd HH:mm')=@NowTimeStr";
            var count = _dbConnection.ExecuteScalar<int>(sql, new { Uid = Uid, NowTimeStr = NowTimeStr });
            return count > 0;
        }
        public int AddOrder(Order order)
        {
            var sql = "INSERT INTO OrderTB (Pid, Date, Time, Event, id, Placeid, Orderdate) VALUES (@Pid, @Date, @Time, 0, @Uid, @Placeid, (SELECT CONVERT(DATETIMEOFFSET, SYSDATETIMEOFFSET() AT TIME ZONE 'Taipei Standard Time'))); SELECT CAST(SCOPE_IDENTITY() as int);";
            var insertedId = _dbConnection.ExecuteScalar<int>(sql, order);
            return insertedId;
        }
        public bool UpdateOrder(Order order)
        {
            var sql = "UPDATE OrderTB SET Pid=@Pid,Date=@Date,Time=@Time,Event=@Event,id=@id,Placeid=@Placeid WHERE Sid = @Sid";
            var affectedRows = _dbConnection.Execute(sql, order);
            return affectedRows > 0;
        }
        public bool UpdateOrderEvent(string sid,string Event)
        {
            var sql = "UPDATE OrderTB set Event=@Event WHERE Sid = @Sid";
            var parameters = new { Event = Event, Sid = sid };
            var affectedRows = _dbConnection.Execute(sql, parameters);
            return affectedRows > 0;
        }
        public bool DeleteOrder(string Sid)
        {
            var sql = "DELETE FROM OrderTB WHERE Sid= @Sid";
            var affectedRows = _dbConnection.Execute(sql, new { Sid = Sid });
            return affectedRows > 0;
        }
        public bool UpdateExpired()
        {
            var sql = "update  OrderTB set [Event] = 10 Where Date < DATEADD(DAY, DATEDIFF(DAY, 0, dateadd(hh,8,getdate())), '00:00') and Event = 0";
            sql = sql+ " update  OrderTB set [Event] = 11 Where Date < DATEADD(DAY, DATEDIFF(DAY, 0, dateadd(hh,8,getdate())), '00:00') and Event = 1 ";
            sql = sql + " update  OrderTB set [Event] = 19 Where Date < DATEADD(DAY, DATEDIFF(DAY, 0, dateadd(hh,8,getdate())), '00:00') and Event = 9 ";
            var affectedRows = _dbConnection.Execute(sql);
            return affectedRows > 0;
        }
    }
}
