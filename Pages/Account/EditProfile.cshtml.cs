using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;
using System.Security.Claims;
using Microsoft.Win32;
using static MDP.DevKit.LineMessaging.LineMessageException;
using System.Text.RegularExpressions;

namespace Web0524.Pages.Account
{
    public class EditProfileModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly IEmailVerificationService _emailVerificationService;
        public EditProfileModel(IUserService userService, IEmailVerificationService emailVerificationService)
        {
            _userService = userService;
            _emailVerificationService = emailVerificationService;
        }


        [BindProperty]
        public User EditUser { get; set; }

        [BindProperty]
        public IFormFile? PhotoFile { get; set; }

        public IActionResult OnGet(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.Sid);
            if (id != userId) return Unauthorized();

            var user = _userService.GetUserById(id);
            if (user == null) return NotFound();

            EditUser = user;
            return Page();
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnPostSaveProfile()
        {
            var id = Request.Form["Id"];
            var name = Request.Form["Name"];
            var phone = Request.Form["Phone"];
            var address = Request.Form["Address"];
            var birthday = Request.Form["Birthday"];
            var newEmail = Request.Form["NewEmail"];
            var verifyCode = Request.Form["NewEmailVerifyCode"];
            var photoFile = Request.Form.Files["PhotoFile"];

            var currentId = User.FindFirstValue(ClaimTypes.Sid);
            if (id != currentId)
                return new JsonResult(new { success = false, message = "無權限編輯他人資料。" });

            var user = _userService.GetUserById(id);
            if (user == null)
                return new JsonResult(new { success = false, message = "找不到使用者。" });

            // 信箱驗證 + 更新（由服務處理）
            if (!string.IsNullOrEmpty(newEmail) && newEmail != user.Email)
            {
                var verified = _emailVerificationService.VerifyCode(newEmail, verifyCode);
                if (!verified)
                    return new JsonResult(new { success = false, message = "驗證碼錯誤或過期，無法更改信箱。" });

                var updated = _userService.UpdateUserEmail(user.Id, newEmail);
                if (!updated)
                    return new JsonResult(new { success = false, message = "更新信箱或訂單失敗。" });

                // Email 與 Id 都已由服務更新，所以我們要重新設置 user.Id & Email
                user.Id = newEmail;
                user.Email = newEmail;
            }

            // 更新其餘欄位
            user.Name = name;
            user.Phone = phone;
            user.Address = address;
            if (DateTime.TryParse(birthday, out var parsedBirthday))
                user.Birthday = parsedBirthday;

            // 處理頭像
            if (photoFile != null && photoFile.Length > 0)
            {
                using var ms = new MemoryStream();
                photoFile.CopyTo(ms);
                user.Photo = ms.ToArray();
            }

            var success = _userService.UpdateUser(user);
            return new JsonResult(new { success, message = success ? "更新成功！" : "更新失敗" });
        }

        public IActionResult OnPostSendEmailCode()
        {


            var email = Request.Form["NewEmail"];

            if (string.IsNullOrWhiteSpace(email))
            {
                return new JsonResult(new { success = false, message = "請正確輸入 Email。" });
            }

            var now = DateTime.UtcNow;
            var lastSent = _emailVerificationService.GetLastSentTime(email);
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
            if (!_emailVerificationService.CanSendEmail(email, ip))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "寄送頻率過高或次數超過限制，請稍後再試。",
                    countdown = 60
                });
            }

            // ✅ 嘗試寄送
            var success = _emailVerificationService.SendVerificationCode(email, ip);

            return new JsonResult(new
            {
                success,
                message = success ? "驗證碼已發送至您的信箱。" : "發送失敗，請稍後再試。",
                countdown = 60
            });
        }


        [IgnoreAntiforgeryToken]
        public JsonResult OnPostChangePassword(string id, string oldPassword, string newPassword, string confirmPassword)
        {
            var userId = User.FindFirstValue(ClaimTypes.Sid);
            if (id != userId)
                return new JsonResult(new { success = false, message = "無權限操作。" });

            var user = _userService.GetUserById(id);
            if (user == null)
                return new JsonResult(new { success = false, message = "找不到帳戶。" });

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
                return new JsonResult(new { success = false, message = "密碼欄位不得為空。" });

            if (user.Password != oldPassword)
                return new JsonResult(new { success = false, message = "目前密碼錯誤。" });

            if (newPassword != confirmPassword)
                return new JsonResult(new { success = false, message = "新密碼與確認密碼不一致。" });

            var regex = new Regex(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d]{8,}$");
            if (!regex.IsMatch(newPassword))
                return new JsonResult(new { success = false, message = "新密碼格式錯誤，請輸入至少 8 位數的英數混合密碼。" });

            user.Password = newPassword;
            var updated = _userService.UpdateUser(user);

            return new JsonResult(new
            {
                success = updated,
                message = updated ? "密碼變更成功！" : "密碼變更失敗。"
            });
        }

    }

}
