using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class UserListModel : PageModel
    {
        private readonly IUserService _userService;

        public UserListModel(IUserService userService)
        {
            _userService = userService;
        }

        public List<User> Users { get; set; }
        [BindProperty]
        public User User_me { get; set; }
        public string Myname { get; set; }
        public string Myphone { get; set; }
        public string Myemail { get; set; }
        public string Myline { get; set; }

        public List<(string uid, double point)> UserPointTB { get; set; }
        public List<(string uid, string Rank)> UserRandTB { get; set; }
        public List<UserPointStr> userPointStr { get; set; }
        public IActionResult OnGet()
        {
            User_me= _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if(User_me == null || int.Parse(User_me.UserType) > 2 )
            {
                return NotFound();
            }
            Users = _userService.GetUserTB().ToList();
            UserPointTB = _userService.GetUserPointTB().ToList();
            userPointStr = _userService.GetUserPointStr();
            UserRandTB = _userService.GetUserRank().ToList();

            return Page();
        }
        public IActionResult OnPost()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            string userId = Request.Form["userId"];
            if(userId != null)
            {
                _userService.DeleteUser(userId);
            }
            

            return RedirectToPage("UserList");
        }
    }
}
