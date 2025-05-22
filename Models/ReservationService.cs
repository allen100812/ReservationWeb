using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;

namespace Web0524.Models
{
    public interface IReservationService
    {
        List<Reservation_Designer> Designers { get; set; }
        List<Reservation_Shift> Shifts { get; set; }
        List<Reservation_Order> Orders { get; set; }

        bool Reservation_IsFixedHoliday(int designerId, DateTime date);
        bool Reservation_IsDayOff(int designerId, DateTime date);

        List<Reservation_AvailableServiceSlot> GetAvailableServiceSlots(int designerId, DateTime date, int cooldownMinutes, int advanceMinutes);

        List<Reservation_AvailableSlot> GenerateAvailableSlots(int designerId, DateTime date);
        bool IsSlotAvailable(int designerId, int serviceId, DateTime time);
        Reservation_Order? CreateOrder(int designerId, int serviceId, DateTime time);
        bool UpdateOrderStatus(int orderId, OrderStatus newStatus);
        List<Reservation_Order> GetOrdersForDay(int designerId, DateTime date);


    }

    public class ReservationService : IReservationService
    {



        public List<Reservation_Designer> Designers { get; set; } = new();
        public List<Reservation_Shift> Shifts { get; set; } = new();
        public List<Reservation_Order> Orders { get; set; } = new();

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

        // 取得某日可預約時間（動態產生）
        public List<Reservation_AvailableSlot> GenerateAvailableSlots(int designerId, DateTime date)
        {
            List<Reservation_AvailableSlot> slots = new();

            if (Reservation_IsFixedHoliday(designerId, date) || Reservation_IsDayOff(designerId, date))
                return slots; // 空清單，整天關閉

            var designer = Designers.FirstOrDefault(d => d.DesignerId == designerId);
            if (designer == null) return slots;

            foreach (var rule in designer.ScheduleRules)
            {
                DateTime start = date.Date.AddHours(9);  // 預設從 09:00 開始
                DateTime end = date.Date.AddHours(18);   // 預設到 18:00

                while (start.AddMinutes(rule.DurationMinutes) <= end)
                {
                    int bookedCount = Orders.Count(o =>
                        o.DesignerId == designerId &&
                        o.ServiceId == rule.ServiceId &&
                        o.ReservationDateTime == start &&
                        o.Status != OrderStatus.Cancelled);

                    SlotStatus status = bookedCount >= rule.MaxCustomers
                        ? SlotStatus.Booked
                        : SlotStatus.Open;

                    slots.Add(new Reservation_AvailableSlot
                    {
                        DesignerId = designerId,
                        SlotTime = start,
                        Status = status
                    });

                    start = start.AddMinutes(rule.DurationMinutes);
                }
            }

            return slots;
        }

        // 檢查某時段是否可預約
        public bool IsSlotAvailable(int designerId, int serviceId, DateTime time)
        {
            var designer = Designers.FirstOrDefault(d => d.DesignerId == designerId);
            if (designer == null) return false;

            var rule = designer.ScheduleRules.FirstOrDefault(r => r.ServiceId == serviceId);
            if (rule == null) return false;

            if (Reservation_IsFixedHoliday(designerId, time.Date) || Reservation_IsDayOff(designerId, time.Date))
                return false;

            int bookedCount = Orders.Count(o =>
                o.DesignerId == designerId &&
                o.ServiceId == serviceId &&
                o.ReservationDateTime == time &&
                o.Status != OrderStatus.Cancelled);

            return bookedCount < rule.MaxCustomers;
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

                var availableServiceIds = new List<int>();

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

                        var bookedRule = designer.ScheduleRules.FirstOrDefault(r => r.ServiceId == o.ServiceId);
                        if (bookedRule == null) return false;

                        var bookedStart = o.ReservationDateTime;
                        var bookedEnd = bookedStart.AddMinutes(bookedRule.DurationMinutes);

                        // 時間重疊（含冷卻）
                        return !(serviceEnd.AddMinutes(cooldownMinutes) <= bookedStart || serviceStart >= bookedEnd.AddMinutes(cooldownMinutes));
                    }).ToList();

                    // ⛔ 若有預約，且該預約的時間點 ≠ t，則不能預約
                    if (overlappingOrders.Any(o => o.ServiceId == rule.ServiceId && o.ReservationDateTime != t))
                        continue;

                    // ⛔ 若有其他不同服務也重疊（即不同 ServiceId），不能預約
                    if (overlappingOrders.Any(o => o.ServiceId != rule.ServiceId))
                        continue;

                    // ✅ 若同服務在 t 預約人數未滿，可再預約
                    int countAtT = Orders.Count(o =>
                        o.DesignerId == designerId &&
                        o.ServiceId == rule.ServiceId &&
                        o.ReservationDateTime == t &&
                        o.Status != OrderStatus.Cancelled);

                    if (countAtT < rule.MaxCustomers)
                    {
                        availableServiceIds.Add(rule.ServiceId);
                    }
                }

                if (availableServiceIds.Count > 0)
                {
                    result.Add(new Reservation_AvailableServiceSlot
                    {
                        StartTime = t,
                        AvailableServiceIds = availableServiceIds
                    });
                }
            }

            return result;
        }

        // 建立新預約訂單
        public Reservation_Order? CreateOrder(int designerId, int serviceId, DateTime time)
        {
            if (!IsSlotAvailable(designerId, serviceId, time)) return null;

            var newOrder = new Reservation_Order
            {
                OrderId = Orders.Count + 1,
                DesignerId = designerId,
                ServiceId = serviceId,
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
        public List<Reservation_Order> GetOrdersForDay(int designerId, DateTime date)
        {
            return Orders.Where(o =>
                o.DesignerId == designerId &&
                o.ReservationDateTime.Date == date.Date).ToList();
        }
    }
}
