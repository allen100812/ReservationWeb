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

        public List<Coupon> AllCoupons { get; set; } = new();

        [BindProperty]
        public Coupon NewCoupon { get; set; } = new();

        [BindProperty]
        public List<int> SelectedCategories { get; set; } = new();

        public List<SelectListItem> PGroupSelectList { get; set; } = new();


        [BindProperty(SupportsGet = true)]
        public string SearchKeyword { get; set; } = "";

        public List<CouponDispatchRecord> FilteredRecords { get; set; } = new();

        public IActionResult OnGet(string? message = null)
        {
            var check = _userService.CheckCurrentUserPermission(this);
            if (check != null) return check;

            LoadSelectList();
            NewCoupon.ValidFrom = DateTime.Today;
            NewCoupon.ValidTo = DateTime.Today.AddMonths(1);
            AllUser = _userService.GetUserTB().ToList();
            AllPgroup = _pgroupService.GetAllPgroups().ToList();
            AllCoupons = _marketingService.GetAllCoupons();
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


            // ✅ 不論有沒有關鍵字，FilteredRecords 一律賦值
            FilteredRecords = !string.IsNullOrWhiteSpace(SearchKeyword)
                ? _marketingService.SearchCouponRecords(SearchKeyword)
                : new List<CouponDispatchRecord>();

            return Page();
        }
        [IgnoreAntiforgeryToken]

        public IActionResult OnPostToggleStatus(int id)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            _marketingService.ToggleCouponStatus(id);
            return new JsonResult(new { success = true, message = "已切換狀態！" });
        }


        [IgnoreAntiforgeryToken]
        public IActionResult OnPostDeleteRecord(int id)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            var ok = _marketingService.DeleteCouponRecord(id);
            return new JsonResult(new
            {
                success = ok,
                message = ok ? "✅ 已刪除該優惠券紀錄" : "❌ 刪除失敗"
            });
        }

        [IgnoreAntiforgeryToken]
        public IActionResult OnPostBatchDistribute(int SelectedCouponId, string TargetMemberIds)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            var ids = TargetMemberIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var id in ids)
            {
                Console.WriteLine($"🚀 CouponId: {SelectedCouponId}, MemberId: {id}");

                if (!string.IsNullOrWhiteSpace(id))
                    _marketingService.AssignCouponToMember(id, SelectedCouponId);
            }

            return new JsonResult(new { success = true, message = "優惠券已派發成功！" });
        }


        public IActionResult OnPostAddPoints(string MemberId, int Points, string Action)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            if (Points > 0)
                _marketingService.AddPoints(MemberId, Points, Action);
            else
                _marketingService.DeductPoints(MemberId, -Points, Action);

            return RedirectToPage(new { memberId = MemberId });
        }
        public IActionResult OnPostAddCoupon()
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            LoadSelectList();

            // ⚠️ 先產生優惠券代碼，再驗證
            NewCoupon.Code = "CPN" + DateTime.Now.ToString("yyyyMMdd") + Guid.NewGuid().ToString("N")[..4].ToUpper();

            if (SelectedCategories.Any())
                NewCoupon.CategoryLimit = string.Join(",", SelectedCategories);

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(NewCoupon);
            bool isValid = Validator.TryValidateObject(NewCoupon, context, validationResults, true);

            if (!isValid)
            {
                ViewData["Message"] = "❌ 表單驗證失敗：<br/>" + string.Join("<br/>",
                    validationResults.Select(vr => $"【{string.Join(", ", vr.MemberNames)}】{vr.ErrorMessage}"));
                return Page();
            }

            NewCoupon.CouponSource = NewCoupon.IsWelcome
                ? CouponSourceEnum.Register
                : NewCoupon.AutoAssign
                    ? (NewCoupon.AutoAssignRule == AutoAssignRuleEnum.BeforeBirthday20 ? CouponSourceEnum.Birthday
                       : NewCoupon.AutoAssignRule == AutoAssignRuleEnum.SpecificDate ? CouponSourceEnum.Campaign
                       : CouponSourceEnum.Manual)
                    : CouponSourceEnum.Manual;

            _marketingService.AddSystemCoupon(NewCoupon);

            return RedirectToPage(new { message = "新增成功！" });
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
        public JsonResult OnGetGetCouponInfo(int id)
        {

            AllCoupons = _marketingService.GetAllCoupons();
            var coupon = AllCoupons.FirstOrDefault(c => c.CouponId == id);
            if (coupon == null)
                return new JsonResult(new { success = false });

            return new JsonResult(new
            {
                success = true,
                title = coupon.Title,
                code = coupon.Code
            });
        }

        public JsonResult OnGetGetMemberInfo(string id)
        {
            AllUser = _userService.GetUserTB().ToList();

            var member = AllUser.FirstOrDefault(u => u.Id == id);
            if (member == null)
                return new JsonResult(new { success = false });

            return new JsonResult(new
            {
                success = true,
                name = member.Name
            });
        }


    }
}
