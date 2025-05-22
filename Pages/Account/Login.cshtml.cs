using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Web0524.Models;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Web0524.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        public LoginModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public User? UserModel { get; set; }

        
        public void OnGet()
        {
            //ViewData["MyName"] = My.Name;

        }

        public IActionResult OnPost()
        {
            // 驗證 User 物件
            var validationContext = new ValidationContext(UserModel);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(UserModel, validationContext, validationResults, true);

            if (!isValid)
            {
                return Page();
            }


            UserModel = _userService.UserLogin(UserModel.Id, UserModel.Password);
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

                return RedirectToPage("/Index");
                }
                else
                { 
                ViewData["ErrorMsg"] = "登入帳號或密碼錯誤!!";
                 }

                return Page();
        }
    }
}
