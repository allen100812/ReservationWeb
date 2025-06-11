using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class PGManagementModel : PageModel
    {
        private readonly IPgroupService _pgroupService;

        public PGManagementModel(IPgroupService pgroupService)
        {
            _pgroupService = pgroupService;
        }

        [BindProperty]
        public Pgroup Pgroup { get; set; } = new();
        [IgnoreAntiforgeryToken]
        public JsonResult OnGetList()
        {
            var data = _pgroupService.GetAllPgroups();
            return new JsonResult(data);
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnGetGet(int id)
        {
            var data = _pgroupService.GetPgroupById(id);
            if (data == null) return new JsonResult(NotFound());
            return new JsonResult(data);
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnPostSave()
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "資料驗證失敗";
                return new JsonResult(new { success = false, message = firstError });
            }

            if (_pgroupService.IsPgroupNameDuplicate(Pgroup.PGname, Pgroup.PGid == 0 ? null : Pgroup.PGid))
            {
                return new JsonResult(new { success = false, message = "分類名稱重複" });
            }

            if (Pgroup.PGid == 0)
            {
                var newId = _pgroupService.CreatePgroup(Pgroup);
                return new JsonResult(new { success = true, message = "新增成功", id = newId });
            }
            else
            {
                var updated = _pgroupService.UpdatePgroup(Pgroup);
                return new JsonResult(new { success = updated, message = updated ? "更新成功" : "更新失敗" });
            }
        }
        [IgnoreAntiforgeryToken]
        public JsonResult OnPostDelete(int id)
        {
            var deleted = _pgroupService.DeletePgroup(id);
            return new JsonResult(new { success = deleted, message = deleted ? "刪除成功" : "刪除失敗" });
        }
    }
}
