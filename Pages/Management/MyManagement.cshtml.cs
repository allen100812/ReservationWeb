using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web0524.Models;

namespace Web0524.Pages.Management
{
    public class MyManagementModel : PageModel
    {
        private readonly IMyService _myService;

        public MyManagementModel(IMyService myService)
        {
            _myService = myService;
        }

        [BindProperty]
        public MyData MyData { get; set; }

        public void OnGet()
        {
            MyData = _myService.GetBaseData();
        }

        public IActionResult OnPost()
        {

            // 將所有 string 為 null 的欄位預設為空字串
            MyData.Name ??= "";
            MyData.Name_short ??= "";
            MyData.Phone ??= "";
            MyData.Email ??= "";
            MyData.Line ??= "";
            MyData.WebURL ??= "";
            MyData.LineBotURL ??= "";
            MyData.Fb_Url ??= "";
            MyData.Ig_Url ??= "";
            MyData.Yt_Url ??= "";
            MyData.Tk_Url ??= "";
            MyData.Line_Url ??= "";

            MyData.Msg_BindOk ??= "";
            MyData.PageTitle ??= "";
            MyData.HeroTitle ??= "";

            MyData.Section1_Title ??= "";
            MyData.Section1_Paragraph1 ??= "";
            MyData.Section1_Paragraph2 ??= "";

            MyData.Section2_Title ??= "";
            MyData.Section2_Paragraph1 ??= "";
            MyData.Section2_Paragraph2 ??= "";

            MyData.Section3_Title ??= "";
            MyData.Section3_Item1 ??= "";
            MyData.Section3_Item2 ??= "";
            MyData.Section3_Item3 ??= "";
            MyData.Section3_Item4 ??= "";


            try
            {
                var success = _myService.UpdateBaseData(MyData);
                if (success)
                {
                    TempData["SuccessMessage"] = "資料已成功更新";
                }
                else
                {
                    TempData["ErrorMessage"] = "資料更新失敗，請稍後再試";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"發生錯誤：{ex.Message}";
            }

            return RedirectToPage(); // 重新整理頁面會清空 BindProperty
        }

    }



}
