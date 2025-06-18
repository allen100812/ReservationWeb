using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Web0524.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Web0524.Models.Helper
{
    public interface IGoogleCalendarHelper
    {
        Task<string> AddEventAsync(Order order, string designerName, string serviceName, string customerName, string paymentMethod);
        Task<bool> UpdateEventAsync(Order order, string googleEventId, string designerName, string serviceName, string customerName, string paymentMethod);

        Task<bool> CancelEventAsync(string googleEventId);
    }

    public class GoogleCalendarHelper : IGoogleCalendarHelper
    {
        private readonly CalendarService _calendarService;
        private readonly string _calendarId;

        public GoogleCalendarHelper(string credentialsPath, string calendarId)
        {
            var credential = GoogleCredential.FromFile(credentialsPath)
                .CreateScoped(CalendarService.Scope.Calendar);

            _calendarService = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Reservation System"
            });

            _calendarId = calendarId;
        }
        private string GetColorIdByStatus(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "5",     // 黃色
                OrderStatus.Confirmed => "10",  // 藍色
                OrderStatus.Completed => "2",   // 綠色
                OrderStatus.Cancelled => "11",  // 紅色
                _ => "10"
            };
        }

        public async Task<string> AddEventAsync(Order order, string designerName, string serviceName, string customerName, string paymentMethod)
        {
            var summary = $"[{order.Status}] 預約 - {designerName}（{order.Uid}）";
            var description = $"🗓 日期: {order.ReservationDateTime:yyyy/MM/dd HH:mm}\n" +
                              $"💇 設計師: {designerName}\n" +
                              $"🛎️ 服務: {serviceName}\n" +
                              $"👤 客戶: {customerName}\n" +
                              $"🧾 付款方式: {paymentMethod}\n" +
                              $"📝 備註: {order.Remark}";

            var @event = new Event
            {
                Summary = summary,
                Description = description,
                Start = new EventDateTime
                {
                    DateTime = order.ReservationDateTime,
                    TimeZone = "Asia/Taipei"
                },
                End = new EventDateTime
                {
                    DateTime = order.ReservationDateTime.AddMinutes(30),
                    TimeZone = "Asia/Taipei"
                },
                ColorId = GetColorIdByStatus(order.Status)
            };

            var createdEvent = await _calendarService.Events.Insert(@event, _calendarId).ExecuteAsync();
            return createdEvent.Id;
        }


        public async Task<bool> UpdateEventAsync(Order order, string googleEventId, string designerName, string serviceName, string customerName, string paymentMethod)
        {
            try
            {
                var existingEvent = await _calendarService.Events.Get(_calendarId, googleEventId).ExecuteAsync();

                existingEvent.Summary = $"[{order.Status}] 預約 - {designerName}（{order.Uid}）";
                existingEvent.Description =
                    $"🗓 日期: {order.ReservationDateTime:yyyy/MM/dd HH:mm}\n" +
                    $"💇 設計師: {designerName}\n" +
                    $"🛎️ 服務: {serviceName}\n" +
                    $"👤 客戶: {customerName}\n" +
                    $"🧾 付款方式: {paymentMethod}\n" +
                    $"📝 備註: {order.Remark}";
                existingEvent.Start.DateTime = order.ReservationDateTime;
                existingEvent.End.DateTime = order.ReservationDateTime.AddMinutes(30);
                existingEvent.ColorId = GetColorIdByStatus(order.Status);

                await _calendarService.Events.Update(existingEvent, _calendarId, googleEventId).ExecuteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }



        public async Task<bool> CancelEventAsync(string googleEventId)
        {
            try
            {
                var existingEvent = await _calendarService.Events.Get(_calendarId, googleEventId).ExecuteAsync();

                existingEvent.Summary = $"❌已取消 - {existingEvent.Summary}";
                existingEvent.ColorId = "11"; // 紅色
                existingEvent.Description += "\n\n此預約已取消";

                await _calendarService.Events.Update(existingEvent, _calendarId, googleEventId).ExecuteAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
