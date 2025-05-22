using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class LineMessageSender
{
    private readonly HttpClient httpClient;
    private const string LineMessagingApiUrl = "https://api.line.me/v2/bot/message/push";
    private readonly string channelAccessToken = "ESAVvIKbHra6txvAg897oc7ceSBgUgn7Gc35o4LPyfkYKIZg6JehkyXsTacCmDPAj1tsKCnaZSrgZJyT5ntx7fWRTPQdDdXEgAg6vfWFPYMioQw7Af0Ef0eUbxEa4/DLEbOfeA7rmDQQhallAehYZAdB04t89/1O/w1cDnyilFU=";
    private string NotifyResult;
    private readonly string NotifyToken = "0yvzRfyx8Z0tD9M2eTywA4jG6t8YCRlyXnuuhu3La42";
    // 發送訊息的範例
    // string Token = "ESAVvIKbHra6txvAg897oc7ceSBgUgn7Gc35o4LPyfkYKIZg6JehkyXsTacCmDPAj1tsKCnaZSrgZJyT5ntx7fWRTPQdDdXEgAg6vfWFPYMioQw7Af0Ef0eUbxEa4/DLEbOfeA7rmDQQhallAehYZAdB04t89/1O/w1cDnyilFU=";
    // LineMessageSender lineMessageSender = new LineMessageSender(Token);
    // lineMessageSender.SendMessage("Udf96f6192a32d72329c908a69805aa9e", "測試訊息");
    public LineMessageSender()
    {
        httpClient = new HttpClient();
    }
    public async Task SendMessage(string userId, string message)//PushMsg
    {
        var request = new HttpRequestMessage(HttpMethod.Post, LineMessagingApiUrl);
        request.Headers.Add("Authorization", $"Bearer {channelAccessToken}");

        var content = new StringContent(
            $"{{ \"to\": \"{userId}\", \"messages\": [{{ \"type\": \"text\", \"text\": \"{message}\" }}] }}",
            Encoding.UTF8,
            "application/json");

        request.Content = content;

        var response = await httpClient.SendAsync(request);

        // 檢查回應狀態碼
        if (!response.IsSuccessStatusCode)
        {
            System.Diagnostics.Debug.Print("Error");
            // 處理失敗的情況
            // ...
        }
    }
    public async Task SendMessage_Notify(string Token ,string Message)//Line Notify發訊息
    {
        if (!string.IsNullOrWhiteSpace(Message))
        {
            var accessToken = Token; // 替换为您的 Line Notify 访问令牌

            using (var httpClient = new HttpClient())
            {
                var content = new StringContent($"message={Message}", Encoding.UTF8, "application/x-www-form-urlencoded");
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                var response = await httpClient.PostAsync("https://notify-api.line.me/api/notify", content);

                if (response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.Print("通知已成功发送！");
                }
                else
                {
                    System.Diagnostics.Debug.Print("通知发送失败，请检查您的访问令牌和消息。");
                }
            }
        }
    }
}
