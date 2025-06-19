using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        public List<Portfolio> AllPortfolios { get; set; } = new();
        public List<PortfolioGroup> AllGroups { get; set; } = new();

        public void OnGet()
        {
            AllPortfolios = _portfolioService.GetAllAsync().Result.ToList();
            AllGroups = _groupService.GetAllAsync().Result.ToList();
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostEdit(int portfolioId)
        {
            var p = _portfolioService.GetByIdAsync(portfolioId).Result;
            if (p == null)
                return new JsonResult(new { success = false });

            return new JsonResult(new
            {
                success = true,
                data = new
                {
                    id = p.Portfolio_Id,
                    title = p.Portfolio_Title,
                    content = p.Portfolio_Content,
                    url = p.Portfolio_URL,
                    isPublished = p.IsPublished,
                    groupId = p.PortfolioGroup_Id,
                    photoList = p.PhotoList.Select(photo => new
                    {
                        photoId = photo.Photo_Id,
                        base64 = Convert.ToBase64String(photo.Photo)
                    }).ToList()
                }
            });
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostSave()
        {
            try
            {
                var form = Request.Form;
                var files = Request.Form.Files;

                var portfolioId = string.IsNullOrEmpty(form["id"]) ? 0 : int.Parse(form["id"]);
                var original = portfolioId > 0 ? _portfolioService.GetByIdAsync(portfolioId).Result : null;

                var portfolio = new Portfolio
                {
                    Portfolio_Id = portfolioId,
                    PortfolioGroup_Id = int.Parse(form["groupId"]),
                    Portfolio_Title = form["title"],
                    Portfolio_Content = form["content"],
                    Portfolio_URL = form["url"],
                    IsPublished = form["isPublished"] == "true",
                    PhotoList = new List<PortfolioPhoto>()
                };

                var preservedIds = form["preservedPhotoIds"].ToArray()
                    .Where(s => int.TryParse(s, out _))
                    .Select(int.Parse)
                    .ToHashSet();

                if (original != null)
                {
                    foreach (var photo in original.PhotoList)
                    {
                        if (preservedIds.Contains(photo.Photo_Id))
                            portfolio.PhotoList.Add(photo);
                    }
                }


                foreach (var file in files)
                {
                    if (file?.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        file.CopyTo(ms);
                        portfolio.PhotoList.Add(new PortfolioPhoto
                        {
                            Portfolio_Id = portfolio.Portfolio_Id,
                            Photo = ms.ToArray()
                        });
                    }
                }

                if (portfolioId == 0)
                {
                    _portfolioService.CreateAsync(portfolio, portfolio.PhotoList.Select(p => p.Photo).ToList()).Wait();
                }
                else
                {
                    var deletePhotoIds = original?.PhotoList
                        .Where(p => !preservedIds.Contains(p.Photo_Id))
                        .Select(p => p.Photo_Id)
                        .ToList() ?? new List<int>();

                    _portfolioService.UpdateAsync(
                        portfolio,
                        portfolio.PhotoList.Where(p => p.Photo_Id == 0).Select(p => p.Photo).ToList(),
                        deletePhotoIds
                    ).Wait();
                }



                return new JsonResult(new { success = true, message = "儲存成功" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "儲存失敗：" + ex.Message });
            }
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostDelete(int portfolioId)
        {
            try
            {
                _portfolioService.DeleteAsync(portfolioId).Wait();
                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostDeletePhoto(int photoId)
        {
            var success = _portfolioService.DeletePhotoAsync(photoId).Result;
            return new JsonResult(new { success });
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostAddGroup(string name, string content)
        {
            var group = new PortfolioGroup
            {
                PortfolioGroup_Name = name,
                PortfolioGroup_Content = content
            };

            var success = _groupService.CreateAsync(group).Result;
            return new JsonResult(new { success, group });
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostDeleteGroup(int id)
        {
            var result = _groupService.DeleteAsync(id).Result;
            return new JsonResult(new { success = result });
        }

        [IgnoreAntiforgeryToken]
        public JsonResult OnPostUpdateGroup(int id, string name, string content)
        {
            var group = new PortfolioGroup
            {
                PortfolioGroup_Id = id,
                PortfolioGroup_Name = name,
                PortfolioGroup_Content = content
            };

            var result = _groupService.UpdateAsync(group).Result;
            return new JsonResult(new { success = result });
        }
    }
}
