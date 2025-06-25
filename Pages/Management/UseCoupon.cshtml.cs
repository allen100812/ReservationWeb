using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Web0524.Models;
using Web0524.Models.Marketing;

namespace Web0524.Pages.Management
{
    public class UseCouponModel : PageModel
    {
        private readonly IReservationService _reservationService;
        private readonly IMarketingService _marketingService;

        public UseCouponModel(IReservationService reservationService, IMarketingService marketingService)
        {
            _reservationService = reservationService;
            _marketingService = marketingService;
        }

        [BindProperty]
        [Required]
        public int SelectedOrderId { get; set; }

        [BindProperty]
        [Required]
        public string ScannedCode { get; set; }

        public List<SelectListItem> OrderOptions { get; set; } = new();
        public string? Message { get; set; }


        [BindProperty]
        public string TestResult { get; set; } = string.Empty;
        public void OnGet()
        {







            var orders = _reservationService.GetAllOrders()
                .Where(o => o.Status == OrderStatus.Confirmed)
                .Select(o => new SelectListItem
                {
                    Value = o.OrderId.ToString(),
                    Text = $"#{o.OrderId} - {o.ReservationDateTime:yyyy-MM-dd HH:mm}"
                }).ToList();

            OrderOptions = orders;



        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                OnGet(); // reload order list
                return Page();
            }

            var order = _reservationService.GetOrderById(SelectedOrderId);
            if (order == null)
            {
                Message = "找不到指定的訂單。";
                OnGet();
                return Page();
            }

            var result = _marketingService.ApplyCouponToOrder(ScannedCode, order);
            Message = result;
            OnGet();
            return Page();
        }
    }
    

}
