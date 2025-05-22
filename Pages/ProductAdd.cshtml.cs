using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Web0524.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using MDP.DevKit.LineMessaging;
using System.Drawing.Imaging;
using System.Drawing;

namespace Web0524.Pages
{
    [Authorize]
    public class ProductAddModel : PageModel
    {
        private readonly IProductService _ProductService;
        private readonly IPgroupService _PgroupService;
        private readonly IUserService _userService;

        public ProductAddModel(IProductService ProductService, IPgroupService pgroupService, IUserService userService)
        {
            _ProductService = ProductService;
            _PgroupService = pgroupService;
            _userService = userService;
        }

        [BindProperty]
        public Product Product { get; set; }
        public List<Pgroup> Pgroups { get; set; }

        public Models.User User_me { get; set; }
        public IActionResult OnGet()
        {
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }
            Pgroups = _PgroupService.GetPgroupTB().ToList();
            return Page();
        }

        [HttpPost]
        public IActionResult OnPost(IFormFile newImage)
        {
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
                                        Product.Photo = memoryStream.ToArray();
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
            if (Product.Porder == null)
            {
                Product.Porder = "";
            }
            Product.Pid = "0";
            // 驗證 User 物件
            var validationContext = new ValidationContext(Product);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(Product, validationContext, validationResults, true);

            if (!isValid)
            {
                //validationResult.ErrorMessage
                return NotFound();
            }

            bool AddProduct = _ProductService.AddProduct(Product);

            if (AddProduct == true)
            {
                TempData["add_success"] = true;
                return RedirectToPage("ProductList_m");
            }
            else
            {
                TempData["add_error"] = true;
                return RedirectToPage("ProductAdd");
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
