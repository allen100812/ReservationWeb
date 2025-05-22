using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class OrderEditModel : PageModel
    {
        private readonly IOrdersetService _OrdersetService;
        private readonly IPlaceService _placeService;
        private readonly LineMessageContext _lineMessageContext;
        private readonly IUserService _userService;
        private readonly IMyService _myService;
        public OrderEditModel(IOrdersetService OrdersetService, IPlaceService placeService, LineMessageContext lineMessageContext, IUserService userService, IMyService myService)
        {
            _OrdersetService = OrdersetService;
            _placeService = placeService;
            _lineMessageContext = lineMessageContext;
            _userService = userService;
            _myService = myService;
        }


        [BindProperty]
        public Orderset Orderset { get; set; }
        public Place place { get; set; }
        public List<Orderset> Orderset_CheckDate { get; set; }
        public Models.User user { get; set; }
        public List<Models.User> admins { get; set; }
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

            return Page();
        }

        public IActionResult OnPost(string? sid, string? OrigEvent, string? SetEvent)
        {
            if (sid == null || SetEvent == null || OrigEvent == null)
            {
                TempData["error"] = true;
                return NotFound();
            }
            Orderset Orderset_temp = _OrdersetService.GetOrdersetById(sid);
            place = _placeService.GetPlaceById(Orderset_temp.Placeid.ToString());
            user = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            admins = _userService.GetUserByType(2).ToList();
            basedata = _myService.GetBaseData();
            LineMessageSender lineMessageSender = new LineMessageSender();
            string WebUrl = "";
            if (basedata == null)
            {
                WebUrl = "";
            }
            else
            {
                WebUrl = basedata.WebURL;
            }

            _OrdersetService.UpdateOrdersetEvent(sid, SetEvent);

            string msg;
            if (OrigEvent == "1" && user != null && user.LineUserId != null)
            {
                //�ϥΪ̹w�������ƶq�W�[
                _userService.CancelNumAdd1(user.Id);
                //�q���|��
                msg = My.Msg_OrderCancel_client;
                msg = msg.Replace("{Sid}", (sid == null) ? "�L�k���o" : "S" + sid.PadLeft(6, '0'));
                msg = msg.Replace("{Pname}", (Orderset_temp.Pname == null) ? "�L�k���o" : Orderset_temp.Pname.ToString());
                msg = msg.Replace("{Date}", (Orderset_temp.Date == null) ? "�L�k���o" : Orderset_temp.Date.ToString());
                msg = msg.Replace("{Address}", (place.Placetitle == null) ? "�L�k���o" : (place.Placetitle));
                msg = msg.Replace("{Url}", WebUrl + "ProductList");
                //LineUserAsync(msg, user.LineUserId);
            }
            //�q���޲z�����s���w��
            if (OrigEvent == "1" && admins != null)//�u���w�Q�������q��~�|�q���޲z��
            {
                foreach (Models.User admin in admins)
                {
                    if (admin.LineUserId != null)
                    {
                        msg = My.Msg_OrderCancel_op;
                        msg = msg.Replace("{Sid}", (sid == null) ? "�L�k���o" : "S" + sid.PadLeft(6, '0'));
                        msg = msg.Replace("{Pname}", (Orderset_temp.Pname == null) ? "�L�k���o" : Orderset_temp.Pname.ToString());
                        msg = msg.Replace("{Date}", (Orderset_temp.Date == null) ? "�L�k���o" : Orderset_temp.Date.ToString());
                        msg = msg.Replace("{Address}", (place.Placetitle == null) ? "�L�k���o" : (place.Placetitle));
                        msg = msg.Replace("{Uname}", (user.Id== null) ? "�L�k���o" :user.Id+"("+user.Name+")");
                        //if (user.LineUserId == null || user.LineUserId == "")
                        //{
                        //    msg = msg.Replace("{LineId}", (user.Line == null) ? "�L�k���o-�Ȥ᥼�j�wLine�۰ʳq���A�Цۦ�q���P�����Ȥ�" : user.Line + ":�Ȥ᥼�j�wLine�q���A�Цۦ�q���P�����Ȥ�");
                        //}
                        //else
                        //{
                        //    msg = msg.Replace("{LineId}", (user.Line == null) ? "�L�k���o-�Ȥ�w�j�wLine�۰ʳq��" : user.Line + ":�Ȥ�w�j�wLine�۰ʳq��");
                        //}
                        msg = msg.Replace("{LineId}", (user.Line == null) ? "�L�k���o" : user.Line);
                        msg = msg.Replace("{Url}", WebUrl + "OrdersetCalendar_m");
                        lineMessageSender.SendMessage_Notify(admin.LineUserId, msg);
                    }

                }
            }
            TempData["success"] = true;
            return RedirectToPage("Order");
        }
    }
}
