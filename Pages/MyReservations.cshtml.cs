using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    public class MyReservationsModel : PageModel
    {
        private readonly IReservationService _reservationService;
        private readonly IProductService _productService;

        public MyReservationsModel(IReservationService reservationService, IProductService productService)
        {
            _reservationService = reservationService;
            _productService = productService;
        }

        public List<Order> MyOrders { get; set; } = new();
        public List<Designer> AllDesigners { get; set; } = new();
        public List<Product> AllProducts { get; set; } = new();

        [BindProperty(SupportsGet = true)] public int? FilterDesignerId { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterProductName { get; set; }
        [BindProperty(SupportsGet = true)] public OrderStatus? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FilterDate { get; set; }

        private readonly int CancelLimitHours = 2; // 可取消時限小時，0 表示不可取消

        public IActionResult OnGet()
        {
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(uid)) return Redirect("/Account/Login");

            var orders = _reservationService.GetOrdersByMemberId(uid);

            if (FilterDesignerId.HasValue)
                orders = orders.Where(o => o.DesignerId == FilterDesignerId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(FilterProductName))
                orders = orders.Where(o => GetProductName(o.ProductId).Contains(FilterProductName)).ToList();

            if (FilterStatus.HasValue)
                orders = orders.Where(o => o.Status == FilterStatus.Value).ToList();

            if (FilterDate.HasValue)
                orders = orders.Where(o => o.ReservationDateTime.Date == FilterDate.Value.Date).ToList();

            MyOrders = orders;
            AllDesigners = _reservationService.GetAllDesigners();
            var productIds = MyOrders.Select(o => o.ProductId).Distinct();
            AllProducts = _productService.GetAllProducts().Where(p => productIds.Contains(p.ProductId)).ToList();

            return Page();
        }

        public IActionResult OnPostCancel(int id)
        {
            var order = _reservationService.GetOrderById(id);
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (order == null || order.Uid != uid || order.Status != OrderStatus.Pending)
                return RedirectToPage();

            if (CancelLimitHours > 0 && (order.ReservationDateTime - DateTime.Now).TotalHours < CancelLimitHours)
                return RedirectToPage();

            _reservationService.CancelOrder(id);
            return RedirectToPage();
        }

        public string GetDesignerName(int designerId) =>
            AllDesigners.FirstOrDefault(d => d.DesignerId == designerId)?.Name ?? "未知設計師";

        public string GetProductName(int productId) =>
            AllProducts.FirstOrDefault(p => p.ProductId == productId)?.Name ?? "未知服務";
    }
}
