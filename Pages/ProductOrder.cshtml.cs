using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class ProductOrderModel : PageModel
    {
        private readonly IOrdersetService _OrdersetService;

        public ProductOrderModel(IOrdersetService OrdersetService)
        {
            _OrdersetService = OrdersetService;
        }

        [BindProperty]
        public List<Models.Orderset> Ordersets { get; set; }


        public IActionResult OnGet(String? Pid)
        {
            if (Pid == null)
            {
                return NotFound();
            }

            Ordersets = _OrdersetService.GetOrdersetByPid(Pid).ToList();

            if (Ordersets == null)
            {
                return NotFound();
            }
            return Page();
        }

    }
}
