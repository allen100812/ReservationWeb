using Google.Apis.Calendar.v3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Web0524.Models;
using Web0524.Models.Marketing;

namespace Web0524.Pages.Management
{
    public class CreateCouponModel : PageModel
    {
        private readonly IMarketingService _marketingService;
        private readonly IPgroupService _pgroupService;

        public CreateCouponModel(IMarketingService marketingService, IPgroupService pgroupService)
        {
            _marketingService = marketingService;
            _pgroupService = pgroupService;
        }

        [BindProperty]
        public Coupon NewCoupon { get; set; } = new();

        [BindProperty]
        public List<int> SelectedCategories { get; set; } = new();

        public List<SelectListItem> PGroupSelectList { get; set; } = new();

        public void OnGet()
        {
            LoadSelectList();
            NewCoupon.ValidFrom = DateTime.Today;
            NewCoupon.ValidTo = DateTime.Today.AddMonths(1);
        }

        public IActionResult OnPost()
        {
            LoadSelectList();

            if (SelectedCategories.Any())
                NewCoupon.CategoryLimit = string.Join(",", SelectedCategories);

            // 驗證失敗處理
            if (!ModelState.IsValid)
            {
                ViewData["Message"] = GetModelErrors();
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
            return Page();
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
                foreach (var error in entry.Value.Errors)
                {
                    messages.Add($"【{entry.Key}】{error.ErrorMessage}");
                }
            }
            return string.Join("<br/>", messages);
        }
    }

}
