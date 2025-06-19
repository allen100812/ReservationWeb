using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;
using Web0524.Models.Marketing;

namespace Web0524.Pages.Management
{
    public class CouponPointManagementModel : PageModel
    {
        private readonly IMarketingService _marketingService;
        private readonly IUserService _userService;

        public CouponPointManagementModel(IMarketingService marketingService, IUserService userService)
        {
            _marketingService = marketingService;
            _userService = userService;
        }

        [BindProperty(SupportsGet = true)]
        public string MemberId { get; set; }

        public List<User> AllUser { get; set; } = new();
        public List<CouponDispatchRecord> AllCouponRecords { get; set; } = new();
        public Dictionary<int, Coupon> CouponMap { get; set; } = new();
        public int MemberPoints { get; set; }
        public List<PointLog> PointLogs { get; set; } = new();

        public void OnGet()
        {
            AllUser = _userService.GetUserTB().ToList();

            if (!string.IsNullOrWhiteSpace(MemberId))
            {
                // ✅ 取得點數資料
                MemberPoints = _marketingService.GetMemberPoints(MemberId);
                PointLogs = _marketingService.GetPointHistory(MemberId);

                // ✅ 取得優惠券派發記錄與對應的優惠券資料
                AllCouponRecords = _marketingService.GetAllCouponRecords(MemberId);
                CouponMap = _marketingService.GetCouponMapByRecordList(AllCouponRecords);
            }
        }

        public IActionResult OnPostToggleStatus(int id)
        {
            _marketingService.ToggleCouponStatus(id);
            return RedirectToPage(new { memberId = MemberId });
        }

        public IActionResult OnPostBatchDistribute(int SelectedCouponId, string TargetMemberIds)
        {
            var ids = TargetMemberIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var id in ids)
            {
                _marketingService.AssignCouponToMember(id, SelectedCouponId);
            }
            return RedirectToPage(new { memberId = MemberId });
        }

        public IActionResult OnPostAddPoints(string MemberId, int Points, string Action)
        {
            if (Points > 0)
                _marketingService.AddPoints(MemberId, Points, Action);
            else
                _marketingService.DeductPoints(MemberId, -Points, Action);

            return RedirectToPage(new { memberId = MemberId });
        }
    }
}
