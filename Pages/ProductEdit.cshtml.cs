using MDP.DevKit.LineMessaging;
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
    public class ProductEditModel : PageModel
    {
        private readonly IProductService _ProductService;
        private readonly IPgroupService _PgroupService;
        private readonly IUserService _userService;

        public ProductEditModel(IProductService ProductService, IPgroupService pgroupService, IUserService userService)
        {
            _ProductService = ProductService;
            _PgroupService = pgroupService;
            _userService = userService;
        }

        [BindProperty]
        public Product Product { get; set; }

        public Product Product_temp { get; set; }
        public List<Pgroup> Pgroups { get; set; }
        public Models.User User_me { get; set; }
        public IActionResult OnGet(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            User_me = _userService.GetUserById(User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value);
            if (User_me == null || int.Parse(User_me.UserType) > 1)
            {
                return NotFound();
            }

            Product = _ProductService.GetProductById(id);
            Pgroups = _PgroupService.GetPgroupTB().ToList();
            if (Product == null || Pgroups == null)
            //if (Product == null)
            {
                return NotFound();
            }
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
            Product_temp = _ProductService.GetProductById(Product.Pid);
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
            else
            {
                Product.Photo = Product_temp.Photo;
            }
            if (Product.Porder == null)
            {
                Product.Porder = "";
            }
            // 驗證 User 物件
            var validationContext = new ValidationContext(Product);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(Product, validationContext, validationResults, true);

            if (!isValid)
            {
                return NotFound();
            }


            bool EditProduct = _ProductService.UpdateProduct(Product);

            if (EditProduct == true)
            {
                TempData["edit_success"] = true;
                return RedirectToPage("ProductList_m");
            }
            else
            {
                TempData["edit_error"] = true;
                Pgroups = _PgroupService.GetPgroupTB().ToList();
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
