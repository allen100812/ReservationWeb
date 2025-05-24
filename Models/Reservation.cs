using System.ComponentModel.DataAnnotations;

namespace Web0524.Models
{
    // 設計師的排程與休假資訊
    public class Designer
    {
        [Required(ErrorMessage = "設計師編號必填")]
        [DataType(DataType.Text)]
        public int DesignerId { get; set; }

        [Required(ErrorMessage = "設計師姓名必填")]
        [DataType(DataType.Text)]
        public string Name { get; set; } 

        [DataType(DataType.Text)]
        public string Nickname { get; set; } = string.Empty;

        public List<Designer_ProductScheduleRule> ScheduleRules { get; set; } = new();
        public List<DateTime> FixedHolidays { get; set; } = new();

        public bool IsDeleted { get; set; } = false; // 假刪除欄位
    }

    public class Designer_ProductScheduleRule
    {
        [Required(ErrorMessage = "產品 ID 必填")]
        [DataType(DataType.Text)]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "耗時分鐘數必填")]
        [DataType(DataType.Duration)]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "最大可服務人數必填")]
        [DataType(DataType.Text)]
        public int MaxCustomers { get; set; }
    }

    public class Designer_Shift
    {
        [Required(ErrorMessage = "班表 ID 必填")]
        [DataType(DataType.Text)]
        public int ShiftId { get; set; }

        [Required(ErrorMessage = "設計師 ID 必填")]
        [DataType(DataType.Text)]
        public int DesignerId { get; set; }

        [Required(ErrorMessage = "排班日期必填")]
        [DataType(DataType.Date)]
        public DateTime ShiftDate { get; set; }

        [Required(ErrorMessage = "休假狀況必填")]
        [DataType(DataType.Text)]
        public bool IsDayOff { get; set; }
    }

    public class Reservation_AvailableServiceSlot
    {
        [Required(ErrorMessage = "起始時間必填")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }
        public List<int> AvailableProductIds { get; set; } = new();
    }

    public enum SlotStatus
    {
        Open,   // 開放預約
        Booked, // 已被預約
        Closed  // 不可預約
    }

}
