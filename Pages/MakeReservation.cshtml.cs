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

        public MakeReservationModel(
            IReservationService reservationService,
            IUserService userService,
            IProductService productService)
        {
            _reservationService = reservationService;
            _userService = userService;
            _productService = productService;
        }

        [BindProperty]
        public Order NewOrder { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public List<Designer> AllDesigners { get; set; } = new();
        public List<Product> AllProducts { get; set; } = new();
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
                // 驗證資料
                if (NewOrder.DesignerId <= 0 || NewOrder.ProductId <= 0 || NewOrder.Uid != "" || NewOrder.ReservationDateTime == default)
                {
                    Message = "請完整填寫所有欄位。";
                    return Page();
                }

                // 設定其他必要欄位
                NewOrder.Status = 0; // 預設狀態：未處理
                NewOrder.Orderdate = DateTime.Now;
                NewOrder.Price = 0;  // 如有定價邏輯，可補上

                // 呼叫服務層建立訂單（使用你的版本）
                var createdOrder = _reservationService.CreateOrder(NewOrder);

                if (createdOrder != null)
                {
                    Message = $"預約成功！訂單編號：{createdOrder.OrderId}";
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

            // 嘗試從登入資訊取得使用者 ID
            var uidStr = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;

            if (string.IsNullOrEmpty(uidStr))
            {
                // 未登入時導向登入頁
                Response.Redirect("/Account/Login"); // 可依你的實際登入路徑調整
                return;
            }


            AvailableTimeSlots = GenerateAvailableTimeSlots(SelectedDate);
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
    }
}
