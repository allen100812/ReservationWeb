using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class DesignerShiftManagementModel : PageModel
    {
        private readonly IReservationService _reservationService;

        public DesignerShiftManagementModel(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [BindProperty]
        public Designer_Shift NewShift { get; set; } = new();

        public List<Designer> AllDesigners { get; set; } = new();
        public List<Designer_Shift> Shifts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime SelectedDate { get; set; } = DateTime.Today;

        public string Message { get; set; } = string.Empty;

        public void OnGet()
        {
            AllDesigners = _reservationService.GetAllDesigners();
            Shifts = _reservationService.GetShiftsForDay(SelectedDate);
        }

        public IActionResult OnPost(string action)
        {
            AllDesigners = _reservationService.GetAllDesigners();

            if (action == "add")
            {
                var result = _reservationService.AddShift(NewShift);
                Message = result != null ? "新增成功" : "此設計師當天已存在排班資料";
            }
            else if (action == "remove")
            {
                var designerId = int.Parse(Request.Form["DesignerId"]);
                var shiftDate = DateTime.Parse(Request.Form["ShiftDate"]);
                bool success = _reservationService.RemoveShift(designerId, shiftDate);
                Message = success ? "已移除排班" : "移除失敗";
            }

            Shifts = _reservationService.GetShiftsForDay(SelectedDate);
            return Page();
        }
    }
}
