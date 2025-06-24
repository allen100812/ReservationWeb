using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class DesignerManagementModel : PageModel
    {
        private readonly IReservationService _reservationService;
        private readonly IProductService _productService;
        private readonly IUserService _userService;

        public DesignerManagementModel(IReservationService reservationService, IProductService productService, IUserService userService)
        {
            _reservationService = reservationService;
            _productService = productService;
            _userService = userService;
        }

        public List<Product> AllProducts { get; set; } = new();
        public List<Designer> AllDesigners { get; set; } = new();

        [BindProperty]
        public Designer NewDesigner { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int DesignerId { get; set; }

        [BindProperty]
        public Designer_ProductScheduleRule NewRule { get; set; }

        public string Message { get; set; } = string.Empty;

        public string DesignersJson { get; set; } = string.Empty;


        public IActionResult OnGet()
        {
            var check = _userService.CheckCurrentUserPermission(this);
            if (check != null) return check;
            LoadData();
            return Page();
        }


        [IgnoreAntiforgeryToken]
        public IActionResult OnPost(string action)
        {

            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            switch (action)
            {
                case "add":
                    if (!string.IsNullOrWhiteSpace(NewDesigner.Name))
                    {
                        _reservationService.AddDesigner(NewDesigner);
                        TempData["StatusMessage"] = "設計師已新增。";
                        TempData["StatusSuccess"] = true;
                    }
                    else
                    {
                        TempData["StatusMessage"] = "設計師姓名不得為空。";
                        TempData["StatusSuccess"] = false;
                    }
                    break;

                case "edit":
                    if (NewDesigner.DesignerId > 0)
                    {
                        _reservationService.UpdateDesigner(NewDesigner);
                        TempData["StatusMessage"] = "設計師資料已更新。";
                        TempData["StatusSuccess"] = true;
                    }
                    else
                    {
                        TempData["StatusMessage"] = "無效的設計師 ID。";
                        TempData["StatusSuccess"] = false;
                    }
                    break;

                case "deactivate":
                    if (DesignerId > 0)
                    {
                        _reservationService.DeleteDesigner(DesignerId);
                        TempData["StatusMessage"] = "設計師已停用。";
                        TempData["StatusSuccess"] = true;
                    }
                    break;

                case "activate":
                    if (DesignerId > 0)
                    {
                        _reservationService.RestoreDesigner(DesignerId);
                        TempData["StatusMessage"] = "設計師已啟用。";
                        TempData["StatusSuccess"] = true;
                    }
                    break;

                case "addrule":
                    if (DesignerId > 0 && NewRule.ProductId > 0 && NewRule.DurationMinutes > 0 && NewRule.MaxCustomers > 0)
                    {
                        var success = _reservationService.AddScheduleRule(DesignerId, NewRule);
                        TempData["StatusMessage"] = success ? "已新增排程規則。" : "新增排程規則失敗。";
                        TempData["StatusSuccess"] = success;
                    }
                    else
                    {
                        TempData["StatusMessage"] = "請填寫完整的規則資訊。";
                        TempData["StatusSuccess"] = false;
                    }
                    break;
            }

            return RedirectToPage();
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostUpdateRule(int ruleId, int duration, int max)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            var result = _reservationService.UpdateScheduleRule(ruleId, duration, max);
            return new JsonResult(new { success = result });
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnPostDeleteRule(int ruleId)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            var result = _reservationService.DeleteScheduleRule(ruleId);
            return new JsonResult(new { success = result });
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnPostAddRule(int designerId, int productId, int duration, int max)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            var rule = new Designer_ProductScheduleRule
            {
                ProductId = productId,
                DurationMinutes = duration,
                MaxCustomers = max
            };

            var success = _reservationService.AddScheduleRule(designerId, rule);
            return new JsonResult(new { success });
        }
        public JsonResult OnPostDeleteDesigner(int designerId)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            var result = _reservationService.DeleteDesigner(designerId);
            return new JsonResult(new { success = result });
        }

        public JsonResult OnPostRestoreDesigner(int designerId)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            var result = _reservationService.RestoreDesigner(designerId);
            return new JsonResult(new { success = result });
        }


        private void LoadData()
        {
            AllDesigners = _reservationService.GetAllDesigners();
            AllProducts = _productService.GetAllProducts().Where(x => x.IsDeleted == false).ToList();
        }
    }
}
