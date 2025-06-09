// Pages/Account/Register.cshtml.cs
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Web0524.Models;
namespace Web0524.Pages.Account
{


    public class RegisterInput
    {
        [Required(ErrorMessage = "請輸入使用者名稱")]
        [Display(Name = "使用者名稱")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "請輸入電子郵件")]
        [EmailAddress(ErrorMessage = "請輸入有效的電子郵件")]
        [Display(Name = "電子郵件")]
        public string Email { get; set; }


        [Required(ErrorMessage = "請輸入密碼")]
        [DataType(DataType.Password)]
        [Display(Name = "密碼")]
        [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$", ErrorMessage = "密碼需為至少8位英數混合")]
        public string Password { get; set; }

        [Required(ErrorMessage = "請再次輸入密碼")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "兩次輸入的密碼不一致")]
        [Display(Name = "確認密碼")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "請輸入驗證碼")]
        [Display(Name = "驗證碼")]
        public string VerificationCode { get; set; }
    }
    public class RegisterModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IEmailVerificationService _emailVerificationService;

        public RegisterModel(IUserService userService, IEmailVerificationService emailVerificationService)
        {
            _userService = userService;
            _emailVerificationService = emailVerificationService;
        }

        [BindProperty]
        public RegisterInput Register { get; set; }

        [TempData]
        public string? Message { get; set; }

        public int CountdownSeconds { get; set; } = 0;

        public void OnGet()
        {
            if (!string.IsNullOrEmpty(Register?.Email))
            {
                var lastSent = _emailVerificationService.GetLastSentTime(Register.Email);
                if (lastSent.HasValue)
                {
                    var secondsLeft = 60 - (int)(DateTime.UtcNow - lastSent.Value).TotalSeconds;
                    if (secondsLeft > 0) CountdownSeconds = secondsLeft;
                }
            }
        }
        public IActionResult OnPostSendCode()
        {
            if (string.IsNullOrWhiteSpace(Register?.Email))
            {
                return new JsonResult(new { success = false, message = "請正確輸入 Email。" });
            }

            var now = DateTime.UtcNow;
            var lastSent = _emailVerificationService.GetLastSentTime(Register.Email);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (lastSent.HasValue)
            {
                var diffSeconds = (now - lastSent.Value).TotalSeconds;
                int secondsLeft = 60 - (int)diffSeconds;

                Console.WriteLine($"[驗證信節流檢查]");
                Console.WriteLine($"現在時間       : {now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"上次寄送時間   : {lastSent.Value:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"相隔秒數       : {diffSeconds}");

                if (diffSeconds < 60)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = $"驗證碼已發送，請在 {secondsLeft} 秒後再試。",
                        countdown = secondsLeft
                    });
                }
            }

            // ✅ 檢查寄信次數限制（IP與Email）
            if (!_emailVerificationService.CanSendEmail(Register.Email, ip))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "寄送頻率過高或次數超過限制，請稍後再試。",
                    countdown = 60
                });
            }

            // ✅ 嘗試寄送
            var success = _emailVerificationService.SendVerificationCode(Register.Email, ip);

            return new JsonResult(new
            {
                success,
                message = success ? "驗證碼已發送至您的信箱。" : "發送失敗，請稍後再試。",
                countdown = 60
            });
        }


        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostRegisterJsonAsync()
        {
            if (!ModelState.IsValid)
                return new JsonResult(new { success = false, message = "請填寫完整資訊。" });

            var emailExists = _userService.GetUserTB().Any(u => u.Id == Register.Email);
            if (emailExists)
                return new JsonResult(new { success = false, message = "此電子郵件已被註冊，請使用其他信箱。" });

            var codeOk = _emailVerificationService.VerifyCode(Register.Email, Register.VerificationCode);
            if (!codeOk)
                return new JsonResult(new { success = false, message = "驗證碼錯誤或已過期。" });

            var newUser = new User
            {
                Id = Register.Email,
                Name = Register.UserName,
                Password = Register.Password,
                UserType = (int)UserTypeEnum.Email,
                Address = "",
                Phone = "",
                Email = Register.Email,
                Line = "",
                Photo = Array.Empty<byte>(),
                OrderNum = 0,
                CancelNum = 0,
                Remark = "",
                Birthday = null,
                LineUserId = "",
                Role = (int)UserRoleEnum.Member,
                PermissionSetId = 5,
                IsDeleted = false
            };

            var result = _userService.CreateUser(newUser);

            if (result)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Sid, newUser.Id),
            new Claim(ClaimTypes.Name, newUser.Name),
            new Claim(ClaimTypes.Role, newUser.Role.ToString())
        };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                return new JsonResult(new { success = true, message = "註冊成功！" });
            }

            return new JsonResult(new { success = false, message = "註冊失敗，請稍後再試。" });
        }


    }


}

