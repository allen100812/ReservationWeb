using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class PlaceList_mModel : PageModel
    {
        private readonly IProductService _ProductService;
        private readonly IPlaceService _placeService;
        private readonly IUserService _userService;
        public PlaceList_mModel(IProductService ProductService,IPlaceService placeService, IUserService userService)
        {
            _ProductService = ProductService;
            _placeService = placeService;
            _userService = userService;
        }
        public List<Models.Product> Products { get; set; }
        public List<Models.Place> Places { get; set; }
        public Models.User User_me { get; set; }
        public IActionResult OnGet()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 2)
            {
                return NotFound();
            }
            Products = _ProductService.GetProductTB().ToList();
            Places = _placeService.GetPlaceTB().ToList();
 
            return Page();
        }
        public IActionResult OnPost()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            string placeId = Request.Form["placeId"];
            if (placeId != null)
            {
                _placeService.DeletePlace(placeId);
                TempData["del_success"] = true;
            }
            return RedirectToPage("PlaceList_m");
        }
    }
}
