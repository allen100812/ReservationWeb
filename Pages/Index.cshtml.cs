using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        public IndexModel(ILogger<IndexModel> logger, IUserService userService, IPlaceService placeService, LineMessageContext lineMessageContext , INewService newService)
        {
            _logger = logger;
            _userService = userService;
            _placeService = placeService;
            _lineMessageContext = lineMessageContext;
            _newService = newService;
        }

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

        }

    }
}