using MDP.DevKit.LineMessaging;
using Microsoft.Extensions.Primitives;
using Quartz;
using Web0524.Models;

namespace Web0524.Controllers
{
    
    public class AutoNotify: IJob
    {
        private readonly IOrderService _orderService;
        private readonly IUserService _userService;
        private readonly LineMessageContext _lineMessageContext;
        private readonly IPlaceService _placeService;
        private readonly IMyService _myService;

        DateTime localTime = DateTime.Now;
        DateTime taipeiTime;
        // 取得台北時區的TimeZoneInfo
        TimeZoneInfo taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
        public AutoNotify(LineMessageContext lineMessageContext, IOrderService orderService, IUserService userService, IPlaceService placeService, IMyService myService)
        {
            if (lineMessageContext == null) throw new ArgumentException($"{nameof(lineMessageContext)}=null");
            _lineMessageContext = lineMessageContext;
            _orderService = orderService;
            _userService = userService;
            _placeService = placeService;
            _myService = myService;
        }
        public My basedata { get; set; }
        public Task Execute(IJobExecutionContext context)
        {

            return Task.CompletedTask;
        }
    }
}
