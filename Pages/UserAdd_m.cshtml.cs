using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace Web0524.Pages
{
    [Authorize]
    public class UserAdd_mModel : PageModel
    {
        private readonly IUserService _userService;

        public UserAdd_mModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public User user { get; set; }
        public User User_me { get; set; }
        public IActionResult OnGet()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 2)
            {
                return NotFound();
            }
            TempData.Clear();
            return Page();
        }

        public IActionResult OnPost()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 2)
            {
                return NotFound();
            }
            // ÅçÃÒ User ª«¥ó
            var validationContext = new ValidationContext(user);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(user, validationContext, validationResults, true);

            if (!isValid)
            {
                return NotFound();
            }
            bool AddUser = _userService.AddUser(user);

            if (AddUser == true)
            {
                TempData["add_success"] = true;
                return RedirectToPage("UserList");
            }
            else
            {
                TempData["add_error"] = true;
                return Page();
            }

        }

    }
}
