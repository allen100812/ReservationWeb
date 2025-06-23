using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class ProductManagementModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly IPgroupService _pgroupService;
        private readonly IUserService _userService;

        public ProductManagementModel(IProductService productService, IPgroupService pgroupService, IUserService userService)
        {
            _productService = productService;
            _pgroupService = pgroupService;
            _userService = userService;
        }

        [BindProperty]
        public Product Product { get; set; } = new Product();

        [BindProperty]
        public IFormFile? PhotoFile { get; set; }

        public List<Product> Products { get; set; } = new();

        public List<Pgroup> PGroups { get; set; } = new(); // 為顯示 PGname
        [IgnoreAntiforgeryToken]
        public IActionResult OnGet(int? id)
        {

            var check = _userService.CheckCurrentUserPermission(this);
            if (check != null) return check;

            Products = _productService.GetAllProducts().ToList();
            PGroups = _pgroupService.GetAllPgroups().ToList(); ;

            if (id.HasValue)
            {
                var product = _productService.GetProductById(id.Value);
                if (product != null)
                {
                    Product = product;
                }
            }
            return Page();
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnPostSave()
        {

            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, message = "請填寫所有必要欄位。" });
            }

            if (Product.ProductId > 0)
            {
                // 取得原本的圖片資料
                var original = _productService.GetProductById(Product.ProductId);
                if (original != null && PhotoFile == null)
                {
                    Product.Photo = original.Photo; // 沒有上傳新圖，就保留原圖
                }
            }

            if (PhotoFile != null)
            {
                using var ms = new MemoryStream();
                PhotoFile.CopyTo(ms);
                Product.Photo = ms.ToArray();
            }

            if (Product.ProductId == 0)
            {
                _productService.CreateProduct(Product);
                return new JsonResult(new { success = true, message = "產品已成功新增。" });
            }
            else
            {
                _productService.UpdateProduct(Product);
                return new JsonResult(new { success = true, message = "產品已成功更新。" });
            }
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostDelete(int id)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            _productService.DeleteProduct(id);
            return new JsonResult(new { success = true, message = "產品已刪除。" });
        }

        [IgnoreAntiforgeryToken]
        public IActionResult OnGetImage(int id)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }
            Products = _productService.GetAllProducts().ToList();
            var product = Products.FirstOrDefault(p => p.ProductId == id);
            if (product?.Photo != null)
            {
                return File(product.Photo, "image/jpeg"); // 或 "image/png"，根據實際格式
            }

            return NotFound();
        }

    }
}
