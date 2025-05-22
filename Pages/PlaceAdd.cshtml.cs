using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class PlaceAddModel : PageModel
    {
        private readonly IPlaceService _PlaceService;
        private readonly IProductService _ProductService;
        private readonly IUserService _userService;

        public PlaceAddModel(IPlaceService PlaceService, IProductService productService, IUserService userService)
        {
            _PlaceService = PlaceService;
            _ProductService = productService;
            _userService = userService;
        }

        [BindProperty]
        public Models.User User_me { get; set; }
        [BindProperty]
        public Place Place { get; set; }

        public List<Models.Product> Products { get; set; }
        public IActionResult OnGet()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            Products = _ProductService.GetProductTB().ToList();
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
            Place.Placepid = Placepids;
            if (Place.Placeorder == null)
            {
                Place.Placeorder ="";
            }
            // ÅçÃÒ User ª«¥ó
            var validationContext = new ValidationContext(Place);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(Place, validationContext, validationResults, true);

            if (!isValid)
            {
                return NotFound();
            }

            bool AddPlace = _PlaceService.AddPlace(Place);

            if (AddPlace == true)
            {
                TempData["add_success"] = true;
                return RedirectToPage("PlaceList_m");
            }
            else
            {
                TempData["add_error"] = true;
                return RedirectToPage("PlaceAdd");
            }
        }
    }
}
