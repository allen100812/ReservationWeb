using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class ProductManagementModel : PageModel
    {
        private readonly IProductService _productService;
        private readonly IPgroupService _pgroupService;

        public ProductManagementModel(IProductService productService, IPgroupService pgroupService)
        {
            _productService = productService;
            _pgroupService = pgroupService;
        }

        [BindProperty]
        public Product Product { get; set; } = new Product();

        [BindProperty]
        public IFormFile? PhotoFile { get; set; }

        public List<Product> Products { get; set; } = new();

        public List<Pgroup> PGroups { get; set; } = new(); // 為顯示 PGname
        [IgnoreAntiforgeryToken]
        public void OnGet(int? id)
        {
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
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnPostSave()
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(new { success = false, message = "請填寫所有必要欄位。" });
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
            _productService.DeleteProduct(id);
            return new JsonResult(new { success = true, message = "產品已刪除。" });
        }

        [IgnoreAntiforgeryToken]
        public IActionResult OnGetImage(int id)
        {
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
