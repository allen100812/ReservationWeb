using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Web;
using Web0524.Models;

namespace Web0524.Pages
{
    public class OrderFormModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrderFormModel(IUserService userService, IHttpContextAccessor httpContextAccessor)
        {
            _userService = userService;
            _httpContextAccessor = httpContextAccessor;
        }

        [BindProperty]
        public User User { get; set; }
        public User? UserModel { get; set; }

        public void OnGet()
        {
            TempData.Clear();
            //ViewData["MyName"] = My.Name;
            //ViewData["MyLineBotURL"] = My.LineBotURL;
            ViewData["ShowModal"] = true;
        }
        public IActionResult OnPost(string handler)
        {
            if (handler == "BindLine")
            {

                return Page();
            }
            else
            {
                var validationContext = new ValidationContext(User);
                var validationResults = new List<ValidationResult>();
                bool isValid = Validator.TryValidateObject(User, validationContext, validationResults, true);

                if (!isValid)
                {
                    //foreach (var validationResult in validationResults)
                    //{
                    //    // 將檢查失敗的原因輸出到控制台
                    //    System.Diagnostics.Debug.Print(validationResult.ErrorMessage);
                    //}
                    //validationResult.ErrorMessage
                    return Page();
                }
                User.UserType = "3";
                var ipAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                var ip_regnum = _userService.Iplog_CheckByIp(ipAddress, 1);
                if (ip_regnum > My.Max_Reg_Oneday)
                {
                    TempData["ip_regnum_error"] = true;
                    return RedirectToPage("/Index");
                }
                _userService.Iplog_save(ipAddress, User.Id, 1);
                bool AddUser = _userService.AddUser(User);

                if (AddUser == true)
                {
                    //直接登入
                    UserModel = _userService.UserLogin(User.Id, User.Password);
                    string base64String;
                    if (UserModel != null)
                    {
                        if (UserModel.Photo != null)
                        {
                            byte[] byteArray = UserModel.Photo;
                            base64String = Convert.ToBase64String(byteArray);
                        }
                        else
                        {
                        base64String = "";
                        }
                        // Now, you have the photo as a byte array, and you can use it as needed.
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Sid, UserModel.Id),
                            new Claim(ClaimTypes.Name, UserModel.Name),
                            new Claim("UserType", UserModel.UserType),
                        };
                        var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));
                        TempData["fristlogin_success"] = true;
                        return RedirectToPage("/ProductList");
                    }
                    else
                    {
                        TempData["fristlogin_err"] = true;
                    }
                    return RedirectToPage("/Account/Login");
                }
                else
                {
                    TempData["add_error"] = true;
                    return Page();
                }
            }


        }

    }
}
