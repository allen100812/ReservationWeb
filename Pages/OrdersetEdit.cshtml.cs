using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class OrdersetEditModel : PageModel
    {
        private readonly IOrdersetService _OrdersetService;
        private readonly IPlaceService _placeService;
        private readonly IUserService _userService;
        private readonly LineMessageContext _lineMessageContext;
        private readonly IMyService _myService;
        public OrdersetEditModel(IOrdersetService OrdersetService, IPlaceService placeService, IUserService userService, LineMessageContext lineMessageContext, IMyService myService)
        {
            #region Contracts

            if (lineMessageContext == null) throw new ArgumentException($"{nameof(lineMessageContext)}=null");

            #endregion
            _OrdersetService = OrdersetService;
            _placeService = placeService;
            _userService = userService;
            _lineMessageContext = lineMessageContext;
            _myService = myService;
        }


        [BindProperty]
        public Orderset Orderset { get; set; }
        public List<Orderset> Orderset_CheckDate { get; set; }
        public Place place { get; set; }
        public List<(string uid, double point)> UserPointTB { get; set; }
        public List<UserPointStr> userPointStr { get; set; }
        public Models.User Orderset_user { get; set; }
        public My basedata { get; set; }

        public IActionResult OnGet(string? sid)
        {
            if (sid == null)
            {
                return NotFound();
            }

            Orderset = _OrdersetService.GetOrdersetById(sid);
            
            if (Orderset == null)
            {
                return NotFound();
            }
            place = _placeService.GetPlaceById(Orderset.Placeid.ToString());
            Orderset_CheckDate = _OrdersetService.GetOrdersetByDate((DateTime)Orderset.Date, sid).ToList();
            Orderset_user = _userService.GetUserById(Orderset.Uid);
            UserPointTB = _userService.GetUserPointTB().ToList();
            userPointStr=_userService.GetUserPointStr();

            return Page();
        }

        public IActionResult OnPost(string? sid, string? Event)
        {

            if (sid == null || Event == null )
            {
                return NotFound();
            }


            basedata = _myService.GetBaseData();
            string WebUrl = "";
            if (basedata == null)
            {
                WebUrl = "";
            }
            else
            {
                WebUrl = basedata.WebURL;
            }

            //�q��
            //�q���|���w�����\
            string msg;
            Orderset orderset = _OrdersetService.GetOrdersetById(sid);
            List<Place> Places = _placeService.GetPlaceTB().ToList();
            place = _placeService.GetPlaceById(Orderset.Placeid.ToString());
            Models.User user = _userService.GetUserById(orderset.Uid);
            List<Models.User> admins = _userService.GetUserByType(2).ToList();
            UserPointTB = _userService.GetUserPointTB().ToList();
            LineMessageSender lineMessageSender = new LineMessageSender();

            var Place = Places.FirstOrDefault(place => place.Placeid == orderset.Placeid);
            string OrderDate = orderset.Date != null ? orderset.Date.Value.ToString("yyyy-MM-dd") : "N/A";
            string OrderTime = orderset.Time.HasValue ? orderset.Time.Value.ToString(@"hh\:mm") : "N/A";

            _OrdersetService.UpdateOrdersetEvent(sid, Event);
            if (orderset.Event == "0")
            {
                if(Event == "1")
                {

                    //�����w��
                    if (user != null && user.LineUserId != null)
                    {
                        //�ϥΪ̹w���ƶq�W�[
                        _userService.OrderNumAdd1(user.Id);
                        //�q���|��
                        msg = My.Msg_OrderAccept_client;
                        msg = msg.Replace("{Sid}", (sid == null) ? "�L�k���o" : "S" + sid.PadLeft(6, '0'));
                        msg = msg.Replace("{Pname}", (orderset.Pname == null) ? "�L�k���o" : orderset.Pname);
                        msg = msg.Replace("{Date}", (OrderDate == null) ? "�L�k���o" : OrderDate + " " + OrderTime);
                        msg = msg.Replace("{Address}", (Place == null) ? "�L�k���o" : Place.Placetitle + "-" + Place.Placeaddress);
                        msg = msg.Replace("{Url}", WebUrl + "Order");
                        //LineUserAsync(msg, user.LineUserId);
                    }

                }
                else if(Event == "9")
                {
                    //�ڵ��w��
                    if (user != null && user.LineUserId != null)
                    {
                        //�q���|��
                        msg = My.Msg_OrderCancel_client;
                        msg = msg.Replace("{Sid}", (sid == null) ? "�L�k���o":"S" + sid.PadLeft(6, '0'));
                        msg = msg.Replace("{Pname}", (orderset.Pname == null) ? "�L�k���o" : orderset.Pname);
                        msg = msg.Replace("{Date}", (OrderDate == null) ? "�L�k���o" : OrderDate + " " + OrderTime);
                        msg = msg.Replace("{Address}", (Place == null) ? "�L�k���o" : Place.Placetitle + "-" + Place.Placeaddress);
                        msg = msg.Replace("{Url}",WebUrl + "Order");
                        //LineUserAsync(msg, user.LineUserId);
                    }
                }
                //
            }
            else if (orderset.Event == "1")
            {
                //�ڵ��w��
                if (user != null && user.LineUserId != null)
                {
                    //�q���|��
                    msg = My.Msg_OrderCancel_client;
                    msg = msg.Replace("{Sid}", (sid == null) ? "�L�k���o" : "S" + sid.PadLeft(6, '0'));
                    msg = msg.Replace("{Pname}", (orderset.Pname == null) ? "�L�k���o" : orderset.Pname);
                    msg = msg.Replace("{Date}", (OrderDate == null) ? "�L�k���o" : OrderDate + " " + OrderTime);
                    msg = msg.Replace("{Address}", (Place == null) ? "�L�k���o" : Place.Placetitle + "-" + Place.Placeaddress);
                    msg = msg.Replace("{Url}", WebUrl + "Order");
                    //LineUserAsync(msg, user.LineUserId);
                }
                //�q���޲z������
                if (admins != null)
                {
                    foreach (Models.User admin in admins)
                    {
                        if (admin.LineUserId != null)
                        {
                            msg = My.Msg_OrderCancel_op;
                            msg = msg.Replace("{Sid}", (sid == null) ? "�L�k���o" : "S" + sid.PadLeft(6, '0'));
                            msg = msg.Replace("{Pname}", (orderset.Pname == null) ? "�L�k���o" : orderset.Pname);
                            msg = msg.Replace("{Date}", (OrderDate == null) ? "�L�k���o" : OrderDate + " " + OrderTime);
                            msg = msg.Replace("{Address}", (Place == null) ? "�L�k���o" : Place.Placetitle + "-" + Place.Placeaddress);
                            msg = msg.Replace("{Uname}", (user.Name == null) ? "�L�k���o" : user.Name);
                            if (user.LineUserId == null || user.LineUserId == "")
                            {
                                msg = msg.Replace("{LineId}", (user.Line == null) ? "�L�k���o-�Ȥ᥼�j�wLine�۰ʳq���A�Цۦ�q���P�����Ȥ�" : user.Line + ":�Ȥ᥼�j�wLine�q���A�Цۦ�q���P�����Ȥ�");
                            }
                            else
                            {
                                msg = msg.Replace("{LineId}", (user.Line == null) ? "�L�k���o-�Ȥ�w�j�wLine�۰ʳq��" : user.Line + ":�Ȥ�w�j�wLine�۰ʳq��");
                            }
                            msg = msg.Replace("{Url}", WebUrl + "OrdersetCalendar_m");
                            lineMessageSender.SendMessage_Notify(admin.LineUserId, msg);
                        }

                    }
                }
            }

            return RedirectToPage("OrdersetCalendar_m");
        }
    }
}
