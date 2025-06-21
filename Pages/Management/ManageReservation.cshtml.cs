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



        public void OnGet()
        {
            AllDesigners = _reservationService.GetAllDesigners();
            AllUsers = _userService.GetUserTB().ToList();

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
        }

        [IgnoreAntiforgeryToken]
        public IActionResult OnPost(string action)
        {
            AllDesigners = _reservationService.GetAllDesigners();

            ModelState.Remove("NewOrder.OrderId");
            ModelState.Remove("NewOrder.Status");
            ModelState.Remove("NewOrder.Orderdate");
            // ✅ 僅在「新增」或「更新」時才驗證欄位
            if ((action == "add" || action == "update") && !ModelState.IsValid)
            {
                var errors = string.Join("；", ModelState
                    .Where(e => e.Value.Errors.Count > 0)
                    .Select(e => $"欄位: {e.Key}, 錯誤: {string.Join(", ", e.Value.Errors.Select(er => er.ErrorMessage))}"));
                Message = $"請填寫所有必要欄位：{errors}";
                return Page();
            }


            switch (action)
            {
                case "add":
                    var productRule = AllDesigners
                        .SelectMany(d => d.ScheduleRules)
                        .FirstOrDefault(r => r.ProductId == NewOrder.ProductId);
                    NewOrder.Price = productRule != null ? productRule.DurationMinutes * 10 : 0; // 假設計價邏輯為每分鐘10元
                    NewOrder.Orderdate = DateTime.Now;
                    NewOrder.Status = OrderStatus.Confirmed;
                    var created = _reservationService.CreateOrder(NewOrder);
                    Message = created != null ? $"成功新增預約單：編號 {created.OrderId}" : "新增失敗，此時段已無法預約。";
                    break;

                case "update":
                    var existing = _reservationService.GetOrderById(NewOrder.OrderId);
                    if (existing != null)
                    {
                        existing.DesignerId = NewOrder.DesignerId;
                        existing.ProductId = NewOrder.ProductId;
                        existing.ReservationDateTime = NewOrder.ReservationDateTime;
                        existing.PaymentMethod = NewOrder.PaymentMethod;
                        existing.Price = NewOrder.Price;
                        existing.Remark = NewOrder.Remark;
                        existing.Uid = NewOrder.Uid;
                        var updated = _reservationService.UpdateOrderStatus(existing.OrderId, NewOrder.Status);
                        Message = updated ? "預約單已更新。" : "更新失敗。";
                    }
                    else
                    {
                        Message = "找不到預約單。";
                    }
                    break;

                case "delete":
                    var deleted = _reservationService.CancelOrder(NewOrder.OrderId);
                    Message = deleted ? "預約單已取消。" : "取消失敗。";
                    break;

                default:
                    Message = "未知操作。";
                    break;
            }

            AvailableSlotDetails = new();
            foreach (var designer in AllDesigners)
            {
                var slots = _reservationService.GetAvailableServiceSlots(
                    designerId: designer.DesignerId,
                    date: SelectedDate,
                    cooldownMinutes: 10,
                    advanceMinutes: 10);
                AvailableSlotDetails.AddRange(slots);
            }
            AllOrders = _reservationService.GetAllOrders().Where(o => o.ReservationDateTime.Date == SelectedDate).ToList();

            return Page();
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
