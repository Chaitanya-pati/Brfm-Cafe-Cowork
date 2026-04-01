using co_working.Services;
using Microsoft.AspNetCore.Mvc;

namespace co_working.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chat;

        public ChatController(IChatService chat)
        {
            _chat = chat;
        }

        [HttpPost("send")]
        public IActionResult Send([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest(new { error = "Message is required." });

            var history = request.History?
                .Select(h => new ChatTurn { Role = h.Role, Content = h.Content })
                .ToList();

            var reply = _chat.GetReply(request.Message, history);
            return Ok(new { reply });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public List<ChatHistoryItem>? History { get; set; }
    }

    public class ChatHistoryItem
    {
        public string Role    { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
