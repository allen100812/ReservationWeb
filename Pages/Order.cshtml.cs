using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace Web0524.Pages
{
    [Authorize]
    public class OrderModel : PageModel
    {
        private readonly IOrdersetService _OrdersetService;
        private readonly IProductService _ProductService;
        private readonly IUserService _userService;
        private readonly IPlaceService _placeService;
        public List<Place> place { get; set; }

        public DateTime TaipeiTime { get; set; }

        public OrderModel(IOrdersetService OrdersetService, IProductService ProductService, IUserService userService, IPlaceService placeService)
        {
            _OrdersetService = OrdersetService;
            _ProductService = ProductService;
            _userService = userService;
            _placeService = placeService;
        }
        public List<Models.Orderset> Ordersets { get; set; }
        public User User_me { get; set; }


        public void OnGet()
        {
            var twtzinfo = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            TaipeiTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, twtzinfo);
            _OrdersetService.UpdateExpired();
            Ordersets = _OrdersetService.GetOrdersetByUid(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value).ToList();
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            place = _placeService.GetPlaceTB().ToList();
        }
        public IActionResult OnPostCheckSJ(string sid)
        {

            Ordersets = _OrdersetService.GetOrdersetByUid(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value).ToList();
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            return RedirectToPage("OrderEdit", new { sid = sid });
        }


    }
}
