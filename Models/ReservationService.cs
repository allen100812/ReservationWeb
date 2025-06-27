using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.Linq;
using Dapper;
using Web0524.Models.Helper;
using Web0524.Models;
using System.Data.Common;
using Web0524.Models.LineMessage;
using MDP.DevKit.LineMessaging;
using System.Reflection.Metadata;
using Web0524.Models.SystemMessage;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Web0524.Models
{
    public interface IReservationService
    {

        // 取得指定設計師的所有固定休假
        List<string> GetFixedHolidays(int designerId);

        // 設定（覆蓋）固定休假清單
        bool SetFixedHolidays(int designerId, List<string> weekdayList);


        // 建議加入這個方法（比傳入 MinValue 更語意明確）
        List<Designer_Shift> GetShiftsByDesignerId(int designerId);

        // 判斷是否為固定休假日
        bool Reservation_IsFixedHoliday(int designerId, DateTime date);

        // 判斷是否為排休日
        bool Reservation_IsDayOff(int designerId, DateTime date);


        // 取得當日所有時段可預約的服務（依冷卻時間與提前限制）
        List<Reservation_AvailableSlotDetail> GetAvailableServiceSlots(int designerId, DateTime date, int cooldownMinutes, int advanceMinutes);

        // 檢查某設計師在指定時間是否可預約某服務
        bool IsSlotAvailable(int? designerId, int? ProductId, DateTime time);

        // 建立新預約單
        Order? CreateOrder(Order order);

        // 更新預約單狀態（如完成、取消）
        bool UpdateOrderStatus(int orderId, OrderStatus newStatus);

        // 取得指定設計師在某天的所有預約
        List<Order> GetOrdersForDay(int designerId, DateTime date);

        // 取得預約單（依 ID）
        Order? GetOrderById(int orderId);

        // 取消預約單
        bool CancelOrder(int orderId, bool customer_appeal);

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


        bool AddScheduleRule(int designerId, Designer_ProductScheduleRule rule);
        // 新增排休資料
        Designer_Shift AddShift(Designer_Shift shift);

        // 移除排休
        bool RemoveShift(int designerId, DateTime shiftDate);

        // 查詢當天所有排休資料
        List<Designer_Shift> GetShiftsForDay(DateTime date);

        // 依會員 ID 取得該會員所有預約紀錄
        List<Order> GetOrdersByMemberId(string uid);

        //自動將前一日訂單完成

        int AutoCompleteExpiredOrders();


        bool UpdateScheduleRule(int ruleId, int duration, int max);
        bool DeleteScheduleRule(int ruleId);

        bool RestoreDesigner(int designerId);


    }


    public class ReservationService : IReservationService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IGoogleCalendarHelper _calendarHelper;
        private readonly IUserService _userService;
        private readonly LineMessageService _lineService;
        private readonly IMessageService _messageService;

        private readonly IProductService _productService;

        public ReservationService(IDbConnection dbConnection,IGoogleCalendarHelper calendarHelper, IUserService userService, LineMessageService lineService, IMessageService messageService, IProductService productService)
        {
            _dbConnection = dbConnection;
            _calendarHelper = calendarHelper;
            _userService = userService;
            _lineService = lineService;
            _messageService = messageService;
            _productService = productService;
        }

        public List<string> GetFixedHolidays(int designerId)
        {
            var sql = "SELECT WeekdayString FROM DesignerFixedHolidayTB WHERE DesignerId = @designerId";
            return _dbConnection.Query<string>(sql, new { designerId }).ToList();
        }

        public bool SetFixedHolidays(int designerId, List<string> weekdayList)
        {
            try
            {
                if (_dbConnection.State != ConnectionState.Open)
                    _dbConnection.Open();

                using var tran = _dbConnection.BeginTransaction();

                // 刪除舊資料
                _dbConnection.Execute("DELETE FROM DesignerFixedHolidayTB WHERE DesignerId = @designerId",
                    new { designerId }, tran);

                // 插入新資料
                foreach (var wd in weekdayList)
                {
                    _dbConnection.Execute(
                        "INSERT INTO DesignerFixedHolidayTB (DesignerId, WeekdayString) VALUES (@designerId, @wd)",
                        new { designerId, wd }, tran);
                }

                tran.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ 儲存固定休假失敗：" + ex.Message);
                return false;
            }
            finally
            {
                if (_dbConnection.State == ConnectionState.Open)
                    _dbConnection.Close();
            }
        }

        public List<Designer_Shift> GetShiftsByDesignerId(int designerId)
        {
            // 撈所有指定設計師的排休（建議只包含未來日期）
            return _dbConnection.Query<Designer_Shift>(
                "SELECT * FROM DesignerShiftTB WHERE DesignerId = @id ORDER BY ShiftDate",
                new { id = designerId }).ToList();
        }

        public Order? GetOrderById(int orderId)
        {
            var sql = "SELECT * FROM OrderTB WHERE OrderId = @OrderId";
            return _dbConnection.QueryFirstOrDefault<Order>(sql, new { OrderId = orderId });
        }

        public bool CancelOrder(int orderId,bool customer_appeal)
        {
            var result = _dbConnection.Execute("UPDATE OrderTB SET Status = @Status WHERE OrderId = @OrderId",
                new { OrderId = orderId, Status = OrderStatus.Cancelled }) > 0;

            if (result)
            {
                
                var order = GetOrderById(orderId);
                Console.WriteLine("刪除行事曆:" + order?.GoogleEventId);
                if (!string.IsNullOrEmpty(order?.GoogleEventId))
                {
                    _calendarHelper.CancelEventAsync(order.GoogleEventId).Wait();

                }
                if (!string.IsNullOrEmpty(order?.Uid))
                {
                    _dbConnection.Execute("UPDATE UserTB SET CancelNum = CancelNum + 1 WHERE Id = @Id", new { Id = order.Uid });
                }

                if (My.CancelSandLineMsgSw && !string.IsNullOrEmpty(order?.Uid))
                {
                    var user_item = _userService.GetUserByLineUserId(order?.Uid);
                    var product = _productService.GetProductById(order.ProductId);
                    string p_name = string.IsNullOrWhiteSpace(product?.Name) ? "（未命名服務）" : product.Name;
                    var MSG = "";
                    if (customer_appeal)
                    {
                        MSG = MyMessageTemplates.FormatOrderCancelByClient(
order.OrderId.ToString("X6"),
p_name,
order.ReservationDateTime.ToString("yyyy-MM-dd HH:mm")
);
                    }
                    else
                    {
                        MSG = MyMessageTemplates.FormatOrderCancelByStore(
order.OrderId.ToString("X6"),
p_name,
order.ReservationDateTime.ToString("yyyy-MM-dd HH:mm")
);
                    }

                    var success = _lineService.SendSecureLineMessageAsync(user_item.LineUserId, MSG);
                    _messageService.SendMessage(
                        order?.Uid,
                        "預約訊息",
                        MSG,
                        MessageType.Store,
                        TimeSpan.FromDays(60)
                    );


                }


            }



            return result;
        }



        public List<Order> GetAllOrders()
        {
            return _dbConnection.Query<Order>("SELECT * FROM OrderTB").ToList();
        }

        public List<Designer> GetAllDesigners()
        {
            var designers = _dbConnection.Query<Designer>("SELECT * FROM DesignerTB").ToList();
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
            SELECT LAST_INSERT_ID();";

            var newId = _dbConnection.ExecuteScalar<int>(sql, designer);
            designer.DesignerId = newId;
            return designer;
        }

        public bool UpdateDesigner(Designer designer)
        {
            var sql = @"UPDATE DesignerTB 
                SET Name = @Name, Nickname = @Nickname 
                WHERE DesignerId = @DesignerId AND IsDeleted = 0";

            var parameters = new
            {
                designer.DesignerId,
                designer.Name,
                designer.Nickname
            };

            return _dbConnection.Execute(sql, parameters) > 0;
        }


        public bool AddScheduleRule(int designerId, Designer_ProductScheduleRule rule)
        {
            // 檢查是否已存在相同 DesignerId + ProductId 的規則
            var exists = _dbConnection.ExecuteScalar<int>(
                @"SELECT COUNT(*) FROM DesignerScheduleRuleTB 
          WHERE DesignerId = @DesignerId AND ProductId = @ProductId",
                new { DesignerId = designerId, rule.ProductId });

            if (exists > 0)
            {
                // 已存在規則，不允許新增
                return false;
            }

            var sql = @"INSERT INTO DesignerScheduleRuleTB
                (DesignerId, ProductId, DurationMinutes, MaxCustomers)
                VALUES (@DesignerId, @ProductId, @DurationMinutes, @MaxCustomers)";

            var param = new
            {
                DesignerId = designerId,
                rule.ProductId,
                rule.DurationMinutes,
                rule.MaxCustomers
            };

            return _dbConnection.Execute(sql, param) > 0;
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
            SELECT LAST_INSERT_ID();";

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
            var result = _dbConnection.Execute("UPDATE OrderTB SET Status = @Status WHERE OrderId = @OrderId",
                new { OrderId = orderId, Status = newStatus }) > 0;

            if (result && newStatus == OrderStatus.Cancelled)
            {
                var order = GetOrderById(orderId);
                if (!string.IsNullOrEmpty(order?.GoogleEventId))
                {
                    _calendarHelper.CancelEventAsync(order.GoogleEventId).Wait();


                    //不同類別不同處裡
                    var MSG = "";
                    if(newStatus == OrderStatus.Pending)
                    {
                        MSG = "";
                    }
                    else if(newStatus == OrderStatus.Confirmed)
                    {
                        var product = _productService.GetProductById(order.ProductId);
                        string p_name = string.IsNullOrWhiteSpace(product?.Name) ? "（未命名服務）" : product.Name;

                        MSG = MyMessageTemplates.FormatOrderCreated(
                            order.OrderId.ToString("X6"),
                            p_name,
                            order.ReservationDateTime.ToString("yyyy-MM-dd HH:mm")
                        );

                    }
                    else if (newStatus == OrderStatus.Cancelled)
                    {
                        var product = _productService.GetProductById(order.ProductId);
                        string p_name = string.IsNullOrWhiteSpace(product?.Name) ? "（未命名服務）" : product.Name;

                        MSG = MyMessageTemplates.FormatOrderCancelByStore(
                            order.OrderId.ToString("X6"),
                            p_name,
                            order.ReservationDateTime.ToString("yyyy-MM-dd HH:mm")
                        );

                    }
                    else if (newStatus == OrderStatus.Completed)
                    {
                        var product = _productService.GetProductById(order.ProductId);
                        string p_name = string.IsNullOrWhiteSpace(product?.Name) ? "（未命名服務）" : product.Name;

                        MSG = MyMessageTemplates.FormatOrderDone(
                            order.OrderId.ToString("X6"),
                            p_name,
                            order.ReservationDateTime.ToString("yyyy-MM-dd HH:mm")
                        );

                    }
                    _messageService.SendMessage(
                        order?.Uid,
                        "預約訊息",
                       MSG,
                        MessageType.Store,
                        TimeSpan.FromDays(60)
                    );

                }
            }

             return result;
        }

        public List<Order> GetOrdersForDay(int designerId, DateTime date)
        {
            var sql = "SELECT * FROM OrderTB WHERE DesignerId = @DesignerId AND CAST(ReservationDateTime AS DATE) = @Date";
            return _dbConnection.Query<Order>(sql, new { DesignerId = designerId, Date = date.Date }).ToList();
        }

        // ✅ 建立訂單方法更新
        public Order? CreateOrder(Order order)
        {
            if (!IsSlotAvailable(order.DesignerId, order.ProductId, order.ReservationDateTime))
                return null;

            var sql = @"
            INSERT INTO OrderTB 
            (Status, DesignerId, ProductId, Price, PaymentMethod, ReservationDateTime, Uid, Remark, Orderdate, UsedCouponId, DiscountAmount)
            VALUES 
            (@Status, @DesignerId, @ProductId, @Price, @PaymentMethod, @ReservationDateTime, @Uid, @Remark, @Orderdate, @UsedCouponId, @DiscountAmount);
            SELECT LAST_INSERT_ID();";

            order.OrderId = _dbConnection.ExecuteScalar<int>(sql, order);

            try
            {
                var designerName = GetDesignerName(order.DesignerId);
                var serviceName = GetServiceName(order.ProductId);
                var customerName = GetUserName(order.Uid);
                var paymentMethod = order.PaymentMethod.ToDisplayName();

                var eventId = _calendarHelper.AddEventAsync(order, designerName, serviceName, customerName, paymentMethod).Result;

                if (order.OrderId > 0 && !string.IsNullOrEmpty(eventId))
                {
                    order.GoogleEventId = eventId;
                    _dbConnection.Execute(
                        "UPDATE OrderTB SET GoogleEventId = @EventId WHERE OrderId = @OrderId",
                        new { EventId = eventId, OrderId = order.OrderId });

                    Console.WriteLine($"[Calendar] 寫入成功，OrderId = {order.OrderId}, EventId = {eventId}");
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Calendar] 新增事件失敗：{ex.Message}");
            }

            // 如有使用優惠券，更新 CouponDispatchRecord 為已使用
            if (order.UsedCouponId.HasValue)
            {
                _dbConnection.Execute("UPDATE CouponDispatchRecordTB SET IsDispatched = 1 WHERE RecordId = @Id", new { Id = order.UsedCouponId });
            }

            _dbConnection.Execute("UPDATE UserTB SET OrderNum = OrderNum + 1 WHERE Id = @Id", new { Id = order.Uid });

            if (My.CreateOrderSandLineMsgSw && !string.IsNullOrEmpty(order?.Uid))
            {
                var user_item = _userService.GetUserByLineUserId(order?.Uid);
                var product = _productService.GetProductById(order.ProductId);
                string p_name = string.IsNullOrWhiteSpace(product?.Name) ? "（未命名服務）" : product.Name;
                var MSG = MyMessageTemplates.FormatOrderCreated(
                order.OrderId.ToString("X6"),
                p_name,
                order.ReservationDateTime.ToString("yyyy-MM-dd HH:mm")
);
                var success = _lineService.SendSecureLineMessageAsync(user_item.LineUserId, MSG);
                _messageService.SendMessage(
                    order?.Uid,
                    "預約訊息",
                    MSG,
                    MessageType.Store,
                    TimeSpan.FromDays(60)
                );

            }


            return GetOrderById(order.OrderId);
        }



        public bool IsSlotAvailable(int? designerId, int? productId, DateTime time)
        {

            if (designerId == null)
            {
                // 如果未指定設計師，就檢查所有設計師是否任一人可預約
                var allDesigners = GetAllDesigners();
                foreach (var d in allDesigners)
                {
                    if (IsSlotAvailable(d.DesignerId, productId, time))  // 遞迴進實際判斷
                    {
                        return true;
                    }
                }
                return false;
            }

            var designer = GetDesignerById(designerId.Value);
            if (designer == null)
            {
                Console.WriteLine("❌ 找不到該設計師");
                return false;
            }

            if (Reservation_IsFixedHoliday(designer.DesignerId, time.Date))
            {
                Console.WriteLine("❌ 該日為固定假日");
                return false;
            }

            if (Reservation_IsDayOff(designer.DesignerId, time.Date))
            {
                Console.WriteLine("❌ 該設計師當日為休假日");
                return false;
            }

            var orders = GetOrdersForDay(designer.DesignerId, time);
            Console.WriteLine($"✅ 當天已有預約 {orders.Count} 筆");

            // 如果未指定服務項目，就檢查該設計師當時段是否有任何服務還可以預約
            if (productId == null)
            {
                foreach (var rule in designer.ScheduleRules)
                {
                    if (IsSlotAvailable(designer.DesignerId, rule.ProductId, time))
                    {
                        return true;
                    }
                }
                return false;
            }

            var ruleForProduct = designer.ScheduleRules.FirstOrDefault(r => r.ProductId == productId);
            if (ruleForProduct == null)
            {
                Console.WriteLine("❌ 該設計師沒有設定此服務項目的排程規則");
                return false;
            }

            DateTime serviceStart = time;
            DateTime serviceEnd = time.AddMinutes(ruleForProduct.DurationMinutes);

            var overlappingOrders = orders.Where(o =>
            {
                if (o.DesignerId != designer.DesignerId || o.Status == OrderStatus.Cancelled)
                    return false;

                var bookedRule = designer.ScheduleRules.FirstOrDefault(r => r.ProductId == o.ProductId);
                if (bookedRule == null) return false;

                var bookedStart = o.ReservationDateTime;
                var bookedEnd = bookedStart.AddMinutes(bookedRule.DurationMinutes);

                return !(serviceEnd <= bookedStart || serviceStart >= bookedEnd);
            }).ToList();

            Console.WriteLine($"🔄 發現有 {overlappingOrders.Count} 筆重疊預約");

            if (overlappingOrders.Any(o => o.ProductId != productId))
            {
                Console.WriteLine("❌ 時段已被其他服務項目預約");
                return false;
            }

            if (overlappingOrders.Any(o => o.ProductId == productId && o.ReservationDateTime != time))
            {
                Console.WriteLine("❌ 同一服務有不同時間重疊");
                return false;
            }

            int countAtT = orders.Count(o =>
                o.DesignerId == designer.DesignerId &&
                o.ProductId == productId &&
                o.ReservationDateTime == time &&
                o.Status != OrderStatus.Cancelled);

            Console.WriteLine($"⏱ 同時間點已有相同服務 {countAtT} 筆 / 最大上限 {ruleForProduct.MaxCustomers}");

            bool result = countAtT < ruleForProduct.MaxCustomers;
            Console.WriteLine(result ? "✅ 時段可預約" : "❌ 該時段已達最大上限");

            return result;
        }

        public List<Reservation_AvailableSlotDetail> GetAvailableServiceSlots(int designerId, DateTime date, int cooldownMinutes, int advanceMinutes)
        {
            List<Reservation_AvailableSlotDetail> result = new();
            var designer = GetDesignerById(designerId);
            if (designer == null)
            {
                Console.WriteLine("找不到設計師：" + designerId);
                return result;
            }

            if (Reservation_IsFixedHoliday(designerId, date))
            {
                Console.WriteLine("當日為固定休假日：" + date.ToShortDateString());
                return result;
            }

            if (Reservation_IsDayOff(designerId, date))
            {
                Console.WriteLine("當日為排休日：" + date.ToShortDateString());
                return result;
            }

            var orders = GetOrdersForDay(designerId, date);
            Console.WriteLine($"共取得 {orders.Count} 筆當日預約單");

            DateTime now = DateTime.Now;
            DateTime earliestAvailableTime = now.AddMinutes(advanceMinutes);
            DateTime dayStart = date.Date.AddHours(9);
            DateTime dayEnd = date.Date.AddHours(18);

            for (DateTime t = dayStart; t.AddMinutes(10) <= dayEnd; t = t.AddMinutes(10))
            {
                if (date.Date == DateTime.Today && t < earliestAvailableTime)
                {
                    Console.WriteLine($"跳過太早的時段：{t:HH:mm}");
                    continue;
                }

                foreach (var rule in designer.ScheduleRules)
                {
                    DateTime serviceStart = t;
                    DateTime serviceEnd = t.AddMinutes(rule.DurationMinutes);
                    if (serviceEnd.AddMinutes(cooldownMinutes) > dayEnd)
                    {
                        Console.WriteLine($"跳過超出工作結束時間的時段：{t:HH:mm} ~ {serviceEnd:HH:mm}");
                        continue;
                    }

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
                    {
                        Console.WriteLine($"時段 {t:HH:mm} 與其他 {rule.ProductId} 預約重疊");
                        continue;
                    }

                    if (overlappingOrders.Any(o => o.ProductId != rule.ProductId))
                    {
                        Console.WriteLine($"時段 {t:HH:mm} 與不同產品預約重疊");
                        continue;
                    }

                    int countAtT = orders.Count(o =>
                        o.DesignerId == designerId &&
                        o.ProductId == rule.ProductId &&
                        o.ReservationDateTime == t &&
                        o.Status != OrderStatus.Cancelled);

                    if (countAtT < rule.MaxCustomers)
                    {
                        Console.WriteLine($"可用時段 {t:HH:mm}，產品 {rule.ProductId}，目前預約數 {countAtT}");
                        result.Add(new Reservation_AvailableSlotDetail
                        {
                            Date = date.Date,
                            StartTime = t,
                            DesignerId = designerId,
                            ProductId = rule.ProductId
                        });
                    }
                    else
                    {
                        Console.WriteLine($"時段 {t:HH:mm} 已達最大人數上限（{countAtT}/{rule.MaxCustomers}）");
                    }
                }
            }

            Console.WriteLine($"共找到 {result.Count} 筆可預約時段");
            return result;
        }



        private string GetDesignerName(int designerId)
        {
            var sql = "SELECT Name FROM DesignerTB WHERE DesignerId = @DesignerId";
            return _dbConnection.ExecuteScalar<string>(sql, new { DesignerId = designerId }) ?? "未命名";
        }
        private string GetServiceName(int productId)
        {
            var sql = "SELECT Name FROM ProductTB WHERE ProductId = @ProductId";
            return _dbConnection.ExecuteScalar<string>(sql, new { ProductId = productId }) ?? "未命名服務";
        }

        private string GetUserName(string uid)
        {
            var sql = "SELECT Name FROM UserTB WHERE Id = @Uid";
            return _dbConnection.ExecuteScalar<string>(sql, new { Uid = uid }) ?? "未命名會員";
        }
        public int AutoCompleteExpiredOrders()
        {
            var now = DateTime.Now;

            // 先取得所有已過期的 Confirmed 訂單
            var expiredOrders = _dbConnection.Query<Order>(@"
        SELECT * FROM OrderTB
        WHERE Status = @ConfirmedStatus AND ReservationDateTime < @Now",
                new { ConfirmedStatus = OrderStatus.Confirmed, Now = now }).ToList();

            int updatedCount = 0;

            foreach (var order in expiredOrders)
            {
                // 更新資料庫中的狀態為 Completed
                var affected = _dbConnection.Execute(@"
            UPDATE OrderTB
            SET Status = @CompletedStatus
            WHERE OrderId = @OrderId", new
                {
                    CompletedStatus = OrderStatus.Completed,
                    OrderId = order.OrderId
                });

                if (affected > 0)
                {
                    updatedCount++;

                    // Google 行事曆同步更新
                    if (!string.IsNullOrEmpty(order.GoogleEventId))
                    {
                        try
                        {
                            var designerName = GetDesignerName(order.DesignerId);
                            var serviceName = GetServiceName(order.ProductId);
                            var customerName = GetUserName(order.Uid);
                            var paymentMethod = order.PaymentMethod.ToDisplayName();

                            _calendarHelper.UpdateEventAsync(order, order.GoogleEventId, designerName, serviceName, customerName, paymentMethod).Wait();
                            Console.WriteLine($"[Calendar] 已同步完成 OrderId = {order.OrderId} 的 Google 行事曆");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Calendar] 同步更新失敗 OrderId = {order.OrderId}，錯誤：{ex.Message}");
                        }
                    }
                }
            }

            return updatedCount;
        }

        public bool UpdateScheduleRule(int ruleId, int duration, int max)
        {
            var sql = @"UPDATE DesignerScheduleRuleTB 
                SET DurationMinutes = @Duration, MaxCustomers = @MaxCustomers 
                WHERE RuleId = @RuleId";
            return _dbConnection.Execute(sql, new
            {
                RuleId = ruleId,
                Duration = duration,
                MaxCustomers = max
            }) > 0;
        }


        public bool DeleteScheduleRule(int ruleId)
        {
            var sql = "DELETE FROM DesignerScheduleRuleTB WHERE RuleId = @RuleId";
            return _dbConnection.Execute(sql, new { RuleId = ruleId }) > 0;
        }


        public bool RestoreDesigner(int designerId)
        {
            var sql = "UPDATE DesignerTB SET IsDeleted = 0 WHERE DesignerId = @DesignerId";
            return _dbConnection.Execute(sql, new { DesignerId = designerId }) > 0;
        }

    }

}
