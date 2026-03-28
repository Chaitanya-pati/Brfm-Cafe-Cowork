using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

namespace co_working.Services
{
    public class GraphSettings
    {
        public string TenantId { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string SenderEmail { get; set; } = "";
        public string ToEmail { get; set; } = "";
        public string FromName { get; set; } = "";
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
        private readonly GraphSettings _graph;
        private readonly string _clientSecret;
        private readonly HttpClient _http;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<EmailService> logger)
        {
            _graph = config.GetSection("Graph").Get<GraphSettings>() ?? new GraphSettings();
            _graph.TenantId = Environment.GetEnvironmentVariable("GRAPH_TENANT_ID") ?? "";
            _graph.ClientId = Environment.GetEnvironmentVariable("GRAPH_CLIENT_ID") ?? "";
            _clientSecret   = Environment.GetEnvironmentVariable("GRAPH_CLIENT_SECRET") ?? "";
            _http = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task SendContactEmailsAsync(ContactFormData form)
        {
            var token = await GetAccessTokenAsync();
            await SendInternalNotificationAsync(form, token);
            await SendConfirmationToSenderAsync(form, token);
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var url = $"https://login.microsoftonline.com/{_graph.TenantId}/oauth2/v2.0/token";

            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "client_credentials",
                ["client_id"]     = _graph.ClientId,
                ["client_secret"] = _clientSecret,
                ["scope"]         = "https://graph.microsoft.com/.default"
            });

            var res = await _http.PostAsync(url, body);
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
                throw new Exception($"Token request failed: {json}");

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString()
                   ?? throw new Exception("access_token missing from response");
        }

        private async Task SendInternalNotificationAsync(ContactFormData form, string token)
        {
            var interestLabel = GetInterestLabel(form.Interest);

            var phoneRow = string.IsNullOrWhiteSpace(form.Phone) ? "" : $@"
      <div class='field'>
        <div class='label'>Phone</div>
        <div class='value'>{HttpUtility.HtmlEncode(form.Phone)}</div>
      </div>";

            var messageRow = string.IsNullOrWhiteSpace(form.Message) ? "" : $@"
      <div class='field'>
        <div class='label'>Message</div>
        <div class='message-box'>{HttpUtility.HtmlEncode(form.Message).Replace("\n", "<br>")}</div>
      </div>";

            var html = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body{{font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0}}
    .wrapper{{max-width:560px;margin:32px auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,.08)}}
    .header{{background:#111;padding:28px 32px}}
    .header h1{{color:#fff;font-size:20px;margin:0;letter-spacing:-.3px}}
    .header p{{color:rgba(255,255,255,.5);font-size:13px;margin:6px 0 0}}
    .body{{padding:28px 32px}}
    .field{{margin-bottom:20px}}
    .label{{font-size:11px;font-weight:700;color:#999;text-transform:uppercase;letter-spacing:1px;margin-bottom:4px}}
    .value{{font-size:15px;color:#222;line-height:1.5}}
    .message-box{{background:#f9f9f7;border-left:3px solid #111;padding:14px 16px;border-radius:0 4px 4px 0;font-size:14px;color:#444;line-height:1.7}}
    .footer{{border-top:1px solid #eee;padding:16px 32px;font-size:12px;color:#aaa;text-align:center}}
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
        <div class='value'>{HttpUtility.HtmlEncode(form.Name)}</div>
      </div>
      <div class='field'>
        <div class='label'>Email</div>
        <div class='value'><a href='mailto:{HttpUtility.HtmlEncode(form.Email)}'>{HttpUtility.HtmlEncode(form.Email)}</a></div>
      </div>
      {phoneRow}
      <div class='field'>
        <div class='label'>Interested In</div>
        <div class='value'>{HttpUtility.HtmlEncode(interestLabel)}</div>
      </div>
      {messageRow}
    </div>
    <div class='footer'>Reply directly to this email to respond to {HttpUtility.HtmlEncode(form.Name)}.</div>
  </div>
</body>
</html>";

            var payload = BuildMailPayload(
                toName: "Brew & Work",
                toEmail: _graph.ToEmail,
                subject: $"New Enquiry from {form.Name} — {interestLabel}",
                htmlBody: html,
                replyToName: form.Name,
                replyToEmail: form.Email
            );

            await SendViaGraphAsync(token, payload);
        }

        private async Task SendConfirmationToSenderAsync(ContactFormData form, string token)
        {
            var interestLabel = GetInterestLabel(form.Interest);
            var msgPreview = string.IsNullOrWhiteSpace(form.Message)
                ? ""
                : $"<p>Message: <em>{HttpUtility.HtmlEncode(form.Message.Length > 120 ? form.Message[..120] + "…" : form.Message)}</em></p>";

            var html = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body{{font-family:Arial,sans-serif;background:#f4f4f4;margin:0;padding:0}}
    .wrapper{{max-width:560px;margin:32px auto;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,.08)}}
    .header{{background:#111;padding:32px;text-align:center}}
    .header .logo{{font-size:18px;color:#fff}}
    .header h1{{color:#fff;font-size:22px;margin:8px 0 6px;letter-spacing:-.3px}}
    .header p{{color:rgba(255,255,255,.5);font-size:13px;margin:0}}
    .body{{padding:32px}}
    .greeting{{font-size:17px;color:#222;margin:0 0 16px}}
    .text{{font-size:14px;color:#555;line-height:1.8;margin:0 0 16px}}
    .highlight{{background:#f9f9f7;border-radius:6px;padding:18px 20px;margin:24px 0}}
    .highlight p{{font-size:13px;color:#666;margin:0 0 4px}}
    .highlight p:last-child{{margin:0}}
    .highlight strong{{color:#222}}
    hr{{border:none;border-top:1px solid #eee;margin:24px 0}}
    .contact{{font-size:13px;color:#888}}
    .contact a{{color:#111}}
    .footer{{background:#f9f9f7;padding:20px 32px;text-align:center;font-size:12px;color:#bbb}}
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
      <p class='greeting'>Hi {HttpUtility.HtmlEncode(form.Name)},</p>
      <p class='text'>Thank you for reaching out! We have received your message and our team will get back to you within <strong>24 hours</strong>.</p>
      <div class='highlight'>
        <p><strong>Your enquiry summary</strong></p>
        <p>Interest: <strong>{HttpUtility.HtmlEncode(interestLabel)}</strong></p>
        {msgPreview}
      </div>
      <p class='text'>In the meantime, feel free to visit us or call us directly:</p>
      <hr>
      <p class='contact'>
        &#128205; Bauxite Road, Valbhay Nagar, Belagavi, Karnataka<br>
        &#128222; <a href='tel:+919922557733'>+91 99225 57733</a><br>
        &#9749; Cafe: 8 AM – 10 PM &nbsp;|&nbsp; Co-Working: 24/7
      </p>
    </div>
    <div class='footer'>You're receiving this because you contacted Brew &amp; Work via our website.</div>
  </div>
</body>
</html>";

            var payload = BuildMailPayload(
                toName: form.Name,
                toEmail: form.Email,
                subject: "We received your message — Brew & Work",
                htmlBody: html
            );

            await SendViaGraphAsync(token, payload);
        }

        private object BuildMailPayload(
            string toName, string toEmail,
            string subject, string htmlBody,
            string? replyToName = null, string? replyToEmail = null)
        {
            var replyTo = (replyToName != null && replyToEmail != null)
                ? new[] { new { emailAddress = new { name = replyToName, address = replyToEmail } } }
                : null;

            return new
            {
                message = new
                {
                    subject,
                    body = new { contentType = "HTML", content = htmlBody },
                    toRecipients = new[]
                    {
                        new { emailAddress = new { name = toName, address = toEmail } }
                    },
                    replyTo,
                    from = new { emailAddress = new { name = _graph.FromName, address = _graph.SenderEmail } }
                },
                saveToSentItems = false
            };
        }

        private async Task SendViaGraphAsync(string token, object payload)
        {
            var url = $"https://graph.microsoft.com/v1.0/users/{_graph.SenderEmail}/sendMail";
            var json = JsonSerializer.Serialize(payload);
            var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var res = await _http.SendAsync(req);
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                throw new Exception($"Graph sendMail failed ({res.StatusCode}): {body}");
            }
        }

        private static string GetInterestLabel(string interest) => interest switch
        {
            "hot-desk"       => "Hot Desk",
            "private-office" => "Private Office",
            "cafe"           => "Cafe Services",
            "both"           => "Cafe & Co-Working",
            _                => interest
        };
    }
}
