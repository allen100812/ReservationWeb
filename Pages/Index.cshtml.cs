using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.Design;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Web0524.Models;

namespace Web0524.Pages
{
     
    public class IndexModel : PageModel
    {

        private readonly IUserService _userService;
        private readonly ILogger<IndexModel> _logger;
        private readonly IPlaceService _placeService;
        private readonly LineMessageContext _lineMessageContext;
        private readonly INewService _newService;

        private readonly IReservationService _reservationService;
        public IndexModel(ILogger<IndexModel> logger, IUserService userService, IPlaceService placeService, LineMessageContext lineMessageContext , INewService newService, IReservationService reservationService)
        {
            _logger = logger;
            _userService = userService;
            _placeService = placeService;
            _lineMessageContext = lineMessageContext;
            _newService = newService;
            _reservationService = reservationService;
        }

        public string ReservationTestResult { get; set; } // 顯示在前端用

        public List<Place> places { get; set; }
        public string SId { get; set; }
        public string Myname { get; set; }


        public My basedata { get; set; }
        public List<NewList> newLists { get; set; }
        public async void OnGet()
        {
            if (User.FindFirst(ClaimTypes.Sid) != null)
            {
                SId = User.FindFirst(ClaimTypes.Sid).ToString();

                // 将用户数据传递给视图
            }
            places= _placeService.GetPlaceTB().ToList();
            newLists = _newService.GetNewTB().ToList();
            //LineMessageSender lineMessageSender = new LineMessageSender();
            //lineMessageSender.SendMessage("Udf96f6192a32d72329c908a69805aa9e", "官方帳號Push方法");
            //lineMessageSender.SendMessage_Notify("Notify傳送訊息方法");

            // 模擬資料
            int designerId = 1;
            DateTime today = DateTime.Today;

            // 模擬設計師與服務
            var designer = new Designer
            {
                DesignerId = designerId,
                Name = "設計師小美",
                ScheduleRules = new List<Designer_ProductScheduleRule>
        {
            new Designer_ProductScheduleRule { ProductId = 1, DurationMinutes = 60, MaxCustomers = 1 },
            new Designer_ProductScheduleRule { ProductId = 2, DurationMinutes = 40, MaxCustomers = 2 },
            new Designer_ProductScheduleRule { ProductId = 3, DurationMinutes = 20, MaxCustomers = 1 },
        },
                FixedHolidays = new List<DateTime>
        {
            today.AddDays(1),
            new DateTime(today.Year, today.Month, 15)
        }
            };

            _reservationService.Designers.Clear();
            _reservationService.Designers.Add(designer);

            // 模擬預約（含可重複時段）
            _reservationService.Orders.Clear();
            _reservationService.Orders.AddRange(new List<Order>
    {
        new Order
        {
            OrderId = 1,
            DesignerId = designerId,
            ProductId = 1,
            ReservationDateTime = today.AddHours(15),
            Status = OrderStatus.Confirmed
        },
        new Order
        {
            OrderId = 2,
            DesignerId = designerId,
            ProductId = 2,
            ReservationDateTime = today.AddHours(16).AddMinutes(20),
            Status = OrderStatus.Confirmed
        }
    });

            // 模擬排休（取消註解可測試）
            _reservationService.Shifts.Clear();
            //_reservationService.Shifts.Add(new Reservation_Shift
            //{
            //    DesignerId = designerId,
            //    ShiftDate = today,
            //    IsDayOff = true
            //});

            // 變數設定
            int cooldownMinutes = 10;     // 預約後冷卻
            int advanceMinutes = 120;     // 預約需提前兩小時

            // 1. 顯示當日是否休假
            var isFixedHoliday = _reservationService.Reservation_IsFixedHoliday(designerId, today);
            var isDayOff = _reservationService.Reservation_IsDayOff(designerId, today);

            ReservationTestResult = $"📆 測試日期：{today:yyyy-MM-dd}\n";
            ReservationTestResult += $"🔴 是否為固定休假：{(isFixedHoliday ? "是" : "否")}\n";
            ReservationTestResult += $"🔴 是否為排休：{(isDayOff ? "是" : "否")}\n\n";

            // 2. 顯示已預約
            ReservationTestResult += $"📌 已預約時段：\n";
            foreach (var o in _reservationService.Orders
                .Where(o => o.DesignerId == designerId && o.ReservationDateTime.Date == today && o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.ReservationDateTime))
            {
                var rule = designer.ScheduleRules.First(r => r.ProductId == o.ProductId);
                string timeRange = $"{o.ReservationDateTime:HH:mm} ~ {o.ReservationDateTime.AddMinutes(rule.DurationMinutes):HH:mm}";
                string serviceName = o.ProductId switch
                {
                    1 => "A",
                    2 => "B",
                    3 => "C",
                    _ => $"服務{o.ProductId}"
                };
                ReservationTestResult += $"- {timeRange} 服務：{serviceName}\n";
            }

            // 3. 顯示可預約服務
            ReservationTestResult += $"\n🟢 每 10 分鐘檢查可預約服務（考慮冷卻 {cooldownMinutes} 分鐘，提前 {advanceMinutes} 分鐘）：\n";
            var serviceSlots = _reservationService.GetAvailableServiceSlots(designerId, today, cooldownMinutes, advanceMinutes);
            foreach (var slot in serviceSlots)
            {
                string timeStr = slot.StartTime.ToString("HH:mm");
                string serviceStr = string.Join(", ", slot.AvailableProductIds.Select(id =>
                {
                    return id switch
                    {
                        1 => "A",
                        2 => "B",
                        3 => "C",
                        _ => $"服務{id}"
                    };
                }));
                ReservationTestResult += $"{timeStr} ➜ {serviceStr}\n";
            }
            Console.WriteLine(ReservationTestResult);
        }





    }
}