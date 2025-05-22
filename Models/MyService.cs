using Dapper;
using Microsoft.Extensions.Caching.Memory; // 引入内存缓存命名空间
using System;
using System.Data;

namespace Web0524.Models
{
    public interface IMyService
    {
        My GetBaseData();
        bool UpdateBaseData(My my);
    }

    public class MyService : IMyService
    {
        private readonly IDbConnection _dbConnection;
        private readonly IMemoryCache _memoryCache; // 注入内存缓存

        public MyService(IDbConnection dbConnection, IMemoryCache memoryCache)
        {
            _dbConnection = dbConnection;
            _memoryCache = memoryCache;
        }

        public My GetBaseData()
        {
            // 尝试从缓存中获取数据
            if (!_memoryCache.TryGetValue("BaseData", out My cachedData))
            {           // 如果缓存中没有数据，从数据库获取数据
                var sql = "SELECT TOP 1 * from BaseData";
                cachedData = _dbConnection.QueryFirstOrDefault<My>(sql);

                // 将数据存入缓存，设置过期时间（例如，10分钟）
                if (cachedData != null)
                {
                    _memoryCache.Set("BaseData", cachedData, TimeSpan.FromMinutes(10));
                }
            }

            return cachedData;
        }
        public bool UpdateBaseData(My my)
        {
            var sql = "UPDATE BaseData SET Fb_Url=@Fb_Url,Ig_Url=@Ig_Url,Yt_Url=@Yt_Url,Tk_Url=@Tk_Url,Line_Url=@Line_Url,Name_short=@Name_short,Name=@Name,Phone=@Phone,Email=@Email,WebURL=@WebURL,LineBotURL=@LineBotURL";
            var affectedRows = _dbConnection.Execute(sql, my);
            return affectedRows > 0;
        }
    }
}
