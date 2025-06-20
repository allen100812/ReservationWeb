using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Web0524.Models;
using Web0524.Models.Marketing;

namespace Web0524.Pages.Management
{
    public class CouponPointManagementModel : PageModel
    {
        private readonly IMarketingService _marketingService;
        private readonly IUserService _userService;
        private readonly IPgroupService _pgroupService;
        public CouponPointManagementModel(IMarketingService marketingService, IUserService userService, IPgroupService pgroupService)
        {
            _marketingService = marketingService;
            _userService = userService;
            _pgroupService = pgroupService;
        }

        [BindProperty(SupportsGet = true)]
        public string MemberId { get; set; }

        public List<Models.User> AllUser { get; set; } = new();
        public List<Pgroup> AllPgroup { get; set; } = new();
        public List<CouponDispatchRecord> AllCouponRecords { get; set; } = new();
        public Dictionary<int, Coupon> CouponMap { get; set; } = new();
        public int MemberPoints { get; set; }
        public List<PointLog> PointLogs { get; set; } = new();



        [BindProperty]
        public Coupon NewCoupon { get; set; } = new();

        [BindProperty]
        public List<int> SelectedCategories { get; set; } = new();

        public List<SelectListItem> PGroupSelectList { get; set; } = new();
        public void OnGet(string? message = null)
        {

            LoadSelectList();
            NewCoupon.ValidFrom = DateTime.Today;
            NewCoupon.ValidTo = DateTime.Today.AddMonths(1);
            AllUser = _userService.GetUserTB().ToList();
            AllPgroup = _pgroupService.GetAllPgroups().ToList();
            if (!string.IsNullOrWhiteSpace(MemberId))
            {
                // ✅ 取得點數資料
                MemberPoints = _marketingService.GetMemberPoints(MemberId);
                PointLogs = _marketingService.GetPointHistory(MemberId);

                // ✅ 取得優惠券派發記錄與對應的優惠券資料
                AllCouponRecords = _marketingService.GetAllCouponRecords(MemberId);
                CouponMap = _marketingService.GetCouponMapByRecordList(AllCouponRecords);
            }
            if (!string.IsNullOrEmpty(message))
                ViewData["Message"] = message;
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
            Console.WriteLine("👉 OnPostAddCfffoupon triggered");
            if (Points > 0)
                _marketingService.AddPoints(MemberId, Points, Action);
            else
                _marketingService.DeductPoints(MemberId, -Points, Action);

            return RedirectToPage(new { memberId = MemberId });
        }
        public IActionResult OnPostAddCoupon()
        {
            Console.WriteLine("👉 OnPostAddCoupon triggered12");
            LoadSelectList();

            if (SelectedCategories.Any())
                NewCoupon.CategoryLimit = string.Join(",", SelectedCategories);

            // ✅ 僅驗證 NewCoupon，不用 ModelState
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(NewCoupon);
            bool isValid = Validator.TryValidateObject(NewCoupon, context, validationResults, true);

            if (!isValid)
            {
                // 將錯誤訊息彙整輸出
                ViewData["Message"] = "❌ 表單驗證失敗：<br/>" + string.Join("<br/>",
                    validationResults.Select(vr => $"【{string.Join(", ", vr.MemberNames)}】{vr.ErrorMessage}"));
                return Page();
            }
            // 自動產生代碼
            NewCoupon.Code = "CPN" + DateTime.Now.ToString("yyyyMMdd") + Guid.NewGuid().ToString("N")[..4].ToUpper();

            // CouponSource 對應邏輯
            NewCoupon.CouponSource = NewCoupon.IsWelcome
                ? CouponSourceEnum.Register
                : NewCoupon.AutoAssign
                    ? (NewCoupon.AutoAssignRule == AutoAssignRuleEnum.BeforeBirthday20 ? CouponSourceEnum.Birthday
                       : NewCoupon.AutoAssignRule == AutoAssignRuleEnum.SpecificDate ? CouponSourceEnum.Campaign
                       : CouponSourceEnum.Manual)
                    : CouponSourceEnum.Manual;

            _marketingService.AddSystemCoupon(NewCoupon);

            ViewData["Message"] = "✅ 優惠券已成功建立！";
            return RedirectToPage(new { Message = "新增成功！" });
        }

        private void LoadSelectList()
        {
            PGroupSelectList = _pgroupService.GetAllPgroups()
                .Select(p => new SelectListItem
                {
                    Value = p.PGid.ToString(),
                    Text = p.PGname
                }).ToList();
        }

        private string GetModelErrors()
        {
            var messages = new List<string> { "❌ 表單驗證失敗：" };

            foreach (var entry in ModelState)
            {
                if (!entry.Key.StartsWith(nameof(NewCoupon))) continue; // 僅顯示 NewCoupon 屬性錯誤

                foreach (var error in entry.Value.Errors)
                {
                    // 顯示的欄位名稱去除前綴 "NewCoupon."
                    var fieldName = entry.Key.Replace(nameof(NewCoupon) + ".", "");
                    messages.Add($"【{fieldName}】{error.ErrorMessage}");
                }
            }

            return string.Join("<br/>", messages);
        }

    }
}
