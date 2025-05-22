using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel.DataAnnotations;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    
    public class BaseDataEditModel : PageModel
    {

        private readonly IMyService _myService;
        private readonly IMemoryCache _memoryCache;
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;
        public BaseDataEditModel( IMyService myService, IMemoryCache memoryCache, IUserService userService, IWebHostEnvironment environment)
        {

            _myService = myService;
            _memoryCache = memoryCache;
            _userService = userService;
            _environment = environment;
        }
        [BindProperty]
        public My basedata { get; set; }
        public Models.User User_me { get; set; }
        public IActionResult OnGet()
        {
            TempData.Clear();
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            basedata = _myService.GetBaseData();
            if (basedata == null)
            {
                return NotFound();
            }
            return Page();
        }

        public IActionResult OnPostUpdate()
        {
            System.Diagnostics.Debug.Print("W");
            // 驗證 User 物件
            var validationContext = new ValidationContext(basedata);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(basedata, validationContext, validationResults, true);

            if (!isValid)
            {
                return NotFound();
            }
            if (_myService.UpdateBaseData(basedata)){
                TempData["Message"] = "已儲存。";
                _memoryCache.Remove("BaseData");
                return Page();
            }
            else
            {
                TempData["Error"] = "失敗。";
                return Page();
            }

        }


        public async Task<IActionResult> OnPostAsync(List<IFormFile> imageFiles)
        {
            if (imageFiles != null && imageFiles.Count >= 1)
            {
                for (int i = 0; i < imageFiles.Count; i++)
                {
                    var imageFile = imageFiles[i];
                    if (imageFile.Length > 0)
                    {   
                        var filePath = Path.Combine(_environment.WebRootPath, "dist", "img", $"Banner{i + 1}.jpg");

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                    }
                }

                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}
