using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    public class ProductModel : PageModel
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IPortfolioGroupService _groupService;

        public List<Portfolio> Portfolios { get; set; } = new();
        public List<PortfolioGroup> Groups { get; set; } = new();

        public ProductModel(IPortfolioService portfolioService, IPortfolioGroupService groupService)
        {
            _portfolioService = portfolioService;
            _groupService = groupService;
        }

        public async Task OnGetAsync()
        {
            Portfolios = (await _portfolioService.GetPublishedAsync()).ToList();
            Groups = (await _groupService.GetAllAsync()).ToList();
        }
    }
}
