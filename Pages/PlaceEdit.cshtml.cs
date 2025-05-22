using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class PlaceEditModel : PageModel
    {
        private readonly IPlaceService _PlaceService;
        private readonly IProductService _ProductService;
        private readonly IUserService _userService;

        public PlaceEditModel(IPlaceService PlaceService, IProductService productService, IUserService userService)
        {
            _PlaceService = PlaceService;
            _ProductService = productService;
            _userService = userService;
        }

        [BindProperty]
        public Models.User User_me { get; set; }
        [BindProperty]
        public Place Place { get; set; }
        // Assuming you have a list of products with id and name
        public List<Models.Product> Products { get; set; }

        public IActionResult OnGet(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            Place = _PlaceService.GetPlaceById(id);
            Products = _ProductService.GetProductTB().ToList();
            if (Place == null || Products == null)
            {
                return NotFound();
            }
            return Page();
        }

        [HttpPost]
        public IActionResult OnPost(List<int> SelectedProductIds)
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            string Placepids = string.Join(",", SelectedProductIds);
            Place.Placepid= Placepids;
            if (Place.Placeorder == null)
            {
                Place.Placeorder = "";
            }
            System.Diagnostics.Debug.Print("sw"+Place.PlaceSw);
            // ÅçÃÒ User ª«¥ó
            var validationContext = new ValidationContext(Place);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(Place, validationContext, validationResults, true);

            if (!isValid)
            {
                return NotFound();
            }


            bool EditPlace = _PlaceService.UpdatePlace(Place);

            if (EditPlace == true)
            {
                TempData["edit_success"] = true;
                return RedirectToPage("PlaceList_m");
            }
            else
            {
                TempData["edit_error"] = true;
                Products = _ProductService.GetProductTB().ToList();
                return Page();
            }
        }
    }
}
