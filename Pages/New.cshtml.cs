using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    public class NewModel : PageModel
    {
        private readonly INewService _newService;
        private readonly IUserService _userService;

        public NewModel(INewService newService, IUserService userService)
        {
            _newService = newService;
            _userService = userService;
        }
        public List<NewList> newLists { get; set; }
        public User User_me { get; set; }
        public void OnGet()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            newLists =_newService.GetNewTB().ToList(); 
        }
    }
}
