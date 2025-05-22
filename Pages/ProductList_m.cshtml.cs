using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using System.Numerics;
using System.Xml.Linq;

namespace Web0524.Pages
{
    [Authorize]
    public class ProductList_mModel : PageModel
    {
        private readonly IProductService _ProductService;
        private readonly IUserService _userService;
        private readonly IPgroupService _pgroupService;

        public ProductList_mModel(IProductService ProductService, IUserService userService, IPgroupService pgroupService)
        {
            _ProductService = ProductService;
            _userService = userService;
            _pgroupService = pgroupService;
        }
        public List<Models.Pgroup> Pgroups { get; set; }
        public List<Models.Product> Products { get; set; }
        public User User_me { get; set; }

        public string Myname { get; set; }
        public string Myphone { get; set; }
        public string Myemail { get; set; }
        public string Myline { get; set; }
        public IActionResult OnGet()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);

            if (User_me == null || int.Parse(User_me.UserType) > 2)
            {
                return NotFound();
            }
            Pgroups = _pgroupService.GetPgroupTB().ToList();
            Products = _ProductService.GetProductTB().ToList();
            return Page();
        }
        public IActionResult OnPostSetOrderSet()
        {
            
            string pid = Request.Form["pid"];
            string orderSw = Request.Form["orderSw"];
            if (pid != null && orderSw != null)
            {
                _ProductService.UpdateProductOrderSw(int.Parse(pid), int.Parse(orderSw));
            }
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            Pgroups = _pgroupService.GetPgroupTB().ToList();
            Products = _ProductService.GetProductTB().ToList();
            return Page();
        }
        public IActionResult OnPost()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            string productId = Request.Form["productId"];
            if (productId != null)
            {

                    _ProductService.DeleteProduct(productId);
                    TempData["del_success"] = true;

            }

            return RedirectToPage("ProductList_m");
        }

    }
}
