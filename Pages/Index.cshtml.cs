using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.Design;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Web0524.Models;

namespace Web0524.Pages
{
     
    public class IndexModel : PageModel
    {

        private readonly IUserService _userService;
        private readonly ILogger<IndexModel> _logger;

        private readonly LineMessageContext _lineMessageContext;
        private readonly INewService _newService;

        private readonly IReservationService _reservationService;
        public IndexModel(ILogger<IndexModel> logger, IUserService userService, LineMessageContext lineMessageContext , INewService newService, IReservationService reservationService)
        {
            _logger = logger;
            _userService = userService;

            _lineMessageContext = lineMessageContext;
            _newService = newService;
            _reservationService = reservationService;

        }

        public string ReservationTestResult { get; set; } // 顯示在前端用

        public string SId { get; set; }
        public string Myname { get; set; }


        public MyData basedata { get; set; }
        public List<NewList> newLists { get; set; }

        public List<NewList> FilteredDesignNews { get; set; } = new();

        public List<NewList> FilteredDesignNews_2 { get; set; } = new();

        public async void OnGet()
        {
            if (User.FindFirst(ClaimTypes.Sid) != null)
            {
                SId = User.FindFirst(ClaimTypes.Sid).ToString();
            }

            // 取得所有新聞主檔
            newLists = _newService.GetNewTB().ToList();

            // 篩選 Tag 2~5，狀態為 1 的資料，依 TopTime 排序
            FilteredDesignNews = newLists
                .Where(n => n.Tag is 2 or 3 or 4 or 5 && n.Status == 1)
                .OrderByDescending(n => n.TopTime ?? DateTime.MinValue)
                .ToList();


            // 篩選 Tag 2~5，狀態為 1 的資料，依 TopTime 排序
            FilteredDesignNews_2 = newLists
                .Where(n => n.Tag is 1 && n.Status == 1)
                .OrderByDescending(n => n.TopTime ?? DateTime.MinValue)
                .ToList();
            // 取得所有新聞圖片
            var photoMap = _newService.GetAllNewsPhotos();

            // 將圖片配回每一筆新聞
            foreach (var news in FilteredDesignNews)
            {
                if (news.NewId != null && photoMap.TryGetValue(news.NewId.Value, out var photos))
                {
                    news.PhotoList = photos;
                }
            }
            foreach (var news in FilteredDesignNews_2)
            {
                if (news.NewId != null && photoMap.TryGetValue(news.NewId.Value, out var photos))
                {
                    news.PhotoList = photos;
                }
            }

        }










    }


}