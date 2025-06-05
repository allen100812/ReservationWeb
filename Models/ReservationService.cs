using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using Dapper;


namespace Web0524.Models
{
    public interface IReservationService
    {


        // 判斷是否為固定休假日
        bool Reservation_IsFixedHoliday(int designerId, DateTime date);

        // 判斷是否為排休日
        bool Reservation_IsDayOff(int designerId, DateTime date);

        // 取得當日所有時段可預約的服務（依冷卻時間與提前限制）
        List<Reservation_AvailableServiceSlot> GetAvailableServiceSlots(int designerId, DateTime date, int cooldownMinutes, int advanceMinutes);

        // 檢查某設計師在指定時間是否可預約某服務
        bool IsSlotAvailable(int designerId, int ProductId, DateTime time);

        // 建立新預約單
        Order? CreateOrder(int designerId, int ProductId, DateTime time);

        // 更新預約單狀態（如完成、取消）
        bool UpdateOrderStatus(int orderId, OrderStatus newStatus);

        // 取得指定設計師在某天的所有預約
        List<Order> GetOrdersForDay(int designerId, DateTime date);

        // 取得預約單（依 ID）
        Order? GetOrderById(int orderId);

        // 取消預約單
        bool CancelOrder(int orderId);

        // 取得所有預約單
        List<Order> GetAllOrders();

        // 取得所有設計師清單
        List<Designer> GetAllDesigners();

        // 取得設計師資料（依 ID）
        Designer? GetDesignerById(int designerId);

        // 新增設計師
        Designer AddDesigner(Designer designer);

        // 更新設計師
        bool UpdateDesigner(Designer designer);

        // 刪除設計師
        bool DeleteDesigner(int designerId);

        // 新增排休資料
        Designer_Shift AddShift(Designer_Shift shift);

        // 移除排休
        bool RemoveShift(int designerId, DateTime shiftDate);

        // 查詢當天所有排休資料
        List<Designer_Shift> GetShiftsForDay(DateTime date);

        // 依會員 ID 取得該會員所有預約紀錄
        List<Order> GetOrdersByMemberId(string uid);

    }


    public class ReservationService : IReservationService
    {
        private readonly IDbConnection _dbConnection;

        public ReservationService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public Order? GetOrderById(int orderId)
        {
            var sql = "SELECT * FROM OrderTB WHERE OrderId = @OrderId";
            return _dbConnection.QueryFirstOrDefault<Order>(sql, new { OrderId = orderId });
        }

        public bool CancelOrder(int orderId)
        {
            var sql = "UPDATE OrderTB SET Status = @Status WHERE OrderId = @OrderId";
            return _dbConnection.Execute(sql, new { OrderId = orderId, Status = OrderStatus.Cancelled }) > 0;
        }

        public List<Order> GetAllOrders()
        {
            return _dbConnection.Query<Order>("SELECT * FROM OrderTB").ToList();
        }

        public List<Designer> GetAllDesigners()
        {
            var designers = _dbConnection.Query<Designer>("SELECT * FROM DesignerTB WHERE IsDeleted = 0").ToList();
            foreach (var d in designers)
            {
                d.ScheduleRules = _dbConnection.Query<Designer_ProductScheduleRule>(
                    "SELECT * FROM DesignerScheduleRuleTB WHERE DesignerId = @DesignerId",
                    new { DesignerId = d.DesignerId }).ToList();

                d.FixedHolidays = _dbConnection.Query<string>(
                    "SELECT WeekdayString FROM DesignerFixedHolidayTB WHERE DesignerId = @DesignerId",
                    new { DesignerId = d.DesignerId }).ToList();
            }
            return designers;
        }

        public Designer? GetDesignerById(int designerId)
        {
            var sql = "SELECT * FROM DesignerTB WHERE DesignerId = @DesignerId AND IsDeleted = 0";
            var designer = _dbConnection.QueryFirstOrDefault<Designer>(sql, new { DesignerId = designerId });
            if (designer != null)
            {
                designer.ScheduleRules = _dbConnection.Query<Designer_ProductScheduleRule>(
                    "SELECT * FROM DesignerScheduleRuleTB WHERE DesignerId = @DesignerId",
                    new { DesignerId = designerId }).ToList();

                designer.FixedHolidays = _dbConnection.Query<string>(
                    "SELECT WeekdayString FROM DesignerFixedHolidayTB WHERE DesignerId = @DesignerId",
                    new { DesignerId = designerId }).ToList();
            }
            return designer;
        }

        public Designer AddDesigner(Designer designer)
        {
            var sql = @"
        INSERT INTO DesignerTB (Name, Nickname, IsDeleted) 
        VALUES (@Name, @Nickname, 0);
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var newId = _dbConnection.ExecuteScalar<int>(sql, designer);
            designer.DesignerId = newId;
            return designer;
        }

        public bool UpdateDesigner(Designer designer)
        {
            var sql = "UPDATE DesignerTB SET Name = @Name, Nickname = @Nickname WHERE DesignerId = @DesignerId AND IsDeleted = 0";
            return _dbConnection.Execute(sql, designer) > 0;
        }

        public bool DeleteDesigner(int designerId)
        {
            var sql = "UPDATE DesignerTB SET IsDeleted = 1 WHERE DesignerId = @DesignerId";
            return _dbConnection.Execute(sql, new { DesignerId = designerId }) > 0;
        }

        public Designer_Shift AddShift(Designer_Shift shift)
        {
            var exists = _dbConnection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM DesignerShiftTB WHERE DesignerId = @DesignerId AND ShiftDate = @ShiftDate",
                new { shift.DesignerId, shift.ShiftDate });

            if (exists > 0) return null;

            var sql = @"
        INSERT INTO DesignerShiftTB (DesignerId, ShiftDate, IsDayOff)
        VALUES (@DesignerId, @ShiftDate, @IsDayOff);
        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var newId = _dbConnection.ExecuteScalar<int>(sql, shift);
            shift.ShiftId = newId;
            return shift;
        }

        public bool RemoveShift(int designerId, DateTime shiftDate)
        {
            var sql = "DELETE FROM DesignerShiftTB WHERE DesignerId = @DesignerId AND ShiftDate = @ShiftDate";
            return _dbConnection.Execute(sql, new { DesignerId = designerId, ShiftDate = shiftDate.Date }) > 0;
        }

        public List<Designer_Shift> GetShiftsForDay(DateTime date)
        {
            var sql = "SELECT * FROM DesignerShiftTB WHERE ShiftDate = @Date";
            return _dbConnection.Query<Designer_Shift>(sql, new { Date = date.Date }).ToList();
        }

        public List<Order> GetOrdersByMemberId(string uid)
        {
            var sql = "SELECT * FROM OrderTB WHERE Uid = @Uid";
            return _dbConnection.Query<Order>(sql, new { Uid = uid }).ToList();
        }

        public bool Reservation_IsFixedHoliday(int designerId, DateTime date)
        {
            string weekdayStr = ((int)date.DayOfWeek).ToString();
            var sql = "SELECT COUNT(*) FROM DesignerFixedHolidayTB WHERE DesignerId = @DesignerId AND WeekdayString = @Weekday";
            return _dbConnection.ExecuteScalar<int>(sql, new { DesignerId = designerId, Weekday = weekdayStr }) > 0;
        }

        public bool Reservation_IsDayOff(int designerId, DateTime date)
        {
            var sql = "SELECT COUNT(*) FROM DesignerShiftTB WHERE DesignerId = @DesignerId AND ShiftDate = @Date AND IsDayOff = 1";
            return _dbConnection.ExecuteScalar<int>(sql, new { DesignerId = designerId, Date = date.Date }) > 0;
        }

        public bool UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            var sql = "UPDATE OrderTB SET Status = @Status WHERE OrderId = @OrderId";
            return _dbConnection.Execute(sql, new { OrderId = orderId, Status = newStatus }) > 0;
        }

        public List<Order> GetOrdersForDay(int designerId, DateTime date)
        {
            var sql = "SELECT * FROM OrderTB WHERE DesignerId = @DesignerId AND CAST(ReservationDateTime AS DATE) = @Date";
            return _dbConnection.Query<Order>(sql, new { DesignerId = designerId, Date = date.Date }).ToList();
        }

        public Order? CreateOrder(int designerId, int productId, DateTime time)
        {
            if (!IsSlotAvailable(designerId, productId, time)) return null;

            var sql = @"INSERT INTO OrderTB (DesignerId, ProductId, ReservationDateTime, Status)
                VALUES (@DesignerId, @ProductId, @ReservationDateTime, @Status);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            int newId = _dbConnection.ExecuteScalar<int>(sql, new
            {
                DesignerId = designerId,
                ProductId = productId,
                ReservationDateTime = time,
                Status = OrderStatus.Confirmed
            });

            return GetOrderById(newId);
        }

        public bool IsSlotAvailable(int designerId, int productId, DateTime time)
        {
            var designer = GetDesignerById(designerId);
            if (designer == null) return false;

            var rule = designer.ScheduleRules.FirstOrDefault(r => r.ProductId == productId);
            if (rule == null) return false;

            if (Reservation_IsFixedHoliday(designerId, time.Date) || Reservation_IsDayOff(designerId, time.Date))
                return false;

            var orders = GetOrdersForDay(designerId, time);

            DateTime serviceStart = time;
            DateTime serviceEnd = time.AddMinutes(rule.DurationMinutes);

            var overlappingOrders = orders.Where(o =>
            {
                if (o.DesignerId != designerId || o.Status == OrderStatus.Cancelled)
                    return false;

                var bookedRule = designer.ScheduleRules.FirstOrDefault(r => r.ProductId == o.ProductId);
                if (bookedRule == null) return false;

                var bookedStart = o.ReservationDateTime;
                var bookedEnd = bookedStart.AddMinutes(bookedRule.DurationMinutes);

                return !(serviceEnd <= bookedStart || serviceStart >= bookedEnd);
            }).ToList();

            if (overlappingOrders.Any(o => o.ProductId != productId))
                return false;

            if (overlappingOrders.Any(o => o.ProductId == productId && o.ReservationDateTime != time))
                return false;

            int countAtT = orders.Count(o =>
                o.DesignerId == designerId &&
                o.ProductId == productId &&
                o.ReservationDateTime == time &&
                o.Status != OrderStatus.Cancelled);

            return countAtT < rule.MaxCustomers;
        }

        public List<Reservation_AvailableServiceSlot> GetAvailableServiceSlots(int designerId, DateTime date, int cooldownMinutes, int advanceMinutes)
        {
            List<Reservation_AvailableServiceSlot> result = new();
            var designer = GetDesignerById(designerId);
            if (designer == null) return result;

            if (Reservation_IsFixedHoliday(designerId, date) || Reservation_IsDayOff(designerId, date))
                return result;

            var orders = GetOrdersForDay(designerId, date);

            DateTime now = DateTime.Now;
            DateTime earliestAvailableTime = now.AddMinutes(advanceMinutes);
            DateTime dayStart = date.Date.AddHours(9);
            DateTime dayEnd = date.Date.AddHours(18);

            for (DateTime t = dayStart; t.AddMinutes(10) <= dayEnd; t = t.AddMinutes(10))
            {
                if (date.Date == DateTime.Today && t < earliestAvailableTime) continue;

                var availableProductIds = new List<int>();

                foreach (var rule in designer.ScheduleRules)
                {
                    DateTime serviceStart = t;
                    DateTime serviceEnd = t.AddMinutes(rule.DurationMinutes);
                    if (serviceEnd.AddMinutes(cooldownMinutes) > dayEnd) continue;

                    var overlappingOrders = orders.Where(o =>
                    {
                        if (o.DesignerId != designerId || o.Status == OrderStatus.Cancelled)
                            return false;

                        var bookedRule = designer.ScheduleRules.FirstOrDefault(r => r.ProductId == o.ProductId);
                        if (bookedRule == null) return false;

                        var bookedStart = o.ReservationDateTime;
                        var bookedEnd = bookedStart.AddMinutes(bookedRule.DurationMinutes);

                        return !(serviceEnd.AddMinutes(cooldownMinutes) <= bookedStart || serviceStart >= bookedEnd.AddMinutes(cooldownMinutes));
                    }).ToList();

                    if (overlappingOrders.Any(o => o.ProductId == rule.ProductId && o.ReservationDateTime != t))
                        continue;

                    if (overlappingOrders.Any(o => o.ProductId != rule.ProductId))
                        continue;

                    int countAtT = orders.Count(o =>
                        o.DesignerId == designerId &&
                        o.ProductId == rule.ProductId &&
                        o.ReservationDateTime == t &&
                        o.Status != OrderStatus.Cancelled);

                    if (countAtT < rule.MaxCustomers)
                    {
                        availableProductIds.Add(rule.ProductId);
                    }
                }

                if (availableProductIds.Count > 0)
                {
                    result.Add(new Reservation_AvailableServiceSlot
                    {
                        StartTime = t,
                        AvailableProductIds = availableProductIds
                    });
                }
            }

            return result;
        }
    }

}
