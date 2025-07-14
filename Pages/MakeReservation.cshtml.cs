using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using Web0524.Models;
using Web0524.Models.Marketing;
using System.Diagnostics;

namespace Web0524.Pages
{
    public class MakeReservationModel : PageModel
    {
        private readonly IReservationService _reservationService;
        private readonly IUserService _userService;
        
            private readonly IPgroupService _pgroupService;
        private readonly IProductService _productService;
        private readonly IMarketingService _marketingService;

        public MakeReservationModel(
            IReservationService reservationService,
            IUserService userService,
            IProductService productService,
            IMarketingService marketingService,
            IPgroupService pgroupService)
        {
            _reservationService = reservationService;
            _userService = userService;
            _productService = productService;
            _marketingService = marketingService;
            _pgroupService = pgroupService;
        }

        [BindProperty]
        public int? SelectedCouponRecordId { get; set; }

        public List<CouponDispatchRecord> AvailableCoupons { get; set; } = new();
        public List<Coupon> AllCoupons { get; set; } = new();
        public List<Designer> AllDesigners { get; set; } = new();

        public List<Pgroup> AllPgroup { get; set; } = new();
        public List<Product> AllProducts { get; set; } = new();
        public List<DateTime> AvailableTimeSlots { get; set; } = new();

        [BindProperty]
        public Order NewOrder { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public int CurrentUserId { get; set; }
        public string Message { get; set; } = "";

        public void OnGet()
        {
            LoadData();
        }

        public IActionResult OnPost(string action)
        {
            Console.WriteLine("R1");
            LoadData();
            Console.WriteLine("R2");
            if (action == "add")
            {
                Console.WriteLine("R3");
                var uidStr = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;

                if (string.IsNullOrEmpty(uidStr))
                {
                    return RedirectToPage("/Account/Login");
                }
                Console.WriteLine("R4");
                if (NewOrder.DesignerId <= 0 || NewOrder.ProductId <= 0 || NewOrder.ReservationDateTime == default)
                {
                    Message = "請確認所有欄位皆已填寫。";
                    return Page();
                }
                Console.WriteLine("R5");
                if (string.IsNullOrWhiteSpace(uidStr))
                {
                    Message = "使用者驗證失敗，請重新登入。";
                    return Page();
                }
                Console.WriteLine("R6");
                var product = AllProducts.FirstOrDefault(x => x.ProductId == NewOrder.ProductId);
                NewOrder.Status = OrderStatus.Confirmed;
                NewOrder.Orderdate = DateTime.Now;
                NewOrder.Price = product?.Price ?? 0;
                NewOrder.Uid = uidStr;

                var createdOrder = _reservationService.CreateOrder(NewOrder);
                Console.WriteLine("R7");
                if (createdOrder != null)
                {
                    Console.WriteLine("R8");
                    var designer = AllDesigners.FirstOrDefault(x => x.DesignerId == NewOrder.DesignerId);
                    var coupon = AllCoupons.FirstOrDefault(c => c.CouponId == SelectedCouponRecordId);

                    string formattedOrderId = createdOrder.OrderId.ToString("D4");
                    string formattedDate = NewOrder.ReservationDateTime.ToString("yyyy/MM/dd");
                    string formattedTime = NewOrder.ReservationDateTime.ToString("HH:mm");

                    Message = $@"預約成功！<br>
        訂單編號：{formattedOrderId}<br>
        日期：{formattedDate}<br>
        時間：{formattedTime}<br>
        設計師：{designer?.Name}<br>
        項目：{product?.Name}<br>
        金額：${NewOrder.Price:0}<br>";

                    if (SelectedCouponRecordId.HasValue)
                    {
                        Console.WriteLine("R9");
                        var resultMsg = _marketingService.ApplyCouponByRecordId(SelectedCouponRecordId.Value, createdOrder);
                        Message += $"優惠券：{coupon?.Title ?? "已套用"}<br>{resultMsg}";
                    }

                    //1.Line通知



                }
                else
                {
                    Message = "該時段已被預約，請重新選擇。";
                }

            }
            Console.WriteLine("R10");
            return Page();
        }

        private void LoadData()
        {
            AllDesigners = _reservationService.GetAllDesigners().ToList();
            AllPgroup = _pgroupService.GetAllPgroups().ToList();
            AllProducts = _productService.GetAllProducts().ToList();
            AllCoupons = _marketingService.GetAllCoupons().ToList();

            var uidStr = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(uidStr))
            {
                Response.Redirect("/Account/Login");
                return;
            }

            AvailableCoupons = _marketingService.GetAvailableCouponRecords(uidStr);
            AvailableTimeSlots = GenerateAvailableTimeSlots(SelectedDate);
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnGetGetAvailableTimeSlots(int designerId, int productId)
        {
            var today = DateTime.Today;
            var endDate = today.AddMonths(1).AddDays(-1);
            var result = new List<object>();

            for (var date = today; date <= endDate; date = date.AddDays(1))
            {
                var slots = GenerateAvailableTimeSlots(date)
                    .Where(t => _reservationService.IsSlotAvailable(designerId, productId, t))
                    .ToList();

                if (slots.Any())
                {
                    result.Add(new
                    {
                        title = $"可預約 {slots.Count} 筆",
                        start = date.ToString("yyyy-MM-dd"),
                        allDay = true,
                        extendedProps = new
                        {
                            slots = slots.Select(t => new
                            {
                                time = t.ToString("yyyy-MM-ddTHH:mm"),
                                label = t.ToString("HH:mm")
                            })
                        }
                    });
                }
            }

            return new JsonResult(result);
        }

        private List<DateTime> GenerateAvailableTimeSlots(DateTime date)
        {
            var list = new List<DateTime>();
            var start = date.Date.AddHours(9);
            var end = date.Date.AddHours(18);

            for (var t = start; t < end; t = t.AddMinutes(30))
                list.Add(t);

            return list;
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnGetGetProducts(int designerId)
        {
            var designer = _reservationService.GetDesignerById(designerId);
            if (designer == null)
                return new JsonResult(new { success = false });

            var productIds = designer.ScheduleRules.Select(r => r.ProductId).Distinct().ToList();

            var products = _productService.GetAllProducts()
                .Where(p => productIds.Contains(p.ProductId))
                .Select(p => new { productId = p.ProductId, name = p.Name })
                .ToList();

            return new JsonResult(products);
        }
        public IActionResult OnGetImage(int id)
        {
            var product = _productService.GetAllProducts().FirstOrDefault(p => p.ProductId == id);
            if (product == null || product.Photo == null)
                return NotFound();

            return File(product.Photo, "image/jpeg"); // 也可以是 image/png 看你的圖片格式
        }



    }
}
