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
        private readonly IOrderService _orderService;
        private readonly ILogger<IndexModel> _logger;

        private readonly LineMessageContext _lineMessageContext;
        private readonly INewService _newService;

        private readonly IReservationService _reservationService;
        public IndexModel(ILogger<IndexModel> logger, IUserService userService, LineMessageContext lineMessageContext , INewService newService, IReservationService reservationService, IOrderService orderService)
        {
            _logger = logger;
            _userService = userService;

            _lineMessageContext = lineMessageContext;
            _newService = newService;
            _reservationService = reservationService;
            _orderService = orderService;
        }

        public string ReservationTestResult { get; set; } // 顯示在前端用

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
            newLists = _newService.GetNewTB().ToList();
            //LineMessageSender lineMessageSender = new LineMessageSender();
            //lineMessageSender.SendMessage("Udf96f6192a32d72329c908a69805aa9e", "官方帳號Push方法");
            //lineMessageSender.SendMessage_Notify("Notify傳送訊息方法");
            // 測試用 User


        }









    }


}