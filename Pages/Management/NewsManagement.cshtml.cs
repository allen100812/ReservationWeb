using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class NewsManagementModel : PageModel
    {
        private readonly INewService _newService;
        private readonly IUserService _userService;

        public NewsManagementModel(INewService newService, IUserService userService)
        {
            _newService = newService;
            _userService = userService;
        }

        [BindProperty]
        public NewList NewItem { get; set; } = new();

        public List<NewList> NewsList { get; set; } = new();
        public DateTime? TopTime { get; set; }

        public IActionResult OnGet()
        {

            var check = _userService.CheckCurrentUserPermission(this);
            if (check != null) return check;

            NewsList = _newService.GetNewTB().ToList();
            return Page();
        }

        public IActionResult OnPostEdit(int id)
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }


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
                    topTime = news.TopTime?.ToString("yyyy-MM-ddTHH:mm"), // ✅ 加上這行
                    status = news.Status,
                    tag = news.Tag,
                    link = news.Link, // ✅ 加這行
                    // 將圖片轉為 base64 字串陣列
                    photoList = news.PhotoList?.Select(p => Convert.ToBase64String(p)).ToList()
                }
            });
        }


        public JsonResult OnPostSave()
        {
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }


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
                    Link = form["NewItem.Link"], // ✅ 加這行：接收表單送進來的 Link
                    PhotoList = new List<byte[]>()
                };

                // ✅ 接收 TopTime（空字串代表取消置頂）
                var topTimeRaw = form["NewItem.TopTime"];
                news.TopTime = string.IsNullOrEmpty(topTimeRaw)
                    ? null
                    : (DateTime.TryParse(topTimeRaw, out var parsedTop) ? parsedTop : null);

                // ✅ 若為「已發布」但未指定 TopTime，則自動補上現在時間（確保有置頂時間）
                if (news.Status == 1 && news.TopTime == null)
                {
                    news.TopTime = DateTime.Now;
                }
                
                // 取得保留的原圖索引
                var preservedIndexes = form["PreservedPhotoIndexes"].ToArray().Select(int.Parse).ToList();

                // 撈出舊圖（若為編輯）
                var original = news.NewId != null ? _newService.GetNewListById(news.NewId.Value) : null;
                var oldPhotos = original?.PhotoList ?? new List<byte[]>();

                foreach (var idx in preservedIndexes)
                {
                    if (idx >= 0 && idx < oldPhotos.Count)
                        news.PhotoList.Add(oldPhotos[idx]);
                }

                // 加入新圖
                foreach (var file in files)
                {
                    using var ms = new MemoryStream();
                    file.CopyTo(ms);
                    news.PhotoList.Add(ms.ToArray());
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
            var pageName = this.GetType().Name.Replace("Model", "");
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId) || !_userService.HasPagePermissionByName(userId, pageName))
            {
                return new JsonResult(new { success = false, message = "無權限" });
            }


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
