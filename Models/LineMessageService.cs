using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Web0524.Models
{

    public class LineMessageService
    {
        private readonly HttpClient _httpClient;

        public LineMessageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<bool> SendSecureLineMessageAsync(string lineUserId, string message)
        {
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


            // 也可以回傳 status code 給呼叫者（若你要用）
            // return (int)statusCode;

            return response.IsSuccessStatusCode;
        }

    }

}



