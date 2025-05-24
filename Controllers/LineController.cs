using MDP.DevKit.LineMessaging;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text;
using Web0524.Models;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace WebApplication1
{
    public partial class LineController : Controller
    {
        // Fields                
        private readonly IUserService _userService;
        private readonly LineMessageContext _lineMessageContext;

        // Constructors
        public LineController(LineMessageContext lineMessageContext, IUserService userService)
        {
            #region Contracts

            if (lineMessageContext == null) throw new ArgumentException($"{nameof(lineMessageContext)}=null");

            #endregion

            // Default
            _lineMessageContext = lineMessageContext;
            _userService = userService;
        }
        // Methods

        [HttpPost]
        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("/Hook-Line", Name = "Hook-Line")]

        public async Task<ActionResult> Hook() //監聽器
        {

            try
            {
                // Content
                var content = string.Empty;
                using (var reader = new StreamReader(this.Request.Body))
                {
                    content = await reader.ReadToEndAsync();
                }
                if (string.IsNullOrEmpty(content) == true) return this.BadRequest();

                // Signature 
                var signature = string.Empty;
                if (this.Request.Headers.TryGetValue("X-Line-Signature", out var signatureHeader) == true)
                {
                    signature = signatureHeader.FirstOrDefault();
                }
                if (string.IsNullOrEmpty(signature) == true) return this.BadRequest();

                string userid="";
                string text="";

                // HandleHook
                var eventList = _lineMessageContext.HandleHook(content, signature);

                if (eventList == null) return this.BadRequest();
                foreach (var lineEvent in eventList)
                {
                    if (lineEvent.Source is UserSource userSource)
                    {
                        userid = userSource.UserId;
                    }
                    if (lineEvent is TextMessageEvent textMessageEvent)
                    {
                        text = textMessageEvent.Text;
                    }
                }
                // Display
                var serializerSettings = new JsonSerializerSettings();
                {
                    serializerSettings.Converters.Add(new StringEnumConverter());
                }
                Console.WriteLine("UserId: " + userid);
                Console.WriteLine("text: " + text);
                //1.當使用者輸入
                string prefix = "我要綁定";
                if (text.StartsWith(prefix))
                {
                    string uid = text.Substring(prefix.Length);
                    Web0524.Models.User user = new Web0524.Models.User();
                    user = _userService.GetUserById(uid);
                    if (user != null)
                    {
                        //_userService.UpdateUser_LineUserId(user.Id, userid);


                        // Variables
                        var message = new TextMessage() { Text = "綁定成功!\n我們會將透過這個官方帳號通知您預約狀況與預約提醒。" };
                        // PushMessage
                        var result = await _lineMessageContext.MessageService.PushMessageAsync(message, userid);
                    }
                    else
                    {
                        // Variables
                        var message = new TextMessage() { Text = "查無此會員帳號，請重新確認。" };
                        // PushMessage
                        var result = await _lineMessageContext.MessageService.PushMessageAsync(message, userid);
                    }
                    
                }
                else
                {
                    Console.WriteLine("不處理。");
                }

                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(eventList, Newtonsoft.Json.Formatting.Indented, serializerSettings));
                Console.WriteLine();


                // Return
                return this.Ok();
            }
            catch (Exception ex)
            {
                // Display
                //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ex, Newtonsoft.Json.Formatting.Indented));
                //Console.WriteLine();
                return this.Ok();
            }
        }

    }
}
