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
        public int? SelectedDesignerId { get; set; }

        public List<Designer> AllDesigners { get; set; } = new();
        public Designer? SelectedDesigner { get; set; }
        public List<Designer_Shift> DayOffList { get; set; } = new();

        public void OnGet()
        {
            AllDesigners = _reservationService.GetAllDesigners();

        }

        public void OnPost()
        {
            AllDesigners = _reservationService.GetAllDesigners();

            if (SelectedDesignerId.HasValue)
            {
                SelectedDesigner = _reservationService.GetDesignerById(SelectedDesignerId.Value);

                if (SelectedDesigner != null)
                {
                    SelectedDesigner.FixedHolidays = _reservationService.GetFixedHolidays(SelectedDesignerId.Value);
                    DayOffList = _reservationService.GetShiftsByDesignerId(SelectedDesignerId.Value);
                }
                else
                {
                    Console.WriteLine("❌ 找不到設計師 ID: " + SelectedDesignerId.Value);
                }
            }
        }


        public IActionResult OnPostUpdateFixedHolidays(int DesignerId, List<string> FixedHolidays)
        {
            Console.WriteLine($"▶ 更新固定休假 DesignerId={DesignerId}, Days={string.Join(",", FixedHolidays)}");

            bool success = _reservationService.SetFixedHolidays(DesignerId, FixedHolidays);

            TempData["Message"] = success ? "✅ 固定休假已更新" : "❌ 固定休假更新失敗";

            return RedirectToPage(new { SelectedDesignerId = DesignerId });
        }


        public IActionResult OnPostAddDayOff(int DesignerId, DateTime ShiftDate)
        {
            var shift = new Designer_Shift
            {
                DesignerId = DesignerId,
                ShiftDate = ShiftDate.Date,
                IsDayOff = true
            };
            _reservationService.AddShift(shift);
            return RedirectToPage();
        }

        public IActionResult OnPostRemoveDayOff(int DesignerId, DateTime ShiftDate)
        {
            _reservationService.RemoveShift(DesignerId, ShiftDate.Date);
            return RedirectToPage();
        }
    }
}
