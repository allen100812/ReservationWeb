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
        bool AddDesigner(Designer designer);

        // 更新設計師
        bool UpdateDesigner(Designer designer);

        // 刪除設計師
        bool DeleteDesigner(int designerId);

        // 新增排休資料
        bool AddShift(Designer_Shift shift);

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

                d.FixedHolidays = _dbConnection.Query<DateTime>(
                    "SELECT HolidayDate FROM DesignerHolidayTB WHERE DesignerId = @DesignerId",
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

                designer.FixedHolidays = _dbConnection.Query<DateTime>(
                    "SELECT HolidayDate FROM DesignerHolidayTB WHERE DesignerId = @DesignerId",
                    new { DesignerId = designerId }).ToList();
            }
            return designer;
        }

        public bool AddDesigner(Designer designer)
        {
            var sql = "INSERT INTO DesignerTB (Name, Nickname, IsDeleted) VALUES (@Name, @Nickname, 0)";
            return _dbConnection.Execute(sql, designer) > 0;
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

        public bool AddShift(Designer_Shift shift)
        {
            var exists = _dbConnection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM DesignerShiftTB WHERE DesignerId = @DesignerId AND ShiftDate = @ShiftDate",
                new { shift.DesignerId, shift.ShiftDate });

            if (exists > 0) return false;

            var sql = "INSERT INTO DesignerShiftTB (DesignerId, ShiftDate, IsDayOff) VALUES (@DesignerId, @ShiftDate, @IsDayOff)";
            return _dbConnection.Execute(sql, shift) > 0;
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
            var sql = "SELECT COUNT(*) FROM DesignerHolidayTB WHERE DesignerId = @DesignerId AND HolidayDate = @Date";
            return _dbConnection.ExecuteScalar<int>(sql, new { DesignerId = designerId, Date = date.Date }) > 0;
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



        // 檢查某時段是否可預約指定產品
        public bool IsSlotAvailable(int designerId, int productId, DateTime time)
        {
            List<Designer> Designers = GetAllDesigners().ToList();
            List<Designer_Shift> Shifts = GetShiftsForDay(time).ToList();
            List<Order> Orders = GetOrdersForDay(designerId, time).ToList();

            var designer = Designers.FirstOrDefault(d => d.DesignerId == designerId);
            if (designer == null) return false;

            var rule = designer.ScheduleRules.FirstOrDefault(r => r.ProductId == productId);
            if (rule == null) return false;

            // 若為固定休假或排休日，不可預約
            if (Reservation_IsFixedHoliday(designerId, time.Date) || Reservation_IsDayOff(designerId, time.Date))
                return false;

            DateTime serviceStart = time;
            DateTime serviceEnd = time.AddMinutes(rule.DurationMinutes);

            // 查詢與該時間段重疊的所有預約（不含取消）
            var overlappingOrders = Orders.Where(o =>
            {
                if (o.DesignerId != designerId || o.Status == OrderStatus.Cancelled)
                    return false;

                var bookedRule = designer.ScheduleRules.FirstOrDefault(r => r.ProductId == o.ProductId);
                if (bookedRule == null) return false;

                var bookedStart = o.ReservationDateTime;
                var bookedEnd = bookedStart.AddMinutes(bookedRule.DurationMinutes);

                // 時間區間有交集即視為重疊
                return !(serviceEnd <= bookedStart || serviceStart >= bookedEnd);
            }).ToList();

            // ⛔ 有其他不同服務重疊時段，不能預約
            if (overlappingOrders.Any(o => o.ProductId != productId))
                return false;

            // ⛔ 有相同服務但預約時間 ≠ 該時間點，不可預約
            if (overlappingOrders.Any(o => o.ProductId == productId && o.ReservationDateTime != time))
                return false;

            // ✅ 該時間點相同服務預約數量未超過限制時可預約
            int countAtT = Orders.Count(o =>
                o.DesignerId == designerId &&
                o.ProductId == productId &&
                o.ReservationDateTime == time &&
                o.Status != OrderStatus.Cancelled);

            return countAtT < rule.MaxCustomers;
        }


        public List<Reservation_AvailableServiceSlot> GetAvailableServiceSlots(int designerId, DateTime date, int cooldownMinutes, int advanceMinutes)
        {
            List<Designer> Designers =GetAllDesigners().ToList();
            List<Designer_Shift> Shifts = GetShiftsForDay(date).ToList();
            List<Order> Orders = GetOrdersForDay(designerId,date).ToList();


            List<Reservation_AvailableServiceSlot> result = new();
            var designer = Designers.FirstOrDefault(d => d.DesignerId == designerId);
            if (designer == null) return result;

            if (Reservation_IsFixedHoliday(designerId, date) || Reservation_IsDayOff(designerId, date))
                return result;

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
                    if (serviceEnd.AddMinutes(cooldownMinutes) > dayEnd)
                        continue;

                    // 查詢與此服務重疊的所有預約
                    var overlappingOrders = Orders.Where(o =>
                    {
                        if (o.DesignerId != designerId || o.Status == OrderStatus.Cancelled)
                            return false;

                        var bookedRule = designer.ScheduleRules.FirstOrDefault(r => r.ProductId == o.ProductId);
                        if (bookedRule == null) return false;

                        var bookedStart = o.ReservationDateTime;
                        var bookedEnd = bookedStart.AddMinutes(bookedRule.DurationMinutes);

                        // 時間重疊（含冷卻）
                        return !(serviceEnd.AddMinutes(cooldownMinutes) <= bookedStart || serviceStart >= bookedEnd.AddMinutes(cooldownMinutes));
                    }).ToList();

                    // ⛔ 若有預約，且該預約的時間點 ≠ t，則不能預約
                    if (overlappingOrders.Any(o => o.ProductId == rule.ProductId && o.ReservationDateTime != t))
                        continue;

                    // ⛔ 若有其他不同服務也重疊（即不同 ProductId），不能預約
                    if (overlappingOrders.Any(o => o.ProductId != rule.ProductId))
                        continue;

                    // ✅ 若同服務在 t 預約人數未滿，可再預約
                    int countAtT = Orders.Count(o =>
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
