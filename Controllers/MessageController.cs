using Microsoft.AspNetCore.Mvc;
using Web0524.Models.SystemMessage;

namespace Web0524.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet("GetUserMessages")]
        public IActionResult GetUserMessages([FromQuery] string userId)
        {
            var messages = _messageService.GetUserMessages(userId, onlyUnread: false);
            return Ok(messages);
        }

        [HttpPost("MarkAllMessagesRead")]
        public IActionResult MarkAllMessagesRead()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.Sid)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            _messageService.MarkAllUserMessagesAsRead(userId);
            return Ok();
        }

    }

}
