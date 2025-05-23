using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Imaging;
using System.Drawing;
using Web0524.Models;

namespace Web0524.Pages
{
    [Authorize]
    public class NewEditModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly INewService _newService;
        public NewEditModel(IUserService userService, INewService newService)
        {
            _userService = userService;
            _newService = newService;
        }
        [BindProperty]
        public Models.User User_me { get; set; }
        [BindProperty]
        public NewList newList { get; set; }
        public DateTime TaipeiTime { get; set; }
        public IActionResult OnGet(string? id)
        {
            var twtzinfo = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            TaipeiTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, twtzinfo);
            if (id == null)
            {
                return NotFound();
            }
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            newList = _newService.GetNewListById(int.Parse(id));
            if (newList == null)
            {
                return NotFound();
            }
            return Page();
        }

        [HttpPost]
        public IActionResult OnPost(IFormFile newImage)
        {
            var twtzinfo = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");
            TaipeiTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, twtzinfo);
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
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
                                        newList.Photo = memoryStream.ToArray();
                                        System.Diagnostics.Debug.Print("2"+BitConverter.ToString(newList.Photo).Replace("-", "").ToLower());
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
            newList.PublishDate = TaipeiTime;
            // 驗證 User 物件
            var validationContext = new ValidationContext(newList);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(newList, validationContext, validationResults, true);

            if (!isValid)
            {
                return NotFound();
            }

            bool EditnewList = _newService.UpdateNewList(newList);


            if (EditnewList == true)
            {
                TempData["edit_success"] = true;
                return RedirectToPage("New");
            }
            else
            {
                TempData["edit_error"] = true;
                return Page();
            }
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
