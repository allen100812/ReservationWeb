using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Quartz.Util;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class PortfolioManagementModel : PageModel
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IPortfolioGroupService _groupService;

        public PortfolioManagementModel(IPortfolioService portfolioService, IPortfolioGroupService groupService)
        {
            _portfolioService = portfolioService;
            _groupService = groupService;
        }

        [BindProperty]
        public Portfolio NewPortfolio { get; set; } = new();

        [BindProperty]
        public List<IFormFile> PhotoUploads { get; set; } = new();

        public List<Portfolio> AllPortfolios { get; set; } = new();
        public List<PortfolioGroup> AllGroups { get; set; } = new();

        public async Task OnGetAsync()
        {
            AllPortfolios = (await _portfolioService.GetAllAsync()).ToList();
            AllGroups = (await _groupService.GetAllAsync()).ToList();
        }

        public async Task<IActionResult> OnPostUpdateAsync(string action)
        {
            Console.WriteLine($"✅ OnPostUpdateAsync called, Action = {action}");

            AllGroups = (await _groupService.GetAllAsync()).ToList(); // 保證錯誤時仍可用
            AllPortfolios = (await _portfolioService.GetAllAsync()).ToList(); // 同上
            if (!ModelState.IsValid)
            {
                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        Console.WriteLine($"❌ 驗證失敗：{key} = {error.ErrorMessage}");
                    }
                }

                // 重新讀取 PhotoList（若為編輯模式）
                if (NewPortfolio?.Portfolio_Id > 0)
                {
                    var dbPortfolio = await _portfolioService.GetByIdAsync(NewPortfolio.Portfolio_Id);
                    if (dbPortfolio != null)
                    {
                        NewPortfolio.PhotoList = dbPortfolio.PhotoList;
                    }
                }

                TempData["ErrorMessage"] = "欄位驗證失敗，請檢查欄位是否正確填寫。";
                return Page();
            }


            var photoBytes = new List<byte[]>();
            foreach (var file in PhotoUploads)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                photoBytes.Add(ms.ToArray());
            }

            try
            {
                if (action == "add")
                {
                    await _portfolioService.CreateAsync(NewPortfolio, photoBytes);
                }
                else if (action == "save")
                {
                    await _portfolioService.UpdateAsync(NewPortfolio, photoBytes, new List<int>());
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "儲存過程中發生錯誤：" + ex.Message;
                return Page();
            }

            return RedirectToPage();
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostDeletePhotoAsync(int photoId)
        {
            var success = await _portfolioService.DeletePhotoAsync(photoId);
            return new JsonResult(new { success });
        }

        public async Task<IActionResult> OnPostEditAsync(int portfolioId)
        {
            Console.WriteLine($"⚙️ 編輯作品集 ID = {portfolioId}"); // 先確認有執行

            NewPortfolio = await _portfolioService.GetByIdAsync(portfolioId) ?? new();
            AllPortfolios = (await _portfolioService.GetAllAsync()).ToList();
            AllGroups = (await _groupService.GetAllAsync()).ToList();

            return Page(); // 回到原本頁面，表單會用 NewPortfolio 資料渲染
        }

        public async Task<IActionResult> OnPostDeleteAsync(int portfolioId)
        {
            await _portfolioService.DeleteAsync(portfolioId);
            return RedirectToPage();
        }
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostAddGroupAsync(string name, string content)
        {
            var group = new PortfolioGroup
            {
                PortfolioGroup_Name = name,
                PortfolioGroup_Content = content
            };

            var success = await _groupService.CreateAsync(group);

            // ✅ 回傳 group 本體給前端（CreateAsync 要能更新 Id）
            return new JsonResult(new { success, group });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostDeleteGroupAsync(int id)
        {
            var result = await _groupService.DeleteAsync(id);
            return new JsonResult(new { success = result });
        }
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostUpdateGroupAsync(int id, string name, string content)
        {
            var result = await _groupService.UpdateAsync(new PortfolioGroup { PortfolioGroup_Id = id, PortfolioGroup_Name = name, PortfolioGroup_Content = content });
            return new JsonResult(new { success = result });
        }
    }
}

