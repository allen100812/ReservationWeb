using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;
using Web0524.Models.Marketing;

namespace Web0524.Pages
{
    public class MyReservationsModel : PageModel
    {
        private readonly IReservationService _reservationService;
        private readonly IProductService _productService;
        private readonly IMarketingService _marketingService;

        public MyReservationsModel(
            IReservationService reservationService,
            IProductService productService,
            IMarketingService marketingService)
        {
            _reservationService = reservationService;
            _productService = productService;
            _marketingService = marketingService;
        }

        public List<Order> MyOrders { get; set; } = new();
        public List<Designer> AllDesigners { get; set; } = new();
        public List<Product> AllProducts { get; set; } = new();
        public List<Coupon> AllCoupons { get; set; } = new();

        public string StorePhoneNumber => "0987654321"; // 可替換為設定檔來源

        [BindProperty(SupportsGet = true)] public int? FilterDesignerId { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterProductName { get; set; }
        [BindProperty(SupportsGet = true)] public OrderStatus? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FilterMonth { get; set; }

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

            if (FilterMonth.HasValue)
            {
                var month = FilterMonth.Value;
                orders = orders.Where(o => o.ReservationDateTime.Year == month.Year &&
                                           o.ReservationDateTime.Month == month.Month).ToList();
            }
            else
            {
                var now = DateTime.Now;
                orders = orders.Where(o => o.ReservationDateTime.Year == now.Year &&
                                           o.ReservationDateTime.Month == now.Month).ToList();
            }

            MyOrders = orders;

            AllDesigners = _reservationService.GetAllDesigners();

            var productIds = MyOrders.Select(o => o.ProductId).Distinct();
            AllProducts = _productService.GetAllProducts().Where(p => productIds.Contains(p.ProductId)).ToList();

            var usedCouponIds = MyOrders.Where(o => o.UsedCouponId.HasValue)
                                        .Select(o => o.UsedCouponId.Value)
                                        .Distinct()
                                        .ToList();

            AllCoupons = _marketingService.GetAllCoupons();

            return Page();
        }

        public IActionResult OnPostCancel(int id)
        {
            var order = _reservationService.GetOrderById(id);
            var uid = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;

            if (order == null || order.Uid != uid || order.Status != OrderStatus.Pending)
                return RedirectToPage();

            _reservationService.CancelOrder(id,true);
            return RedirectToPage();
        }

        public string GetDesignerName(int designerId) =>
            AllDesigners.FirstOrDefault(d => d.DesignerId == designerId)?.Name ?? "未知設計師";

        public string GetProductName(int productId) =>
            AllProducts.FirstOrDefault(p => p.ProductId == productId)?.Name ?? "未知服務";

        public string GetPaymentMethodName(OrderPaymentMethod method) =>
            method switch
            {
                OrderPaymentMethod.Cash => "現金",
                OrderPaymentMethod.CreditCard => "刷卡",
                OrderPaymentMethod.LinePay => "LinePay",
                _ => method.ToString()
            };

        public string GetCouponTitle(int? couponId)
        {
            if (!couponId.HasValue) return "—";
            return AllCoupons.FirstOrDefault(c => c.CouponId == couponId.Value)?.Title ?? "未知優惠券";
        }
    }
}
