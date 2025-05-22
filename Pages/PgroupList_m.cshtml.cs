using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{

    [Authorize]
    public class PgroupList_mModel : PageModel
    {
        private readonly IProductService _ProductService;
        private readonly IPgroupService _PgroupService;
        private readonly IUserService _userService;
        public PgroupList_mModel(IProductService ProductService, IPgroupService PgroupService, IUserService userService)
        {
            _ProductService = ProductService;
            _PgroupService = PgroupService;
            _userService = userService;
        }
        public List<Models.Product> Products { get; set; }
        public List<Models.Pgroup> Pgroups { get; set; }
        public Models.User User_me { get; set; }
        public IActionResult OnGet()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 2)
            {
                return NotFound();
            }
            Products = _ProductService.GetProductTB().ToList();
            Pgroups = _PgroupService.GetPgroupTB().ToList();

            return Page();
        }
        public IActionResult OnPost()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            string PgroupId = Request.Form["PgId"];
            if (PgroupId != null)
            {
                _PgroupService.DeletePgroup(PgroupId);
                TempData["del_success"] = true;
            }
            return RedirectToPage("PgroupList_m");
        }
    }
}
