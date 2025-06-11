using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class DesignerManagementModel : PageModel
    {
        private readonly IReservationService _reservationService;
        private readonly IProductService _productService;

        public DesignerManagementModel(IReservationService reservationService, IProductService productService)
        {
            _reservationService = reservationService;
            _productService = productService;
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

        public void OnGet()
        {
            AllDesigners = _reservationService.GetAllDesigners();
            AllProducts = _productService.GetAllProducts().ToList();
        }

        public IActionResult OnPost(string action)
        {
            AllDesigners = _reservationService.GetAllDesigners();
            AllProducts = _productService.GetAllProducts().ToList();

            switch (action)
            {
                case "add":
                    if (!string.IsNullOrWhiteSpace(NewDesigner.Name))
                    {
                        _reservationService.AddDesigner(NewDesigner);
                        Message = "設計師已新增。";
                    }
                    else
                    {
                        Message = "設計師姓名不得為空。";
                    }
                    break;

                case "delete":
                    if (DesignerId > 0)
                    {
                        _reservationService.DeleteDesigner(DesignerId);
                        Message = "設計師已刪除。";
                    }
                    break;

                case "addrule":
                    if (DesignerId > 0 && NewRule.ProductId > 0 && NewRule.DurationMinutes > 0 && NewRule.MaxCustomers > 0)
                    {
                        var success = _reservationService.AddScheduleRule(DesignerId, NewRule);
                        Message = success ? "已新增排程規則。" : "新增排程規則失敗。";
                    }
                    else
                    {
                        Message = "請填寫完整的規則資訊。";
                    }
                    break;

                default:
                    Message = "未知操作。";
                    break;
            }

            AllDesigners = _reservationService.GetAllDesigners();
            return Page();
        }
    }
}
