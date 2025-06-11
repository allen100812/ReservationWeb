using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class NewsManagementModel : PageModel
    {
        private readonly INewService _newService;

        public NewsManagementModel(INewService newService)
        {
            _newService = newService;
        }

        [BindProperty]
        public NewList NewItem { get; set; } = new();

        public List<NewList> NewsList { get; set; } = new();

        public IActionResult OnGet()
        {
            NewsList = _newService.GetNewTB().ToList();
            return Page();
        }

        public IActionResult OnPostEdit(int id)
        {
            var news = _newService.GetNewListById(id);
            if (news == null)
                return new JsonResult(new { success = false });

            return new JsonResult(new
            {
                success = true,
                data = new
                {
                    newId = news.NewId,
                    title = news.Title,
                    author = news.Author,
                    content = news.Content,
                    publishDate = news.PublishDate?.ToString("yyyy-MM-ddTHH:mm"),
                    status = news.Status,
                    tag = news.Tag,
                    // 將圖片轉為 base64 字串陣列
                    photoList = news.PhotoList?.Select(p => Convert.ToBase64String(p)).ToList()
                }
            });
        }


        public JsonResult OnPostSave()
        {
            try
            {
                var form = Request.Form;
                var files = Request.Form.Files;

                var news = new NewList
                {
                    NewId = string.IsNullOrEmpty(form["NewItem.NewId"]) ? null : int.Parse(form["NewItem.NewId"]),
                    Title = form["NewItem.Title"],
                    Author = form["NewItem.Author"],
                    Content = form["NewItem.Content"],
                    PublishDate = DateTime.TryParse(form["NewItem.PublishDate"], out var dt) ? dt : null,
                    Status = int.TryParse(form["NewItem.Status"], out var st) ? st : 0,
                    Tag = int.TryParse(form["NewItem.Tag"], out var tag) ? tag : null,
                };

                // 儲存圖片
                news.PhotoList = new();
                foreach (var photo in files)
                {
                    using var ms = new MemoryStream();
                    photo.CopyTo(ms);
                    news.PhotoList.Add(ms.ToArray());
                }

                if (news.Status == 1 && (news.TopTime == null || news.TopTime == DateTime.MinValue))
                {
                    news.TopTime = DateTime.Now;
                }

                if (news.NewId == null)
                {
                    _newService.AddNewList(news);
                }
                else
                {
                    _newService.UpdateNewList(news);
                }

                return new JsonResult(new { success = true, message = "儲存成功" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "儲存錯誤: " + ex.Message });
            }
        }

        public JsonResult OnPostToggleStatus(int id, int status)
        {
            try
            {
                _newService.UpdateStatus(id, status);
                return new JsonResult(new { success = true, message = "狀態已更新" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "更新錯誤: " + ex.Message });
            }
        }
    }
}
