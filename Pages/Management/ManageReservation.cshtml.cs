using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;
using Web0524.Models.Marketing;

namespace Web0524.Pages.Management
{
    public class ManageReservationModel : PageModel
    {
        private readonly IReservationService _reservationService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IMarketingService _marketingService;
        public ManageReservationModel(IReservationService reservationService, IUserService userService, IProductService productService,
            IMarketingService marketingService)
        {
            _reservationService = reservationService;
            _userService = userService;
            _productService = productService;
            _marketingService = marketingService;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public List<Reservation_AvailableSlotDetail> AvailableSlotDetails { get; set; } = new();
        public List<Designer> AllDesigners { get; set; } = new();
        public List<Order> AllOrders { get; set; } = new();
        public List<Web0524.Models.User> AllUsers { get; set; } = new();

        public List<Product> AllProducts { get; set; } = new();
        public List<Coupon> AllCoupons { get; set; } = new();
        [BindProperty]
        public Order NewOrder { get; set; } = new();

        public string Message { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)] public int? FilterDesignerId { get; set; }
        [BindProperty(SupportsGet = true)] public string? FilterProductName { get; set; }
        [BindProperty(SupportsGet = true)] public OrderStatus? FilterStatus { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FilterStartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? FilterEndDate { get; set; }

        [BindProperty]
        public int SelectedOrderId { get; set; }

        [BindProperty]
        public string ScannedCode { get; set; }

        public IActionResult OnGet()
        {

            var check = _userService.CheckCurrentUserPermission(this);
            if (check != null) return check;

            AllDesigners = _reservationService.GetAllDesigners();
            AllUsers = _userService.GetUserTB().ToList();
            AllProducts = _productService.GetAllProducts().ToList(); // ✅ 加這行
            AllCoupons = _marketingService.GetAllCoupons().ToList(); // 如有使用優惠券標題也建議補上

            var orders = _reservationService.GetAllOrders();

            if (FilterDesignerId.HasValue)
                orders = orders.Where(o => o.DesignerId == FilterDesignerId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(FilterProductName))
                orders = orders.Where(o => GetProductName(o.ProductId).Contains(FilterProductName)).ToList();

            if (FilterStatus.HasValue)
                orders = orders.Where(o => o.Status == FilterStatus.Value).ToList();

            if (FilterStartDate.HasValue && FilterEndDate.HasValue)
            {
                var start = FilterStartDate.Value.Date;
                var end = FilterEndDate.Value.Date.AddDays(1).AddSeconds(-1); // 包含整天
                orders = orders.Where(o => o.ReservationDateTime >= start && o.ReservationDateTime <= end).ToList();
            }
            else
            {
                // 預設顯示當月資料
                var now = DateTime.Now;
                var start = new DateTime(now.Year, now.Month, 1);
                var end = start.AddMonths(1).AddSeconds(-1);
                orders = orders.Where(o => o.ReservationDateTime >= start && o.ReservationDateTime <= end).ToList();
            }


            AllOrders = orders;

            // 可用時段（非必要）
            AvailableSlotDetails = new();
            foreach (var designer in AllDesigners)
            {
                var slots = _reservationService.GetAvailableServiceSlots(
                    designerId: designer.DesignerId,
                    date: DateTime.Today,
                    cooldownMinutes: 10,
                    advanceMinutes: 10);
                AvailableSlotDetails.AddRange(slots);
            }

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
            // 重新載入必要資料
            AllDesigners = _reservationService.GetAllDesigners();
            AllUsers = _userService.GetUserTB().ToList();
            AllProducts = _productService.GetAllProducts().ToList();
            AllCoupons = _marketingService.GetAllCoupons().ToList();

            if (SelectedDate == default)
                SelectedDate = DateTime.Today;

            switch (action)
            {
                case "apply_coupon":
                    var form = Request.Form;
                    if (!int.TryParse(form["SelectedOrderId"], out int selectedOrderId) || string.IsNullOrWhiteSpace(form["ScannedCode"]))
                    {
                        Message = "❌ 缺少優惠資訊。";
                        break;
                    }

                    var order = _reservationService.GetOrderById(selectedOrderId);
                    if (order == null)
                    {
                        Message = "❌ 找不到指定的訂單。";
                        break;
                    }

                    var code = form["ScannedCode"];
                    var result = _marketingService.ApplyCouponToOrder(code, order);
                    Message = result;
                    break;



                case "update":
                    if (NewOrder.OrderId <= 0)
                    {
                        Message = "❌ 缺少預約單編號。";
                        break;
                    }
                    var existing = _reservationService.GetOrderById(NewOrder.OrderId);
                    if (existing != null)
                    {
                        if (existing.Status != OrderStatus.Confirmed)
                        {
                            Message = "⚠️ 僅能對「預約中」的訂單進行狀態更新。";
                            break;
                        }
                        var updated = _reservationService.UpdateOrderStatus(existing.OrderId, NewOrder.Status);
                        Message = updated ? "✅ 預約單狀態已更新。" : "❌ 狀態更新失敗。";
                    }
                    else
                    {
                        Message = "❌ 找不到預約單。";
                    }
                    break;


                case "delete":
                    if (NewOrder.OrderId <= 0)
                    {
                        Message = "❌ 缺少預約單編號。";
                        break;
                    }
                    var target = _reservationService.GetOrderById(NewOrder.OrderId);
                    if (target == null)
                    {
                        Message = "❌ 找不到預約單。";
                        break;
                    }
                    if (target.Status != OrderStatus.Confirmed)
                    {
                        Message = "⚠️ 僅能取消「預約中」的訂單。";
                        break;
                    }
                    var deleted = _reservationService.CancelOrder(NewOrder.OrderId,false);
                    Message = deleted ? "🗑️ 預約單已取消。" : "❌ 取消失敗。";
                    break;


                default:
                    Message = "❌ 未知操作。";
                    break;
            }

            // 套用與 OnGet 一樣的篩選條件，避免資料被清空
            var orders = _reservationService.GetAllOrders();

            if (FilterDesignerId.HasValue)
                orders = orders.Where(o => o.DesignerId == FilterDesignerId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(FilterProductName))
                orders = orders.Where(o => GetProductName(o.ProductId).Contains(FilterProductName)).ToList();

            if (FilterStatus.HasValue)
                orders = orders.Where(o => o.Status == FilterStatus.Value).ToList();

            if (FilterStartDate.HasValue && FilterEndDate.HasValue)
            {
                var start = FilterStartDate.Value.Date;
                var end = FilterEndDate.Value.Date.AddDays(1).AddSeconds(-1);
                orders = orders.Where(o => o.ReservationDateTime >= start && o.ReservationDateTime <= end).ToList();
            }
            else
            {
                var now = DateTime.Now;
                var start = new DateTime(now.Year, now.Month, 1);
                var end = start.AddMonths(1).AddSeconds(-1);
                orders = orders.Where(o => o.ReservationDateTime >= start && o.ReservationDateTime <= end).ToList();
            }

            AllOrders = orders;

            // 可用時段（非必要）
            AvailableSlotDetails = new();
            foreach (var designer in AllDesigners)
            {
                var slots = _reservationService.GetAvailableServiceSlots(
                    designerId: designer.DesignerId,
                    date: DateTime.Today,
                    cooldownMinutes: 10,
                    advanceMinutes: 10);
                AvailableSlotDetails.AddRange(slots);
            }

            return Page();
        }

        public string GetDesignerName(int designerId) =>
            AllDesigners.FirstOrDefault(d => d.DesignerId == designerId)?.Name ?? "未知設計師";

        public string GetProductName(int productId) =>
            AllProducts.FirstOrDefault(p => p.ProductId == productId)?.Name ?? "未知服務";

        public string GetUserName(string uid) =>
    AllUsers.FirstOrDefault(p => p.Id == uid)?.Name ?? "未知客戶";

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
