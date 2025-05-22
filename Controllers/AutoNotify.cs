using MDP.DevKit.LineMessaging;
using Microsoft.Extensions.Primitives;
using Quartz;
using Web0524.Models;

namespace Web0524.Controllers
{
    
    public class AutoNotify: IJob
    {
        private readonly IOrdersetService _OrdersetService;
        private readonly IUserService _userService;
        private readonly LineMessageContext _lineMessageContext;
        private readonly IPlaceService _placeService;
        private readonly IMyService _myService;

        DateTime localTime = DateTime.Now;
        DateTime taipeiTime;
        // 取得台北時區的TimeZoneInfo
        TimeZoneInfo taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        public AutoNotify(LineMessageContext lineMessageContext, IOrdersetService OrdersetService, IUserService userService, IPlaceService placeService, IMyService myService)
        {
            if (lineMessageContext == null) throw new ArgumentException($"{nameof(lineMessageContext)}=null");
            _lineMessageContext = lineMessageContext;
            _OrdersetService = OrdersetService;
            _userService = userService;
            _placeService = placeService;
            _myService = myService;
        }
        public My basedata { get; set; }
        public Task Execute(IJobExecutionContext context)
        {

            basedata = _myService.GetBaseData();
            if (basedata == null)
            {
                return Task.CompletedTask;
            }

            string msg="";
            // 將本地時間轉換為台北時區的時間s
            taipeiTime = TimeZoneInfo.ConvertTime(localTime, TimeZoneInfo.Local, taipeiTimeZone);
            DayOfWeek dayOfWeek = taipeiTime.DayOfWeek;
            //讀取order
            List<Orderset> Ordersets_1 = _OrdersetService.GetOrdersetInDate(taipeiTime.AddDays(1), taipeiTime.AddDays(8), "0").ToList();
            List<Orderset> Ordersets_2 = _OrdersetService.GetOrdersetInDate(taipeiTime.AddDays(1), taipeiTime.AddDays(3), "0").ToList();
            List<Orderset> Ordersets_3 = _OrdersetService.GetOrdersetByDate(taipeiTime, "0").ToList();
            List<Models.User> Admins = _userService.GetUserByType(2).ToList();
            List<Place> Places = _placeService.GetPlaceTB().ToList();
            LineMessageSender lineMessageSender = new LineMessageSender();
            System.Diagnostics.Debug.Print("開始讀取...");
            //0點系統通知1(下周報)
            System.Diagnostics.Debug.Print("是否週報?");
            if (dayOfWeek == 0)
            {
                System.Diagnostics.Debug.Print("開始週報");
                if (Ordersets_1 != null)
                {
                    int Event0_Num = 0;
                    int Event1_Num = 0;
                    string Event1List = "";
                    System.Diagnostics.Debug.Print("週報時間" + taipeiTime.AddDays(1).ToShortDateString() + "-" + taipeiTime.AddDays(8).ToShortDateString());
                    System.Diagnostics.Debug.Print("共" + Ordersets_1.Count.ToString() + "筆資料，開始播報");
                    foreach (Orderset orderset in Ordersets_1)
                    {
                        if (orderset.Event == "0")
                        {
                            Event0_Num++;
                        }
                        else if (orderset.Event == "1")
                        {
                            Event1_Num++;
                            var Place = Places.FirstOrDefault(place => place.Placeid == orderset.Placeid);
                            string OrderDate = orderset.Date != null ? orderset.Date.Value.ToString("yyyy-MM-dd") : "N/A";
                            string OrderTime;
                            if (orderset.Time != null)
                            {
                                OrderTime = $"{orderset.Time.Value.Hours:D2}:{orderset.Time.Value.Minutes:D2}";
                            }
                            else
                            {
                                OrderTime = "N/A";
                            }
                            Event1List = Event1List + "S" + orderset.Sid.ToString().PadLeft(6, '0') + "-" + Place.Placetitle + "\r\n └" + orderset.Pname + "\r\n └⏰ " + OrderDate + " " + OrderTime + "\r\n └🙎 " + orderset.Uname + "\r\n └ID:" + orderset.Uid + "\r\n\r\n";
                        }
                    }
                    if (Admins != null)
                    {
                        int OrderCount = Event0_Num + Event1_Num;
                        System.Diagnostics.Debug.Print("開始週報發布共" + Admins.Count.ToString() +"管理員");
                        foreach (Models.User Admin in Admins)
                        {
                            if (Admin.LineUserId != null && Admin.LineUserId != "")
                            {
                                msg = My.Msg_OrderWeekReport;
                                msg = msg.Replace("{DateStart}", taipeiTime.AddDays(1).ToShortDateString());
                                msg = msg.Replace("{DateEnd}", taipeiTime.AddDays(7).ToShortDateString());
                                msg = msg.Replace("{OrderCount}", OrderCount.ToString());
                                msg = msg.Replace("{Event0_Num}", Event0_Num.ToString());
                                msg = msg.Replace("{Event1_Num}", Event1_Num.ToString());
                                msg = msg.Replace("{Event1List}", Event1List);
                                msg = msg.Replace("{Url}", basedata.WebURL + "OrdersetCalendar_m");
                                lineMessageSender.SendMessage_Notify(Admin.LineUserId, msg);
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("無管理員資料，無發送訊息。");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.Print("非星期日不執行週報。");
            }



            //0點系統通知2(3日後預約)
            System.Diagnostics.Debug.Print("開始播報3日內預約單");
            System.Diagnostics.Debug.Print("播報時間:" + taipeiTime.AddDays(1).ToShortDateString()  + "-"+ taipeiTime.AddDays(3).ToShortDateString());
            if (Ordersets_2 != null)
            {
                int Event0_Num = 0;
                int Event1_Num = 0;
                string Event1List = "";
                string OrderData = "";
                System.Diagnostics.Debug.Print("播報資料共 "+ Ordersets_2.Count().ToString() + "筆");
                foreach (Orderset orderset in Ordersets_2)
                {
                    if (orderset.Event == "0")
                    {
                        Event0_Num++;
                    }
                    else if (orderset.Event == "1")
                    {
                        Event1_Num++;
                        var Place = Places.FirstOrDefault(place => place.Placeid == orderset.Placeid);
                        string OrderDate = orderset.Date != null ? orderset.Date.Value.ToString("yyyy-MM-dd") : "N/A";
                        string OrderTime;
                        if (orderset.Time != null)
                        {
                            OrderTime = $"{orderset.Time.Value.Hours:D2}:{orderset.Time.Value.Minutes:D2}";
                        }
                        else
                        {
                            OrderTime = "N/A";
                        }
                        Event1List = Event1List + "S" + orderset.Sid.ToString().PadLeft(6, '0') + "-" + Place.Placetitle + "\r\n └" + orderset.Pname + "\r\n └⏰ " + OrderDate + " " + OrderTime + "\r\n └🙎 " + orderset.Uname + "\r\n └ID:" + orderset.Uid + "\r\n\r\n";
                        OrderData = Place.Placetitle + "-訂單編號：S" + orderset.Sid.ToString().PadLeft(6, '0') + "服務項目:" + orderset.Pname + "預約時間:" + OrderDate + " " + OrderTime;
                        //以下為會員自動通知代碼
                        //Models.User user = _userService.GetUserById(orderset.Uid);
                        //if (user != null)
                        //{
                        //    System.Diagnostics.Debug.Print("正在嘗試播報至uid=" + user.Id);
                        //    msg = My.Msg_OrderCome_client;
                        //    msg = msg.Replace("{OrderData}", OrderData);
                        //    msg = msg.Replace("{Url}", basedata.WebURL + "Order");
                        //    if (user.LineUserId != null)
                        //    {
                        //        LineUserAsync(msg, user.LineUserId);
                        //        System.Diagnostics.Debug.Print("成功播報。");
                        //    }
                        //    else
                        //    {
                        //        System.Diagnostics.Debug.Print("失敗!該會員並無綁定line");
                        //    }
                                
                        //}
                        //else
                        //{
                        //    System.Diagnostics.Debug.Print("訂單" + orderset.Sid + "找不到對應會員無法播報");
                        //}
                    }

                }
                if (Event1List != "" || Event0_Num !=0)
                {
                    if(Admins != null)
                    {
                        System.Diagnostics.Debug.Print("開始發布共" + Admins.Count.ToString() + "管理員");
                        foreach (Models.User Admin in Admins)
                        {
                            int OrderCount = Event0_Num + Event1_Num;
                            if (Admin.LineUserId != null && Admin.LineUserId != "")
                            {
                                msg = My.Msg_OrderCome_op;
                                msg = msg.Replace("{OrderCount}", OrderCount.ToString());
                                msg = msg.Replace("{Event0_Num}", Event0_Num.ToString());
                                msg = msg.Replace("{Event1_Num}", Event1_Num.ToString());
                                msg = msg.Replace("{Event1List}", Event1List);
                                msg = msg.Replace("{Url}", basedata.WebURL + "OrdersetCalendar_m");
                                lineMessageSender.SendMessage_Notify(Admin.LineUserId, msg);
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("無管理員資料，無發送訊息。");
                    }

                }

            }
            else
            {
                System.Diagnostics.Debug.Print("3日後無資料，終止播報。");
            }
            //0點系統通知3(今日預約)
            System.Diagnostics.Debug.Print("開始播報今日預約");
            System.Diagnostics.Debug.Print("播報時間:" + taipeiTime.ToShortDateString());
            if (Ordersets_3 != null)
            {
                int Event1_Num = 0;
                string Event1List = "";
                string OrderData = "";
                System.Diagnostics.Debug.Print("播報資料共 " + Ordersets_3.Count().ToString() + "筆");
                foreach (Orderset orderset in Ordersets_3)
                {
                    if (orderset.Event == "1")
                    {
                        Event1_Num++;
                        var Place = Places.FirstOrDefault(place => place.Placeid == orderset.Placeid);
                        string OrderDate = orderset.Date != null ? orderset.Date.Value.ToString("MM/dd") : "N/A";
                        string OrderTime;
                        if (orderset.Time != null)
                        {
                            OrderTime = $"{orderset.Time.Value.Hours:D2}:{orderset.Time.Value.Minutes:D2}";
                        }
                        else
                        {
                            OrderTime = "N/A";
                        }
                        Event1List = Event1List + "S" + orderset.Sid.ToString().PadLeft(6, '0') + "-" + Place.Placetitle + "\r\n └" + orderset.Pname + "\r\n └⏰ " + OrderDate + " " + OrderTime + "\r\n └🙎 " + orderset.Uname + "\r\n └ID:" + orderset.Uid + "\r\n\r\n";
                        OrderData =  "序號：S" + orderset.Sid.ToString().PadLeft(6, '0') + "\r\n💖 " + orderset.Pname + "\r\n🏠 " + Place.Placetitle + "-" + Place.Placeaddress + "\r\n⏰ " + OrderDate + " " + OrderTime + "\r\n";
                        //以下為會員自動通知代碼
                        //Models.User user = _userService.GetUserById(orderset.Uid);
                        
                        //if (user != null)
                        //{
                        //    System.Diagnostics.Debug.Print("正在嘗試播報至uid=" + user.Id);
                        //    msg = My.Msg_OrderToday_client;
                        //    msg = msg.Replace("{Date}", taipeiTime.ToShortDateString());
                        //    msg = msg.Replace("{OrderData}", OrderData);
                        //    msg = msg.Replace("{Url}", basedata.WebURL + "Order");
                        //    if (user.LineUserId != null)
                        //    {
                        //        LineUserAsync(msg, user.LineUserId);
                        //        System.Diagnostics.Debug.Print("成功播報。");
                        //    }
                        //    else
                        //    {
                        //        System.Diagnostics.Debug.Print("失敗!該會員並無綁定line");
                        //    }

                        //}
                        //else
                        //{
                        //    System.Diagnostics.Debug.Print("訂單" + orderset.Sid + "找不到對應會員無法播報");
                        //}
                    }
     

                }
                if (Event1List != "")
                {
                    if( Admins != null)
                    {
                        System.Diagnostics.Debug.Print("開始發布共" + Admins.Count.ToString() + "管理員");
                        foreach (Models.User Admin in Admins)
                        {
                            if (Admin.LineUserId != null && Admin.LineUserId != "")
                            {
                                msg = My.Msg_OrderToday_op;
                                msg = msg.Replace("{Date}", taipeiTime.ToShortDateString());
                                msg = msg.Replace("{Event1_Num}", Event1_Num.ToString());
                                msg = msg.Replace("{Event1List}", Event1List);
                                msg = msg.Replace("{Url}", basedata.WebURL + "OrdersetCalendar_m");
                                lineMessageSender.SendMessage_Notify(Admin.LineUserId, msg);
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("無管理員資料，無發送訊息。");
                    }

                }

            }
            else
            {
                System.Diagnostics.Debug.Print("今日無資料，終止播報。");
            }

            //LineUserAsync($"Server! {DateTime.Now:yyyy-MM-dd HH:mm:ss}", "Udf96f6192a32d72329c908a69805aa9e");
            //LineUserAsync($"Tp! {taipeiTime:yyyy-MM-dd HH:mm:ss}", "Udf96f6192a32d72329c908a69805aa9e");
            return Task.CompletedTask;
        }
    }
}
