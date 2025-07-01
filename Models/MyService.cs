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
            int maxRetries = 10;
            int delaySeconds = 3;
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    if (!_memoryCache.TryGetValue("BaseData", out MyData cachedData))
                    {
                        var sql = "SELECT * FROM mytb LIMIT 1";
                        cachedData = _dbConnection.QueryFirstOrDefault<MyData>(sql);

                        if (cachedData != null)
                        {
                            // ✅ 同步寫入 My 靜態類別
                            My.Id = cachedData.Id;
                            My.Name = cachedData.Name;
                            My.Name_short = cachedData.Name_short;
                            My.Phone = cachedData.Phone;
                            My.Email = cachedData.Email;
                            My.Line = cachedData.Line;
                            My.WebURL = cachedData.WebURL;
                            My.LineBotURL = cachedData.LineBotURL;
                            My.Fb_Url = cachedData.Fb_Url;
                            My.Ig_Url = cachedData.Ig_Url;
                            My.Yt_Url = cachedData.Yt_Url;
                            My.Tk_Url = cachedData.Tk_Url;
                            My.Line_Url = cachedData.Line_Url;
                            My.CreateOrderSandLineMsgSw = cachedData.CreateOrderSandLineMsgSw;
                            My.CancelSandLineMsgSw = cachedData.CancelSandLineMsgSw;
                            My.Max_Order_Oneday = cachedData.Max_Order_Oneday;
                            My.Max_Reg_Oneday = cachedData.Max_Reg_Oneday;
                            My.CancelLimitHours = cachedData.CancelLimitHours;
                            My.Msg_BindOk = cachedData.Msg_BindOk;
                            My.PageTitle = cachedData.PageTitle;
                            My.HeroTitle = cachedData.HeroTitle;
                            My.Section1_Title = cachedData.Section1_Title;
                            My.Section1_Paragraph1 = cachedData.Section1_Paragraph1;
                            My.Section1_Paragraph2 = cachedData.Section1_Paragraph2;
                            My.Section2_Title = cachedData.Section2_Title;
                            My.Section2_Paragraph1 = cachedData.Section2_Paragraph1;
                            My.Section2_Paragraph2 = cachedData.Section2_Paragraph2;
                            My.Section3_Title = cachedData.Section3_Title;
                            My.Section3_Item1 = cachedData.Section3_Item1;
                            My.Section3_Item2 = cachedData.Section3_Item2;
                            My.Section3_Item3 = cachedData.Section3_Item3;
                            My.Section3_Item4 = cachedData.Section3_Item4;
                            My.OpenTime = cachedData.OpenTime;
                            My.CloseTime = cachedData.CloseTime;

                            _memoryCache.Set("BaseData", cachedData, TimeSpan.FromMinutes(10));
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ 第 {attempt + 1} 次查無資料，將重試...");
                        }

                        return cachedData;
                    }
                    else
                    {
                        return cachedData; // 快取命中
                    }
                }
                catch (Exception ex)
                {
                    attempt++;
                    Console.WriteLine($"❌ GetBaseData 連線失敗：{ex.Message}（第 {attempt} 次）");

                    if (attempt >= maxRetries)
                        throw new Exception("GetBaseData：無法成功從 mytb 讀取資料，請確認資料庫是否正常。");

                    Thread.Sleep(delaySeconds * 1000); // 等待重試
                }
            }

            // 安全備援
            throw new Exception("GetBaseData：重試失敗");
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
    Section3_Item4 = @AboutPage_Section3_Items_3,
    , OpenTime = @OpenTime
    , CloseTime = @CloseTime

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
                AboutPage_Section3_Items_3 = my.Section3_Item4,
                OpenTime = my.OpenTime,
                CloseTime = my.CloseTime
            };
             
            var result = _dbConnection.Execute(sql, param);
            _memoryCache.Remove("BaseData");
            return result > 0;
        }
    }
}
