using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    public class ReportModel : PageModel
    {
        private readonly IReportService _reportService;
        private readonly IUserService _userService;

        public ReportModel(IReportService reportService, IUserService userService)
        {
            _reportService = reportService;
            _userService = userService;
        }

        [BindProperty] public string ReportType { get; set; }
        [BindProperty] public DateTime? StartDate { get; set; }
        [BindProperty] public DateTime? EndDate { get; set; }
        [BindProperty] public int? DesignerId { get; set; }
        [BindProperty] public int TopN { get; set; } = 20;

        public object ResultData { get; set; }
        public string Today => DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        [IgnoreAntiforgeryToken]

        public async Task<IActionResult> OnPostAjaxAsync()
        {

            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            switch (ReportType)
            {
                case "reservationSummary":
                    ResultData = await _reportService.GetReservationSummaryAsync(StartDate, EndDate);
                    break;
                case "designerStats":
                    ResultData = await _reportService.GetDesignerReservationStatsAsync(StartDate, EndDate);
                    break;
                case "peakHour":
                    ResultData = await _reportService.GetPeakHourStatsAsync(StartDate, EndDate);
                    break;
                case "revenue":
                    ResultData = await _reportService.GetRevenueSummaryAsync(StartDate, EndDate);
                    break;
                case "designerRevenue":
                    ResultData = await _reportService.GetDesignerRevenueAsync(StartDate, EndDate);
                    break;
                case "paymentMethod":
                    ResultData = await _reportService.GetPaymentMethodStatsAsync(StartDate, EndDate);
                    break;
                case "memberActivity":
                    ResultData = await _reportService.GetMemberActivityAsync(StartDate, EndDate);
                    break;
                case "pointUsage":
                    ResultData = await _reportService.GetPointUsageSummaryAsync(StartDate, EndDate);
                    break;
                case "topMember":
                    ResultData = await _reportService.GetTopMembersAsync(StartDate, EndDate, TopN);
                    break;
                case "couponUsage":
                    ResultData = await _reportService.GetCouponUsageSummaryAsync(StartDate, EndDate);
                    break;
                case "reservationDetail":
                    ResultData = await _reportService.GetReservationDetailsAsync(StartDate, EndDate, DesignerId);
                    break;
                default:
                    return BadRequest("未知的報表類型");
            }

            return new JsonResult(ResultData);
        }
        [IgnoreAntiforgeryToken]

        public async Task<IActionResult> OnPostAsync()
        {
            // 頁面回傳模式用不到時可略過
            return Page();
        }
    }
}
