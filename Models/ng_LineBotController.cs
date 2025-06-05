using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/linebot")]
public class ng_LineBotController : ControllerBase
{
    private readonly ILogger<ng_LineBotController> _logger;
    private readonly ng_LineMessageSender _lineMessageSender;

    public ng_LineBotController(ILogger<ng_LineBotController> logger)
    {
        _logger = logger;
        string channelAccessToken = "ESAVvIKbHra6txvAg897oc7ceSBgUgn7Gc35o4LPyfkYKIZg6JehkyXsTacCmDPAj1tsKCnaZSrgZJyT5ntx7fWRTPQdDdXEgAg6vfWFPYMioQw7Af0Ef0eUbxEa4/DLEbOfeA7rmDQQhallAehYZAdB04t89/1O/w1cDnyilFU=";
        _lineMessageSender = new ng_LineMessageSender();
    }

    [HttpPost]
    public IActionResult HandleLineBotMessage([FromBody] List<LineBotEvent> events)
    {
        System.Diagnostics.Debug.Print("124");

        // 這個範例假設只有一個事件 (event)
        if (events != null && events.Count > 0)
        {
            var userId = events[0].Source.UserId;
            // 這裡可以使用 userId 進行後續處理
            // ...

            // 假設您要回覆使用者訊息
            _lineMessageSender.SendMessage(userId, "收到訊息了，謝謝！");

            return Ok(); // 回傳成功的回應
        }
        else
        {
            return BadRequest(); // 回應錯誤的回應
        }
    }
}

public class LineBotEvent
{
    public LineBotEventSource Source { get; set; }
    // 可以加入其他相關屬性
}

public class LineBotEventSource
{
    public string UserId { get; set; }
    // 可以加入其他相關屬性
}
