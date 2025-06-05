using Microsoft.AspNetCore.Mvc;

namespace Web0524.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailVerificationService _emailService;

        public EmailController(IEmailVerificationService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public IActionResult Send([FromForm] string email)
        {
            if (!_emailService.SendVerificationCode(email))
                return BadRequest("寄送失敗或達上限");

            return Ok(new { message = "驗證碼已發送" });
        }
    }
}

