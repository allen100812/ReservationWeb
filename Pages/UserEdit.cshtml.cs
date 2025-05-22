using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Drawing.Imaging;


namespace Web0524.Pages
{
    [Authorize]
    public class UserEditModel : PageModel
    {

        private readonly IUserService _userService;
        
        public UserEditModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public User User_me { get; set; }
        public User User_Temp { get; set; }
        public IActionResult OnGet(string? id)
        {
            if (id == null || id != (User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value))
            {
                return NotFound();
            }

            User_me = _userService.GetUserById(id);
            System.Diagnostics.Debug.Print(User_me.Line);
            System.Diagnostics.Debug.Print(User_me.LineUserId);
            User_Temp = _userService.GetUserById(id);
            TempData.Clear();
            if (User_me == null)
            {
                return NotFound();
            }
            ViewData["Uid"] = User_me.Id;
            ViewData["ShowModal"] = true;
            return Page();
        }

        public IActionResult OnPost(string handler, IFormFile newImage)
        {
            User_Temp = _userService.GetUserById(User_me.Id);
            if (User_Temp.Id != (User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value))
            {
                return NotFound();
            }
            if (handler == "Edit")
            {
                //大頭貼
                if (newImage != null && newImage.Length > 0)
                {
                    using (var imageStream = newImage.OpenReadStream())
                    {
                        using (var originalImage = Image.FromStream(imageStream))
                        {

                            int orientation = 1; 

  
                            try
                            {
                                var orientationProperty = originalImage.GetPropertyItem(0x0112);

                                if (orientationProperty != null)
                                {
                                    orientation = BitConverter.ToInt16(orientationProperty.Value, 0);
                                }
                            }
                            catch (ArgumentException ex)
                            {

                            }

 
                            switch (orientation)
                            {
                                case 1: // Normal
                                    break;
                                case 2: // Flipped horizontally
                                    originalImage.RotateFlip(RotateFlipType.RotateNoneFlipX);
                                    break;
                                case 3: // Rotated 180 degrees
                                    originalImage.RotateFlip(RotateFlipType.Rotate180FlipNone);
                                    break;
                                case 4: // Flipped vertically
                                    originalImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                                    break;
                                case 5: // Rotated 90 degrees clockwise and flipped horizontally
                                    originalImage.RotateFlip(RotateFlipType.Rotate90FlipX);
                                    break;
                                case 6: // Rotated 90 degrees clockwise
                                    originalImage.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                    break;
                                case 7: // Rotated 90 degrees counterclockwise and flipped horizontally
                                    originalImage.RotateFlip(RotateFlipType.Rotate270FlipX);
                                    break;
                                case 8: // Rotated 90 degrees counterclockwise
                                    originalImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                    break;
                            }


                            int targetSizeInBytes = 50000; // 50 KB

                            int originalWidth = originalImage.Width;
                            int originalHeight = originalImage.Height;

                            int newWidth = originalWidth;
                            int newHeight = originalHeight;

                            while (true)
                            {
                                using (var resizedImage = new Bitmap(newWidth, newHeight))
                                using (var graphics = Graphics.FromImage(resizedImage))
                                {
                                    graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight);
                                    using (var memoryStream = new MemoryStream())
                                    {
                                        resizedImage.Save(memoryStream, ImageFormat.Jpeg);

                                        if (memoryStream.Length <= targetSizeInBytes || newWidth <= 0 || newHeight <= 0)
                                        {
                                            long imageSizeInBytes = memoryStream.Length;
                                            string imageSizeFormatted = GetFormattedSize(imageSizeInBytes);
                                            Console.WriteLine($"上傳大小: {imageSizeFormatted}");
                                            User_me.Photo = memoryStream.ToArray();
                                            break;
                                        }
                                    }
                                }

                                newWidth = (int)(newWidth * 0.9);
                                newHeight = (int)(newHeight * 0.9);
                            }
                        }
                    }
                }
                // 個人資料修改
                User_me.Id= User_Temp.Id;
                User_me.Password= User_Temp.Password;
                User_me.UserType = User_Temp.UserType;
                // 驗證 User 物件
                var validationContext = new ValidationContext(User_me);
                var validationResults = new List<ValidationResult>();
                bool isValid = Validator.TryValidateObject(User_me, validationContext, validationResults, true);

                if (!isValid)
                {
                    return NotFound();
                }
                _userService.UpdateUser(User_me);
                TempData["Message"] = "已儲存。";

            }
            else if (handler == "Reset")
            {
                // 密碼重設
                if (Request.Form["oldpassword"] == User_Temp.Password)
                {
                    User_me.Password = Request.Form["newpassword"];
                    if (_userService.UpdateUser_Password(User_Temp.Id, Request.Form["newpassword"]) != false)
                    {
                        TempData["Message"] = "密碼已重設。";
                    }
                    else {
                        TempData["Message"] = "密碼重設失敗。";
                    };

                    
                }
                else
                {
                    TempData["Error"] = "原密碼輸入錯誤。";
                }

            }
            User_me = _userService.GetUserById(User_me.Id);
            return Page();
        }
        private string GetFormattedSize(long sizeInBytes)
        {
            const int scale = 1024;
            string[] sizeSuffixes = { "B", "KB", "MB", "GB" };

            int i = 0;
            double size = (double)sizeInBytes;

            while (size > scale && i < sizeSuffixes.Length - 1)
            {
                size /= scale;
                i++;
            }

            return $"{size:N1} {sizeSuffixes[i]}";
        }
    }
}
