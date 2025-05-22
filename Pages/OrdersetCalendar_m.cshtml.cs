using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using Web0524.Models;
using System.Diagnostics;
using static Web0524.Pages.OrdersetCalendar_mModel;
using Microsoft.AspNetCore.Authorization;
using Hangfire.Server;

namespace Web0524.Pages
{
    [Authorize]
    public class OrdersetCalendar_mModel : PageModel
    {
        private readonly IOrdersetService _OrdersetService;
        private readonly IProductService _ProductService;
        private readonly IUserService _userService;

        public OrdersetCalendar_mModel(IOrdersetService OrdersetService, IProductService ProductService, IUserService userService)
        {
            _OrdersetService = OrdersetService;
            _ProductService = ProductService;
            _userService = userService;
        }
        public List<Models.Orderset> Ordersets { get; set; }
        public List<Models.Product> Products { get; set; }
        public User User_me { get; set; }
        public List<(string uid, double point)> UserPointTB { get; set; }

        public IActionResult OnGetCalendarEvents()
        {
            Ordersets = _OrdersetService.GetOrdersetTB().ToList();

            var calendarEvents = Ordersets.Select(o =>
            {
                DateTime combinedDateTime = o.Date.Value.Date + o.Time.Value;
                DateTime combinedDateTime_e = combinedDateTime.AddHours(o.SpendTime);
                string formattedDateTime_s = combinedDateTime.ToString("yyyy-MM-dd HH:mm");
                string formattedDateTime_e = combinedDateTime_e.ToString("yyyy-MM-dd HH:mm");
                string eventstr = "無";
                switch (o.Event)
                {       
                    case "0":
                        eventstr  ="待接受";
                        break;
                    case "1":
                        eventstr  ="已接受";
                        break;
                    case "9":
                        eventstr  ="已取消";
                        break;
                    case "10":
                        eventstr  ="未接受";
                        break;
                    case "11":
                        eventstr  ="已完成";
                        break;
                    case "19":
                        eventstr  ="已取消";
                        break;
                }

  
                return new
                {
                    title = combinedDateTime.ToString("HH:mm") + " " + eventstr,
                    start = formattedDateTime_s,
                    end = formattedDateTime_e,
                    time = o.Time,
                    sid = o.Sid,
                    pname = o.Pname,
                    oevent=o.Event,
                };
            });

            return new JsonResult(calendarEvents);
        }

        public IActionResult OnGet()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 2)
            {
                return NotFound();
            }
            _OrdersetService.UpdateExpired();
            Ordersets = _OrdersetService.GetOrdersetTB().ToList();
            Products = _ProductService.GetProductTB().ToList();
            UserPointTB = _userService.GetUserPointTB().ToList();
            return Page();
        }
        public IActionResult CheckSJ()
        {
            //User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            //if (User_me == null || int.Parse(User_me.UserType) > 2)
            //{
            //    return NotFound();
            //}
            //Ordersets = _OrdersetService.GetOrdersetTB().ToList();
            //Products = _ProductService.GetProductTB().ToList();
            // UserPointTB=_userService.GetUserPointTB().ToList();
            //return RedirectToPage("OrdersetEdit", new { sid = sid });
            System.Diagnostics.Debug.Print("123");
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
                UserPointTB = _userService.GetUserPointTB().ToList();
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
