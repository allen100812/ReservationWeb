using Dapper;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Data;

namespace Web0524.Models
{
    public interface IMyService
    {
        MyData GetBaseData();
        bool UpdateBaseData(MyData my);
    }

    public class MyService : IMyService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IMemoryCache _memoryCache;

        public MyService(IDbConnection dbConnection, IMemoryCache memoryCache)
        {
            _dbConnection = dbConnection;
            _memoryCache = memoryCache;
        }

        public MyData GetBaseData()
        {
            if (!_memoryCache.TryGetValue("BaseData", out MyData cachedData))
            {
                var sql = "SELECT * FROM mytb LIMIT 1";
                cachedData = _dbConnection.QueryFirstOrDefault<MyData>(sql);

                if (cachedData != null)
                {
                    _memoryCache.Set("BaseData", cachedData, TimeSpan.FromMinutes(10));
                }
            }

            return cachedData;
        }

        public bool UpdateBaseData(MyData my)
        {
            var sql = @"
UPDATE mytb SET 
    Fb_Url = @Fb_Url,
    Ig_Url = @Ig_Url,
    Yt_Url = @Yt_Url,
    Tk_Url = @Tk_Url,
    Line_Url = @Line_Url,
    Name_short = @Name_short,
    Name = @Name,
    Phone = @Phone,
    Email = @Email,
    Line = @Line,
    WebURL = @WebURL,
    LineBotURL = @LineBotURL,
    CreateOrderSandLineMsgSw = @CreateOrderSandLineMsgSw,
    CancelSandLineMsgSw = @CancelSandLineMsgSw,
    Max_Order_Oneday = @Max_Order_Oneday,
    Max_Reg_Oneday = @Max_Reg_Oneday,
    CancelLimitHours = @CancelLimitHours,
    Msg_BindOk = @Msg_BindOk,
    PageTitle = @AboutPage_PageTitle,
    HeroTitle = @AboutPage_HeroTitle,
    Section1_Title = @AboutPage_Section1_Title,
    Section1_Paragraph1 = @AboutPage_Section1_Paragraph1,
    Section1_Paragraph2 = @AboutPage_Section1_Paragraph2,
    Section2_Title = @AboutPage_Section2_Title,
    Section2_Paragraph1 = @AboutPage_Section2_Paragraph1,
    Section2_Paragraph2 = @AboutPage_Section2_Paragraph2,
    Section3_Title = @AboutPage_Section3_Title,
    Section3_Item1 = @AboutPage_Section3_Items_0,
    Section3_Item2 = @AboutPage_Section3_Items_1,
    Section3_Item3 = @AboutPage_Section3_Items_2,
    Section3_Item4 = @AboutPage_Section3_Items_3
WHERE Id = @Id;
";

            var param = new
            {
                my.Fb_Url,
                my.Ig_Url,
                my.Yt_Url,
                my.Tk_Url,
                my.Line_Url,
                my.Name_short,
                my.Name,
                my.Phone,
                my.Email,
                my.Line,
                my.WebURL,
                my.LineBotURL,
                my.CreateOrderSandLineMsgSw,
                my.CancelSandLineMsgSw,
                my.Max_Order_Oneday,
                my.Max_Reg_Oneday,
                my.CancelLimitHours,
                my.Msg_BindOk,
                my.Id,

                AboutPage_PageTitle = my.PageTitle,
                AboutPage_HeroTitle = my.HeroTitle,
                AboutPage_Section1_Title = my.Section1_Title,
                AboutPage_Section1_Paragraph1 = my.Section1_Paragraph1,
                AboutPage_Section1_Paragraph2 = my.Section1_Paragraph2,
                AboutPage_Section2_Title = my.Section2_Title,
                AboutPage_Section2_Paragraph1 = my.Section2_Paragraph1,
                AboutPage_Section2_Paragraph2 = my.Section2_Paragraph2,
                AboutPage_Section3_Title = my.Section3_Title,
                AboutPage_Section3_Items_0 = my.Section3_Item1,
                AboutPage_Section3_Items_1 = my.Section3_Item2,
                AboutPage_Section3_Items_2 = my.Section3_Item3,
                AboutPage_Section3_Items_3 = my.Section3_Item4
            };
             
            var result = _dbConnection.Execute(sql, param);
            _memoryCache.Remove("BaseData");
            return result > 0;
        }
    }
}
