using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace co_working.Services
{
    public class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public string Username { get; set; } = "";
        public string FromName { get; set; } = "";
        public string ToEmail { get; set; } = "";
    }

    public class ContactFormData
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Interest { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public interface IEmailService
    {
        Task SendContactEmailsAsync(ContactFormData form);
    }

    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _smtp;
        private readonly string _password;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _smtp = config.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
            _password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
            _logger = logger;
        }

        public async Task SendContactEmailsAsync(ContactFormData form)
        {
            await SendInternalNotificationAsync(form);
            await SendConfirmationToSenderAsync(form);
        }

        private async Task SendInternalNotificationAsync(ContactFormData form)
        {
            var interestLabel = form.Interest switch
            {
                "hot-desk" => "Hot Desk",
                "private-office" => "Private Office",
                "cafe" => "Cafe Services",
                "both" => "Cafe & Co-Working",
                _ => form.Interest
            };

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.Username));
            message.To.Add(new MailboxAddress("Brew & Work", _smtp.ToEmail));
            message.ReplyTo.Add(new MailboxAddress(form.Name, form.Email));
            message.Subject = $"New Enquiry from {form.Name} — {interestLabel}";

            var body = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; padding: 0; }}
    .wrapper {{ max-width: 560px; margin: 32px auto; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }}
    .header {{ background: #111; padding: 28px 32px; }}
    .header h1 {{ color: #fff; font-size: 20px; margin: 0; letter-spacing: -0.3px; }}
    .header p {{ color: rgba(255,255,255,0.5); font-size: 13px; margin: 6px 0 0; }}
    .body {{ padding: 28px 32px; }}
    .field {{ margin-bottom: 20px; }}
    .label {{ font-size: 11px; font-weight: 700; color: #999; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 4px; }}
    .value {{ font-size: 15px; color: #222; line-height: 1.5; }}
    .message-box {{ background: #f9f9f7; border-left: 3px solid #111; padding: 14px 16px; border-radius: 0 4px 4px 0; font-size: 14px; color: #444; line-height: 1.7; }}
    .footer {{ border-top: 1px solid #eee; padding: 16px 32px; font-size: 12px; color: #aaa; text-align: center; }}
  </style>
</head>
<body>
  <div class='wrapper'>
    <div class='header'>
      <h1>&#9827; New Enquiry</h1>
      <p>Submitted via the Brew &amp; Work website</p>
    </div>
    <div class='body'>
      <div class='field'>
        <div class='label'>Name</div>
        <div class='value'>{System.Web.HttpUtility.HtmlEncode(form.Name)}</div>
      </div>
      <div class='field'>
        <div class='label'>Email</div>
        <div class='value'><a href='mailto:{System.Web.HttpUtility.HtmlEncode(form.Email)}'>{System.Web.HttpUtility.HtmlEncode(form.Email)}</a></div>
      </div>
      {(string.IsNullOrWhiteSpace(form.Phone) ? "" : $@"
      <div class='field'>
        <div class='label'>Phone</div>
        <div class='value'>{System.Web.HttpUtility.HtmlEncode(form.Phone)}</div>
      </div>")}
      <div class='field'>
        <div class='label'>Interested In</div>
        <div class='value'>{System.Web.HttpUtility.HtmlEncode(interestLabel)}</div>
      </div>
      {(string.IsNullOrWhiteSpace(form.Message) ? "" : $@"
      <div class='field'>
        <div class='label'>Message</div>
        <div class='message-box'>{System.Web.HttpUtility.HtmlEncode(form.Message).Replace("\n", "<br>")}</div>
      </div>")}
    </div>
    <div class='footer'>Reply directly to this email to respond to {System.Web.HttpUtility.HtmlEncode(form.Name)}.</div>
  </div>
</body>
</html>"
            };

            message.Body = body.ToMessageBody();
            await SendAsync(message);
        }

        private async Task SendConfirmationToSenderAsync(ContactFormData form)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.Username));
            message.To.Add(new MailboxAddress(form.Name, form.Email));
            message.Subject = "We received your message — Brew & Work";

            var body = new BodyBuilder
            {
                HtmlBody = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background: #f4f4f4; margin: 0; padding: 0; }}
    .wrapper {{ max-width: 560px; margin: 32px auto; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 12px rgba(0,0,0,0.08); }}
    .header {{ background: #111; padding: 32px; text-align: center; }}
    .header h1 {{ color: #fff; font-size: 22px; margin: 0 0 6px; letter-spacing: -0.3px; }}
    .header p {{ color: rgba(255,255,255,0.5); font-size: 13px; margin: 0; }}
    .body {{ padding: 32px; }}
    .greeting {{ font-size: 17px; color: #222; margin: 0 0 16px; }}
    .text {{ font-size: 14px; color: #555; line-height: 1.8; margin: 0 0 16px; }}
    .highlight {{ background: #f9f9f7; border-radius: 6px; padding: 18px 20px; margin: 24px 0; }}
    .highlight p {{ font-size: 13px; color: #666; margin: 0 0 4px; }}
    .highlight p:last-child {{ margin: 0; }}
    .highlight strong {{ color: #222; }}
    .divider {{ border: none; border-top: 1px solid #eee; margin: 24px 0; }}
    .contact {{ font-size: 13px; color: #888; }}
    .contact a {{ color: #111; }}
    .footer {{ background: #f9f9f7; padding: 20px 32px; text-align: center; font-size: 12px; color: #bbb; }}
    .logo {{ font-size: 18px; color: #fff; }}
  </style>
</head>
<body>
  <div class='wrapper'>
    <div class='header'>
      <div class='logo'>&#9827;</div>
      <h1>Brew &amp; Work</h1>
      <p>Belagavi, Karnataka</p>
    </div>
    <div class='body'>
      <p class='greeting'>Hi {System.Web.HttpUtility.HtmlEncode(form.Name)},</p>
      <p class='text'>Thank you for reaching out! We have received your message and our team will get back to you within <strong>24 hours</strong>.</p>
      <div class='highlight'>
        <p><strong>Your enquiry summary</strong></p>
        <p>Interest: <strong>{System.Web.HttpUtility.HtmlEncode(form.Interest switch {
            "hot-desk" => "Hot Desk",
            "private-office" => "Private Office",
            "cafe" => "Cafe Services",
            "both" => "Cafe & Co-Working",
            _ => form.Interest
        })}</strong></p>
        {(string.IsNullOrWhiteSpace(form.Message) ? "" : $"<p>Message: <em>{System.Web.HttpUtility.HtmlEncode(form.Message.Length > 120 ? form.Message[..120] + "…" : form.Message)}</em></p>")}
      </div>
      <p class='text'>In the meantime, feel free to visit us or call us directly:</p>
      <hr class='divider'>
      <p class='contact'>
        &#128205; Bauxite Road, Valbhay Nagar, Belagavi, Karnataka<br>
        &#128222; <a href='tel:+919922557733'>+91 99225 57733</a><br>
        &#9749; Cafe: 8 AM – 10 PM &nbsp;|&nbsp; Co-Working: 24/7
      </p>
    </div>
    <div class='footer'>You're receiving this because you contacted Brew &amp; Work via our website.</div>
  </div>
</body>
</html>"
            };

            message.Body = body.ToMessageBody();
            await SendAsync(message);
        }

        private async Task SendAsync(MimeMessage message)
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_smtp.Username, _password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
