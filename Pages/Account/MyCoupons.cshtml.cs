using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;
using Web0524.Models.Marketing;

namespace Web0524.Pages.Account
{
    public class MyCouponsModel : PageModel
    {
        private readonly IMarketingService _marketingService;
        private readonly IUserService _userService;

        public MyCouponsModel(IMarketingService marketingService, IUserService userService)
        {
            _marketingService = marketingService;
            _userService = userService;
        }

        public List<CouponDispatchRecord> DispatchRecords { get; set; } = new();
        public Dictionary<int, Coupon> CouponMap { get; set; } = new();

        public IActionResult OnGet()
        {
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(uid)) return Redirect("/Account/Login");

            DispatchRecords = _marketingService.GetAvailableCouponRecords(uid);
            CouponMap = _marketingService.GetCouponMapByRecordList(DispatchRecords);

            return Page();
        }

        public JsonResult OnGetGenerateQR(int recordId)
        {

            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "µLÅv­­" });
            }

            var qr = _marketingService.GenerateCouponQRCode(recordId);
            return new JsonResult(new { qr });
        }
    }

}
