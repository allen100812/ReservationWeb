namespace Web0524.Models
{
    public class Reservation
    {

        // 設計師的排程與休假資訊
        public class Designer
        {
            public int DesignerId { get; set; }
            public string Name { get; set; }
            public string Nickname { get; set; }

            // 該設計師可提供服務的排程{服務項目, 耗時, 可服務人數}
            public List<Designer_ProductScheduleRule> ScheduleRules { get; set; } = new();

            // 固定休假日，例如每週日、或指定日期
            public List<DateTime> FixedHolidays { get; set; } = new();
        }
        public class Designer_ProductScheduleRule
        {
            public int ProductId { get; set; }
            public int DurationMinutes { get; set; } //耗時
            public int MaxCustomers { get; set; } //最大可同時服務人數
        }

        // 排班物件：設計師排休
        public class Designer_Shift
        {
            public int ShiftId { get; set; }
            public int DesignerId { get; set; } //設計師編號
            public DateTime ShiftDate { get; set; }  // 日期
            public bool IsDayOff { get; set; }       // 是否休假
        }

        public class Reservation_AvailableServiceSlot
        {
            public DateTime StartTime { get; set; }
            public List<int> AvailableProductIds { get; set; } = new(); // 可提供的服務 ID
        }

        public enum SlotStatus
        {
            Open,       // 開放預約
            Booked,     // 已被預約
            Closed      // 不可預約（排休或滿額）
        }
    }



}
