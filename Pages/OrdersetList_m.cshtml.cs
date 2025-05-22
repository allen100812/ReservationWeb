using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using System.Globalization;
using Web0524.Models;
using System.Diagnostics;
using static Web0524.Pages.OrdersetList_mModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.SqlServer.Server;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace Web0524.Pages
{
    [Authorize]
    public class OrdersetList_mModel : PageModel
    {
        private readonly IOrdersetService _OrdersetService;
        private readonly IProductService _ProductService;
        private readonly IUserService _userService;
        private readonly IPlaceService _placeService;

        public OrdersetList_mModel(IOrdersetService OrdersetService, IProductService ProductService, IUserService userService, IPlaceService placeService)
        {
            _OrdersetService = OrdersetService;
            _ProductService = ProductService;
            _userService = userService;
            _placeService = placeService;
        }
        public List<Models.Orderset> Ordersets { get; set; }
        public List<Models.Product> Products { get; set; }
        public User User_me { get; set; }

        public List<Place> place { get; set; }
        public IActionResult OnGet()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 2)
            {
                return NotFound();
            }
            _OrdersetService.UpdateExpired();
            Ordersets = _OrdersetService.GetOrdersetByKey("Made by MiaoYuWei").ToList();
            Products = _ProductService.GetProductTB().ToList();
            place = _placeService.GetPlaceTB().ToList();
            return Page();
        }
        [HttpPost]
        public IActionResult OnPostSearchOrders([FromForm] string key)
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 2)
            {
                return NotFound();
            }
            // 根據關鍵字 key 搜尋預約資料
            Ordersets = _OrdersetService.GetOrdersetByKey(key).ToList();
            Products = _ProductService.GetProductTB().ToList();
            place = _placeService.GetPlaceTB().ToList();
            return Page();
        }
        public IActionResult OnPost(string handler)
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 2)
            {
                return NotFound();
            }
            if (handler == "CalendarUpdate")
            {
                Ordersets = _OrdersetService.GetOrdersetTB().ToList();
                Products = _ProductService.GetProductTB().ToList();
                place = _placeService.GetPlaceTB().ToList();
                return Page();
            }
            else if (handler == "SetOrderSet")
            {
                string pid = Request.Form["pid"];
                string orderSw = Request.Form["orderSw"];

                if (pid != null && orderSw != null)
                {
                    if (_ProductService.UpdateProductOrderSw(int.Parse(pid), int.Parse(orderSw)))
                    {
                        System.Diagnostics.Debug.Print("成功" + orderSw);
                    }
                    else
                    {
                        System.Diagnostics.Debug.Print("失敗" + orderSw);
                    }
                }

                return RedirectToPage("./OrdersetList_m");
            }
            else
            {
                string Sid = Request.Form["Sid"];
                if (Sid != null)
                {
                    _OrdersetService.DeleteOrderset(Sid);
                }

                return RedirectToPage("OrdersetList_m");
            }
        }

    }
}
