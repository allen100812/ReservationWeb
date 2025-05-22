namespace Web0524.Models
{
    public class Reservation
    {

    }

    // 設計師的排程與休假資訊
    public class Reservation_Designer
    {
        public int DesignerId { get; set; }
        public string Name { get; set; }

        // 該設計師可提供服務的排程{服務項目, 耗時, 可服務人數}
        public List<Reservation_ScheduleRule> ScheduleRules { get; set; } = new();

        // 固定休假日，例如每週日、或指定日期
        public List<DateTime> FixedHolidays { get; set; } = new();
    }



    public class Reservation_ScheduleRule
    {
        public int ServiceId { get; set; }
        public int DurationMinutes { get; set; } //耗時
        public int MaxCustomers { get; set; } //最大可同時服務人數
    }

    // 排班物件：設計師排休
    public class Reservation_Shift
    {
        public int ShiftId { get; set; }
        public int DesignerId { get; set; } //設計師編號
        public DateTime ShiftDate { get; set; }  // 日期
        public bool IsDayOff { get; set; }       // 是否休假
    }

    // 訂單物件
    public class Reservation_Order
    {
        public int OrderId { get; set; }
        public int DesignerId { get; set; } //設計師名稱
        public int ServiceId { get; set; } //服務項目
        public DateTime ReservationDateTime { get; set; } //預約時間
        public OrderStatus Status { get; set; } = OrderStatus.Pending; //訂單狀態
    }

    public enum OrderStatus
    {
        Pending,    // 下單但尚未確認
        Confirmed,  // 成立
        Cancelled,  // 取消
        Completed   // 完成
    }

    public class Reservation_AvailableServiceSlot
    {
        public DateTime StartTime { get; set; }
        public List<int> AvailableServiceIds { get; set; } = new(); // 可提供的服務 ID
    }


    // 可服務時間：每日動態產生
    public class Reservation_AvailableSlot
    {
        public int DesignerId { get; set; }
        public DateTime SlotTime { get; set; }
        public SlotStatus Status { get; set; } = SlotStatus.Open;
    }

    public enum SlotStatus
    {
        Open,       // 開放預約
        Booked,     // 已被預約
        Closed      // 不可預約（排休或滿額）
    }


}
