using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace Web0524.Models
{
    public interface IReservationService
    {
        // 基礎資料：設計師、排班、預約單
        List<Designer> Designers { get; set; }
        List<Designer_Shift> Shifts { get; set; }
        List<Order> Orders { get; set; }

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
        public List<Designer> Designers { get; set; } = new();
        public List<Designer_Shift> Shifts { get; set; } = new();
        public List<Order> Orders { get; set; } = new();


        // 根據訂單 ID 取得單筆訂單
        public Order? GetOrderById(int orderId)
        {
            return Orders.FirstOrDefault(o => o.OrderId == orderId);
        }

        // 取消預約（將狀態設為 Cancelled）
        public bool CancelOrder(int orderId)
        {
            var order = GetOrderById(orderId);
            if (order == null) return false;
            order.Status = OrderStatus.Cancelled;
            return true;
        }

        // 取得所有預約訂單清單
        public List<Order> GetAllOrders()
        {
            return Orders.ToList();
        }

        // 取得所有設計師清單
        public List<Designer> GetAllDesigners()
        {
            return Designers.ToList();
        }

        // 依設計師 ID 取得設計師資料
        public Designer? GetDesignerById(int designerId)
        {
            return Designers.FirstOrDefault(d => d.DesignerId == designerId);
        }

        // 新增設計師
        public bool AddDesigner(Designer designer)
        {
            if (GetDesignerById(designer.DesignerId) != null) return false;
            Designers.Add(designer);
            return true;
        }

        // 更新設計師資料
        public bool UpdateDesigner(Designer designer)
        {
            var existing = GetDesignerById(designer.DesignerId);
            if (existing == null) return false;
            Designers.Remove(existing);
            Designers.Add(designer);
            return true;
        }

        // 刪除設計師
        public bool DeleteDesigner(int designerId)
        {
            var existing = GetDesignerById(designerId);
            if (existing == null) return false;
            Designers.Remove(existing);
            return true;
        }

        // 新增排班資料
        public bool AddShift(Designer_Shift shift)
        {
            if (Shifts.Any(s => s.DesignerId == shift.DesignerId && s.ShiftDate.Date == shift.ShiftDate.Date)) return false;
            Shifts.Add(shift);
            return true;
        }

        // 移除指定日期的排班
        public bool RemoveShift(int designerId, DateTime shiftDate)
        {
            var existing = Shifts.FirstOrDefault(s => s.DesignerId == designerId && s.ShiftDate.Date == shiftDate.Date);
            if (existing == null) return false;
            Shifts.Remove(existing);
            return true;
        }

        // 取得指定日期的所有排班紀錄
        public List<Designer_Shift> GetShiftsForDay(DateTime date)
        {
            return Shifts.Where(s => s.ShiftDate.Date == date.Date).ToList();
        }

        // 依會員 ID 取得所有該會員預約紀錄
        public List<Order> GetOrdersByMemberId(string uid)
        {
            return Orders.Where(o => o.Uid == uid).ToList();
        }

        // 取得當日是否為固定休假日
        public bool Reservation_IsFixedHoliday(int designerId, DateTime date)
        {
            var designer = Designers.FirstOrDefault(d => d.DesignerId == designerId);
            return designer?.FixedHolidays.Any(h => h.Date == date.Date) ?? false;
        }

        // 取得當日是否為排休
        public bool Reservation_IsDayOff(int designerId, DateTime date)
        {
            return Shifts.Any(s => s.DesignerId == designerId && s.ShiftDate.Date == date.Date && s.IsDayOff);
        }

        // 檢查某時段是否可預約指定產品
        public bool IsSlotAvailable(int designerId, int productId, DateTime time)
        {
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
                if (t < earliestAvailableTime) continue;

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

        // 建立新預約訂單
        public Order? CreateOrder(int designerId, int ProductId, DateTime time)
        {
            if (!IsSlotAvailable(designerId, ProductId, time)) return null;

            var newOrder = new Order
            {
                OrderId = Orders.Count + 1,
                DesignerId = designerId,
                ProductId = ProductId,
                ReservationDateTime = time,
                Status = OrderStatus.Confirmed
            };

            Orders.Add(newOrder);
            return newOrder;
        }

        // 更新訂單狀態
        public bool UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            var order = Orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order == null) return false;

            order.Status = newStatus;
            return true;
        }

        // 查詢當日所有訂單
        public List<Order> GetOrdersForDay(int designerId, DateTime date)
        {
            return Orders.Where(o =>
                o.DesignerId == designerId &&
                o.ReservationDateTime.Date == date.Date).ToList();
        }
    }
}
