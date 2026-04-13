using co_working.Services;
using Microsoft.AspNetCore.Mvc;

namespace co_working.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ISpamProtectionService _spam;
        private readonly ILogger<ContactController> _logger;

        private const int MinFormFillSeconds = 3;

        public ContactController(IEmailService emailService, ISpamProtectionService spam, ILogger<ContactController> logger)
        {
            _emailService = emailService;
            _spam = spam;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ContactFormData form)
        {
            if (!string.IsNullOrEmpty(form.Honeypot))
            {
                _logger.LogWarning("Honeypot triggered — bot submission blocked.");
                return Ok(new { success = true, message = "Message sent successfully." });
            }

            var elapsedSeconds = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - form.FormLoadTime) / 1000.0;
            if (form.FormLoadTime > 0 && elapsedSeconds < MinFormFillSeconds)
            {
                _logger.LogWarning("Form submitted too fast ({Seconds:F1}s) — bot submission blocked.", elapsedSeconds);
                return Ok(new { success = true, message = "Message sent successfully." });
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (_spam.IsRateLimited(ip))
            {
                _logger.LogWarning("Rate limit reached for IP {IP}", ip);
                return BadRequest(new { success = false, message = "Too many submissions. Please try again later." });
            }

            if (string.IsNullOrWhiteSpace(form.Name) ||
                string.IsNullOrWhiteSpace(form.Email) ||
                string.IsNullOrWhiteSpace(form.Interest))
            {
                return BadRequest(new { success = false, message = "Please fill in all required fields." });
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(form.Email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return BadRequest(new { success = false, message = "Please enter a valid email address." });
            }

            if (_spam.IsSpamName(form.Name))
            {
                _logger.LogWarning("Spam name detected: {Name}", form.Name);
                return BadRequest(new { success = false, message = "Please enter your real full name." });
            }

            if (!_spam.IsValidInterest(form.Interest))
            {
                _logger.LogWarning("Invalid interest value submitted: {Interest}", form.Interest);
                return BadRequest(new { success = false, message = "Please select a valid option." });
            }

            try
            {
                await _emailService.SendContactEmailsAsync(form);
                return Ok(new { success = true, message = "Message sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact email from {Email}", form.Email);
                return StatusCode(500, new { success = false, message = "Something went wrong. Please try again or contact us directly." });
            }
        }
    }
}
