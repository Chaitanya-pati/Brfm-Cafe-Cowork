using co_working.Services;
using Microsoft.AspNetCore.Mvc;

namespace co_working.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IEmailService emailService, ILogger<ContactController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ContactFormData form)
        {
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
