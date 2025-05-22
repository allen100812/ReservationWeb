using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages
{
    public class ReportModel : PageModel
    {
        private readonly IYearReportService _yearReportService;
        public ReportModel(IYearReportService yearReportService)
        {
            _yearReportService = yearReportService;
        }
        public List<YearReport> YearReports { get; set; }

        public void OnGet()
        {
            YearReports = _yearReportService.GetYearReport("2023").ToList();
        }
    }
}
