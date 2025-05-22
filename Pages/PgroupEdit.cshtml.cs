using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class PgroupEditModel : PageModel
    {
        private readonly IPgroupService _PgroupService;
        private readonly IProductService _ProductService;
        private readonly IUserService _userService;

        public PgroupEditModel(IPgroupService PgroupService, IProductService productService, IUserService userService)
        {
            _PgroupService = PgroupService;
            _ProductService = productService;
            _userService = userService;
        }

        [BindProperty]
        public Models.User User_me { get; set; }
        [BindProperty]
        public Pgroup Pgroup { get; set; }
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
            Pgroup = _PgroupService.GetPgroupById(id);
            Products = _ProductService.GetProductTB().ToList();
            if (Pgroup == null || Products == null)
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


                // 驗證 User 物件
            var validationContext = new ValidationContext(Pgroup);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(Pgroup, validationContext, validationResults, true);

            if (!isValid)
            {
                return NotFound();
            }

            bool EditPgroup = _PgroupService.UpdatePgroup(Pgroup);


            if (EditPgroup == true)
            {
                //處理產品批次分類
                if (SelectedProductIds.Count > 0)
                {
                    foreach (int Pid in SelectedProductIds)
                    {
                        _ProductService.UpdateProductGroup(Pid, (int)Pgroup.Pgid);
                    }
                }
                TempData["edit_success"] = true;
                return RedirectToPage("PgroupList_m");
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
