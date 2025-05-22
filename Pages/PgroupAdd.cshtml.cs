using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class PgroupAddModel : PageModel
    {
        private readonly IPgroupService _PgroupService;
        private readonly IProductService _ProductService;
        private readonly IUserService _userService;

        public PgroupAddModel(IPgroupService PgroupService, IProductService productService, IUserService userService)
        {
            _PgroupService = PgroupService;
            _ProductService = productService;
            _userService = userService;
        }

        [BindProperty]
        public Models.User User_me { get; set; }
        [BindProperty]
        public Pgroup Pgroup { get; set; }

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
            var validationContext = new ValidationContext(Pgroup);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(Pgroup, validationContext, validationResults, true);

            if (!isValid)
            {
                return NotFound();
            }

            bool AddPgroup = _PgroupService.AddPgroup(Pgroup);

            if (AddPgroup == true)
            {
                //處理產品批次分類
                if (SelectedProductIds.Count > 0)
                {
                    foreach (int Pid in SelectedProductIds)
                    {
                        _ProductService.UpdateProductGroup(Pid, (int)Pgroup.Pgid);
                    }
                }
                TempData["add_success"] = true;
                return RedirectToPage("PgroupList_m");
            }
            else
            {
                TempData["add_error"] = true;
                return RedirectToPage("PgroupAdd");
            }
        }
    }
}
