using Dapper;
using System;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;

namespace Web0524.Models
{
    public interface IOrderService
    {

        int CreateOrder(Order order);

        // 更新訂單
        bool UpdateOrder(Order order);

        // 刪除訂單（依 ID）
        bool DeleteOrder(int orderId);

        // 取得所有訂單
        IEnumerable<Order> GetAllOrders();

        // 依訂單 ID 查詢
        Order GetOrderById(int orderId);

        // 依用戶 ID 查詢所有訂單
        IEnumerable<Order> GetOrdersByUser(string uid);

        // 依設計師 ID 查詢所有訂單
        IEnumerable<Order> GetOrdersByDesigner(int designerId);

        // 依產品 ID 查詢所有訂單
        IEnumerable<Order> GetOrdersByProduct(int productId);

        // 依狀態查詢
        IEnumerable<Order> GetOrdersByStatus(OrderStatus status);

        // 依時間區間查詢（預約時間）
        IEnumerable<Order> GetOrdersByReservationRange(DateTime start, DateTime end);

        // 依下單時間區間查詢
        IEnumerable<Order> GetOrdersByOrderDateRange(DateTime start, DateTime end);

        // 統計：某位設計師某天的預約筆數
        int CountReservationsByDesignerAndDate(int designerId, DateTime date);

        // 統計：每日總預約數（可用於月曆檢視）
        Dictionary<DateTime, int> GetDailyReservationCount(DateTime start, DateTime end);

        // 統計：總營收（可篩選日期區間）
        double GetTotalRevenue(DateTime? start = null, DateTime? end = null);

        // 變更訂單狀態
        bool ChangeOrderStatus(int orderId, OrderStatus newStatus);

        // 設定訂單備註
        bool UpdateOrderRemark(int orderId, string remark);

        // 驗證是否已存在某用戶在某時間的預約
        bool HasUserReservation(string uid, DateTime reservationDateTime);

        // 驗證某設計師在該時間是否已有預約
        bool IsDesignerAvailable(int designerId, DateTime reservationDateTime);
    }
    public class orderService: IOrderService
    {
        private readonly IDbConnection _dbConnection;
        public orderService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }
        public int CreateOrder(Order order)
        {
            var sql = @"
            INSERT INTO OrderTB (Status, DesignerId, ProductId, Price, PaymentMethod, ReservationDateTime, Uid, Remark, Orderdate)
            VALUES (@Status, @DesignerId, @ProductId, @Price, @PaymentMethod, @ReservationDateTime, @Uid, @Remark, @Orderdate)";
            return _dbConnection.Execute(sql, order);
        }

        public bool UpdateOrder(Order order)
        {
            var sql = @"
            UPDATE OrderTB
            SET Status = @Status,
                DesignerId = @DesignerId,
                ProductId = @ProductId,
                Price = @Price,
                PaymentMethod = @PaymentMethod,
                ReservationDateTime = @ReservationDateTime,
                Uid = @Uid,
                Remark = @Remark,
                Orderdate = @Orderdate
            WHERE OrderId = @OrderId";
            return _dbConnection.Execute(sql, order) > 0;
        }

        public bool DeleteOrder(int orderId)
        {
            var sql = "DELETE FROM OrderTB WHERE OrderId = @OrderId";
            return _dbConnection.Execute(sql, new { OrderId = orderId }) > 0;
        }

        public IEnumerable<Order> GetAllOrders()
        {
            var sql = "SELECT * FROM OrderTB";
            return _dbConnection.Query<Order>(sql);
        }

        public Order GetOrderById(int orderId)
        {
            var sql = "SELECT * FROM OrderTB WHERE OrderId = @OrderId";
            return _dbConnection.QueryFirstOrDefault<Order>(sql, new { OrderId = orderId });
        }

        public IEnumerable<Order> GetOrdersByUser(string uid)
        {
            var sql = "SELECT * FROM OrderTB WHERE Uid = @Uid";
            return _dbConnection.Query<Order>(sql, new { Uid = uid });
        }

        public IEnumerable<Order> GetOrdersByDesigner(int designerId)
        {
            var sql = "SELECT * FROM OrderTB WHERE DesignerId = @DesignerId";
            return _dbConnection.Query<Order>(sql, new { DesignerId = designerId });
        }

        public IEnumerable<Order> GetOrdersByProduct(int productId)
        {
            var sql = "SELECT * FROM OrderTB WHERE ProductId = @ProductId";
            return _dbConnection.Query<Order>(sql, new { ProductId = productId });
        }

        public IEnumerable<Order> GetOrdersByStatus(OrderStatus status)
        {
            var sql = "SELECT * FROM OrderTB WHERE Status = @Status";
            return _dbConnection.Query<Order>(sql, new { Status = status });
        }

        public IEnumerable<Order> GetOrdersByReservationRange(DateTime start, DateTime end)
        {
            var sql = "SELECT * FROM OrderTB WHERE ReservationDateTime BETWEEN @Start AND @End";
            return _dbConnection.Query<Order>(sql, new { Start = start, End = end });
        }

        public IEnumerable<Order> GetOrdersByOrderDateRange(DateTime start, DateTime end)
        {
            var sql = "SELECT * FROM OrderTB WHERE Orderdate BETWEEN @Start AND @End";
            return _dbConnection.Query<Order>(sql, new { Start = start, End = end });
        }

        public int CountReservationsByDesignerAndDate(int designerId, DateTime date)
        {
            var sql = "SELECT COUNT(*) FROM OrderTB WHERE DesignerId = @DesignerId AND CAST(ReservationDateTime AS DATE) = @Date";
            return _dbConnection.ExecuteScalar<int>(sql, new { DesignerId = designerId, Date = date.Date });
        }

        public Dictionary<DateTime, int> GetDailyReservationCount(DateTime start, DateTime end)
        {
            var sql = @"
            SELECT CAST(ReservationDateTime AS DATE) AS Date, COUNT(*) AS Count
            FROM OrderTB
            WHERE ReservationDateTime BETWEEN @Start AND @End
            GROUP BY CAST(ReservationDateTime AS DATE)";
            return _dbConnection.Query(sql, new { Start = start, End = end })
                .ToDictionary(row => (DateTime)row.Date, row => (int)row.Count);
        }

        public double GetTotalRevenue(DateTime? start = null, DateTime? end = null)
        {
            string sql = "SELECT SUM(Price) FROM OrderTB WHERE 1=1";
            if (start.HasValue && end.HasValue)
                sql += " AND Orderdate BETWEEN @Start AND @End";

            return _dbConnection.ExecuteScalar<double>(sql, new { Start = start, End = end });
        }

        public bool ChangeOrderStatus(int orderId, OrderStatus newStatus)
        {
            var sql = "UPDATE OrderTB SET Status = @Status WHERE OrderId = @OrderId";
            return _dbConnection.Execute(sql, new { OrderId = orderId, Status = newStatus }) > 0;
        }

        public bool UpdateOrderRemark(int orderId, string remark)
        {
            var sql = "UPDATE OrderTB SET Remark = @Remark WHERE OrderId = @OrderId";
            return _dbConnection.Execute(sql, new { OrderId = orderId, Remark = remark }) > 0;
        }

        public bool HasUserReservation(string uid, DateTime reservationDateTime)
        {
            var sql = "SELECT COUNT(*) FROM OrderTB WHERE Uid = @Uid AND ReservationDateTime = @ReservationDateTime";
            return _dbConnection.ExecuteScalar<int>(sql, new { Uid = uid, ReservationDateTime = reservationDateTime }) > 0;
        }

        public bool IsDesignerAvailable(int designerId, DateTime reservationDateTime)
        {
            var sql = "SELECT COUNT(*) FROM OrderTB WHERE DesignerId = @DesignerId AND ReservationDateTime = @ReservationDateTime";
            return _dbConnection.ExecuteScalar<int>(sql, new { DesignerId = designerId, ReservationDateTime = reservationDateTime }) == 0;
        }
    }
}
