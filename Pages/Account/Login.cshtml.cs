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
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace Web0524.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _config;
        private readonly LineMessageService _lineService;
        public LoginModel(IUserService userService, IConfiguration config, LineMessageService lineService)
        {
            _userService = userService;
            _config = config;
            _lineService = lineService;
        }
        [BindProperty]
        public User? UserModel { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            Console.WriteLine("1");
            var validationContext = new ValidationContext(UserModel);
            var validationResults = new List<ValidationResult>();
            bool isValid = (UserModel.Id != null && UserModel.Id != "") && (UserModel.Password != null && UserModel.Password != "");
            Console.WriteLine("2");
            if (!isValid)
                return Page();
            Console.WriteLine("3");
            UserModel = _userService.UserLogin(UserModel.Id, UserModel.Password);
            string base64String;
            if (UserModel != null)
            {
                Console.WriteLine("4");
                if (UserModel.Photo != null)
                    base64String = Convert.ToBase64String(UserModel.Photo);
                else
                    base64String = "";

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Sid, UserModel.Id),
                    new Claim(ClaimTypes.Name, UserModel.Name),
                    new Claim("UserType", UserModel.UserType),
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                return RedirectToPage("/Index");
            }
            else
            {
                Console.WriteLine("5");
                ViewData["ErrorMsg"] = "登入帳號或密碼錯誤!!";
            }

            return Page();
        }

        public IActionResult OnGetLineLogin()
        {
            var clientId = _config["LineLogin:ChannelId"];
            var redirectUri = _config["LineLogin:RedirectUri"];
            var state = Guid.NewGuid().ToString();
            var scope = "profile openid email";

            var url = $"https://access.line.me/oauth2/v2.1/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&state={state}&scope={scope}";
            return Redirect(url);
        }
        public async Task<IActionResult> OnGetLineCallback(string code, string state)
        {
            var tokenEndpoint = "https://api.line.me/oauth2/v2.1/token";
            var clientId = _config["LineLogin:ChannelId"];
            var clientSecret = _config["LineLogin:ChannelSecret"];
            var redirectUri = _config["LineLogin:RedirectUri"];

            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        {"grant_type", "authorization_code"},
        {"code", code},
        {"redirect_uri", redirectUri},
        {"client_id", clientId},
        {"client_secret", clientSecret}
    });

            var response = await client.PostAsync(tokenEndpoint, content);
            var json = await response.Content.ReadAsStringAsync();
            dynamic tokenResult = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
            string idToken = tokenResult.id_token;

            var payload = idToken.Split('.')[1];
            var jsonPayload = Base64UrlDecode(payload);
            var userInfo = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonPayload);


            var lineUserId = userInfo["sub"].ToString();
            var name = userInfo.ContainsKey("name") ? userInfo["name"].ToString() : "LineUser";
            var email = userInfo.ContainsKey("email") ? userInfo["email"].ToString() : "";

            // 嘗試查找已存在使用者
            var user = _userService.GetUserByLineUserId(lineUserId);

            if (user == null)
            {
                user = new User
                {
                    Id = "LINE_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    Name = name,
                    Password = Guid.NewGuid().ToString("N"), // 給定亂數密碼避免登入
                    UserType = "Line",
                    Email = email,
                    LineUserId = lineUserId, //"Udf96f6192a32d72329c908a69805aa9e",
                    OrderNum = 0,
                    CancelNum = 0,

                    // 加上角色與權限
                    Role = "Member",
                    PermissionSetId = 0,
                };

                _userService.CreateUser(user);
            }


            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Sid, user.Id),
        new Claim(ClaimTypes.Name, user.Name),
        new Claim("UserType", user.UserType)
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);


            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            if (!string.IsNullOrEmpty(lineUserId))
            {
                var success = await _lineService.SendSecureLineMessageAsync(lineUserId, "登入完成");
                TempData["Result"] = success ? "訊息已發送！" : "發送失敗。";
            }


            return RedirectToPage("/Index");
        }


        string Base64UrlDecode(string base64Url)
        {
            string padded = base64Url
                .Replace('-', '+')
                .Replace('_', '/');

            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        }

    }
}
