using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Web0524.Models;

namespace Web0524.Pages
{
    public class ProductListModel : PageModel
    {
        private readonly IProductService _ProductService;
        private readonly IUserService _UserService;
        private readonly IOrdersetService _OrdersetService;
        private readonly IPgroupService _pgroupService;
        public ProductListModel(IProductService ProductService, IUserService userService, IOrdersetService ordersetService, IPgroupService pgroupService)
        {
            _ProductService = ProductService;
            _UserService = userService;
            _OrdersetService = ordersetService;
            _pgroupService = pgroupService;
        }
        public List<Models.Pgroup> Pgroups { get; set; }
        public List<Models.Product> Products { get; set; }
        public User Userme { get; set; }

        public void OnGet()
        {
            Pgroups=_pgroupService.GetPgroupTB().ToList();
            Products = _ProductService.GetProductTB_PlaceNotNull().ToList();

            if (User.FindFirst(ClaimTypes.Sid) != null)
            {
                    Userme = _UserService.GetUserById(User.FindFirst(ClaimTypes.Sid).Value.ToString());
                    ViewData["Uid"] = Userme.Id;
            }


        }
        public IActionResult OnPost(string? pgid)
        {
            if (pgid == null)
            {
                return NotFound();
            }
            Pgroups = _pgroupService.GetPgroupTB().ToList();
            Products = _ProductService.GetProductByPgid(pgid).ToList();
            //通知客戶尚未寫
            return Page();
        }

    }
}
