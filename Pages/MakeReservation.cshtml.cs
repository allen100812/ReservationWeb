using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    public class MakeReservationModel : PageModel
    {
        private readonly IReservationService _reservationService;
        private readonly IUserService _userService;
        private readonly IProductService _productService;
        private readonly IMarketingService _marketingService;
        
        public MakeReservationModel(
            IReservationService reservationService,
            IUserService userService,
            IProductService productService, IMarketingService marketingService)
        {
            _reservationService = reservationService;
            _userService = userService;
            _productService = productService;
            _marketingService = marketingService;
        }


        [BindProperty]
        public int? SelectedCouponRecordId { get; set; } // 綁定前端選擇的 RecordId

        public List<CouponDispatchRecord> AvailableCoupons { get; set; } = new();


        [BindProperty]
        public Order NewOrder { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public List<Designer> AllDesigners { get; set; } = new();
        public List<Product> AllProducts { get; set; } = new();

         
        public List<Coupon> AllCoupons { get; set; } = new();
        public List<DateTime> AvailableTimeSlots { get; set; } = new();

        public int CurrentUserId { get; set; }
        public string Message { get; set; } = "";

        public void OnGet()
        {
            LoadData();
        }

        public IActionResult OnPost(string action)
        {
            LoadData();

            if (action == "add")
            {

                // 嘗試從登入資訊取得使用者 ID
                var uidStr = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;


                if (string.IsNullOrEmpty(uidStr))
                {
                    // 未登入時導向登入頁
                    Response.Redirect("/Account/Login"); // 可依你的實際登入路徑調整
                    return Page();
                }

                // 驗證資料
                if (NewOrder.DesignerId <= 0 || NewOrder.ProductId <= 0 || NewOrder.Uid != "" || NewOrder.ReservationDateTime == default)
                {
                    if (NewOrder.DesignerId <= 0)
                    {
                        Message = "請選擇設計師。";
                        return Page();
                    }

                    if (NewOrder.ProductId <= 0)
                    {
                        Message = "請選擇服務項目。";
                        return Page();
                    }

                    if (string.IsNullOrWhiteSpace(NewOrder.Uid))
                    {
                        Message = "使用者資訊錯誤，請重新登入。";
                        return Page();
                    }

                    if (NewOrder.ReservationDateTime == default)
                    {
                        Message = "請選擇預約時間。";
                        return Page();
                    }

                }

                var Product = AllProducts.FirstOrDefault(x => x.ProductId == NewOrder.ProductId);
                // 設定其他必要欄位
                NewOrder.Status = 0; // 預設狀態：未處理
                NewOrder.Orderdate = DateTime.Now;
                NewOrder.Price = Product.Price;  // 如有定價邏輯，可補上
                NewOrder.Uid = uidStr;
                // 呼叫服務層建立訂單（使用你的版本）
                var createdOrder = _reservationService.CreateOrder(NewOrder);
                if (createdOrder != null)
                {
                    Message = $"預約成功！訂單編號：{createdOrder.OrderId}";

                    // ✅ 若有選擇優惠券，直接套用
                    if (SelectedCouponRecordId.HasValue)
                    {
                        var resultMsg = _marketingService.ApplyCouponByRecordId(SelectedCouponRecordId.Value, createdOrder);
                        Message += $"<br>{resultMsg}";
                    }
                }
                else
                {
                    Message = "該時段已被預約，請選擇其他時間。";
                }
            }

            return Page();
        }

        private void LoadData()
        {
            AllDesigners = _reservationService.GetAllDesigners().ToList();
            AllProducts = _productService.GetAllProducts().ToList();
            AllCoupons = _marketingService.GetAllCoupons().ToList();
            // 嘗試從登入資訊取得使用者 ID
            var uidStr = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;

            if (string.IsNullOrEmpty(uidStr))
            {
                // 未登入時導向登入頁
                Response.Redirect("/Account/Login"); // 可依你的實際登入路徑調整
                return;
            }

            AvailableCoupons = _marketingService.GetAvailableCouponRecords(uidStr);
            AvailableTimeSlots = GenerateAvailableTimeSlots(SelectedDate);
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnGetGetAvailableTimeSlots(int designerId, int productId, DateTime date)
        {
            var times = GenerateAvailableTimeSlots(date);
            var available = times
                .Where(t => _reservationService.IsSlotAvailable(designerId, productId, t))
                .Select(t => new { time = t.ToString("yyyy-MM-ddTHH:mm"), label = t.ToString("HH:mm") })
                .ToList();

            return new JsonResult(available);
        }


        private List<DateTime> GenerateAvailableTimeSlots(DateTime date)
        {
            var slots = new List<DateTime>();
            var start = date.Date.AddHours(9);
            var end = date.Date.AddHours(18);

            for (var time = start; time < end; time = time.AddMinutes(30))
            {
                slots.Add(time);
            }

            return slots;
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnGetGetProducts(int designerId)
        {
            var designer = _reservationService.GetDesignerById(designerId);
            if (designer == null)
                return new JsonResult(new { success = false });

            var productIds = designer.ScheduleRules.Select(r => r.ProductId).Distinct().ToList();
            var validProducts = _productService.GetAllProducts()
                .Where(p => productIds.Contains(p.ProductId))
                .Select(p => new { productId = p.ProductId, name = p.Name })
                .ToList();

            return new JsonResult(validProducts);
        }

    }
}
