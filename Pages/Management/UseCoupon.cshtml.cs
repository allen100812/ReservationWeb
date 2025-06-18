using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Web0524.Models;

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





            try
            {
                // 1. 建立 Coupon 物件（模擬資料）
                var coupon = new Coupon
                {
                    Title = "測試券",
                    Code = "TEST001",
                    DiscountType = DiscountTypeEnum.FixedAmount,
                    DiscountAmount = 0,
                    FixedDiscountAmount = 50,
                    MinAmount = 100,
                    ValidFrom = DateTime.Now.AddDays(-1),
                    ValidTo = DateTime.Now.AddDays(7),
                    IsActive = true,
                    AutoAssign = false,
                    AutoAssignRule = AutoAssignRuleEnum.None
                };

                // 新增優惠券
                _marketingService.AddSystemCoupon(coupon);
                var allCoupons = _marketingService.GetAllCoupons();
                var addedCoupon = allCoupons.FirstOrDefault(c => c.Code == "TEST001");
                if (addedCoupon == null)
                    throw new Exception("新增優惠券失敗");

                // 2. 派發給測試會員
                string memberId = "test-member";
                _marketingService.AssignCouponToMember(memberId, addedCoupon.CouponId);

                var record = _marketingService.GetAvailableCouponRecords(memberId)
                    .FirstOrDefault(r => r.CouponId == addedCoupon.CouponId);
                if (record == null)
                    throw new Exception("找不到派發記錄");

                // 3. 建立訂單（模擬）
                var order = new Order
                {
                    OrderId = 123,
                    DesignerId = 1,
                    ProductId = 1,
                    Price = 150,
                    PaymentMethod = OrderPaymentMethod.Cash,
                    Status = OrderStatus.Pending
                };

                // 4. 產生條碼並套用
                var qrCode = _marketingService.GenerateCouponQRCode(record.RecordId);
                var result = _marketingService.ApplyCouponToOrder(qrCode, order);

                TestResult = result;
                Console.WriteLine("✅ 測試成功：" + result);
            }
            catch (Exception ex)
            {
                TestResult = "❌ 錯誤：" + ex.Message;
                Console.WriteLine("❌ 測試失敗：" + ex.ToString());
            }





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
