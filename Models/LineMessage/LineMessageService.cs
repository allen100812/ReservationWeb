using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Dapper;

namespace Web0524.Models.LineMessage
{
    public class LineMessageService
    {
        private readonly HttpClient _httpClient;
        private readonly IDbConnection _db;

        private const int MonthlyLimit = 300;

        public LineMessageService(HttpClient httpClient, IDbConnection db)
        {
            _httpClient = httpClient;
            _db = db;
        }

        public async Task<bool> SendSecureLineMessageAsync(string lineUserId, string message)
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            // 查詢本月已發送次數
            var count = _db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM LineMessageSendLog WHERE SentAt >= @start AND SentAt < @end",
                new { start = startOfMonth, end = endOfMonth }
            );

            if (count >= MonthlyLimit)
            {
                Console.WriteLine("本月發送上限已達，訊息不發送。");
                return false;
            }

            // 發送 LINE 訊息
            var url = "http://localhost:5678/webhook/line-secure";
            var payload = new
            {
                api_key = 1000,
                lineUserId,
                message
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Status Code: {(int)response.StatusCode} ({response.StatusCode})");
            Console.WriteLine($"Response Body: {responseString}");

            if (response.IsSuccessStatusCode)
            {
                _db.Execute(
                    "INSERT INTO LineMessageSendLog (LineUserId, Message, SentAt) VALUES (@LineUserId, @Message, @SentAt)",
                    new { LineUserId = lineUserId, Message = message, SentAt = now });
            }

            return response.IsSuccessStatusCode;
        }

        public async Task<List<MonthlySendStats>> GetMonthlySendStatsAsync(string? lineUserId = null)
        {
            var sql = @"
                SELECT 
                    DATE_FORMAT(SentAt, '%Y-%m') AS Month,
                    COUNT(*) AS Total
                FROM LineMessageSendLog
                WHERE (@LineUserId IS NULL OR LineUserId = @LineUserId)
                GROUP BY DATE_FORMAT(SentAt, '%Y-%m')
                ORDER BY Month DESC";

            var result = await _db.QueryAsync<MonthlySendStats>(sql, new { LineUserId = lineUserId });
            return result.AsList();
        }

        public class MonthlySendStats
        {
            public string Month { get; set; } = "";  // yyyy-MM
            public int Total { get; set; }
        }
    }
}
