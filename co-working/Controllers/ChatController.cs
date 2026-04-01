using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace co_working.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly HttpClient _http;
        private readonly ILogger<ChatController> _logger;

        private static readonly string SystemPrompt = @"
You are the friendly AI assistant for Brew & Work — a specialty cafe and co-working space in Belagavi, Karnataka, India.
Your job is to help visitors with questions about the menu, pricing, co-working plans, timings, location, and amenities.
Keep answers concise, warm, and helpful. If something isn't listed below, say you're not sure and suggest they call or visit.

--- CAFE MENU (100% Pure Vegetarian, Handcrafted with love) ---

HOT BEVERAGES:
- Espresso: ₹130
- Americano: ₹130
- Cappuccino: ₹180 / ₹220
- Machiatto: ₹180
- Latte: ₹220
- Flat White: ₹220
- Mocha: ₹220
- Hot Chocolate: ₹180
- Hibiscus Rose / Mint Tea: ₹150
- Matcha Classic: ₹180
- Matcha Strawberry: ₹220
Add-ons: Vanilla / Caramel / Hazelnut / Whipped Cream +₹50

COLD BEVERAGES:
- Iced Americano: ₹180
- Iced Cappuccino: ₹180
- Iced Latte: ₹180
- Iced Flat White: ₹220
- Iced Mocha: ₹220
- Iced Machiatto: ₹220
- Iced Frappuccino: ₹220
- Cold Brew: ₹180
- Lemonade: ₹180
- Peach Coconut Refresher: ₹180
- Valencia Orange Juice: ₹220
Add-ons: Oat / Almond Milk +₹60, Extra Shot +₹60

FOOD:
- Chilli Cheese Toast: ₹170
- Marinara: ₹140
- Corn Sandwich: ₹150
- Chutney Sandwich: ₹150
- Korean Bun: ₹150
- Fresh Tomato Basil Soup: ₹150
- French Fries: ₹150
- Nachos & Salsa: ₹120

CHEF'S SPECIALS:
- Mediterranean Bowl (Barley, pickled veggies, hummus, seasonal produce): ₹220
- Granola Bowl (In-house granola, nuts, oats, honey, greek yoghurt, seasonal fruits): ₹280
- Penne Arrabiata (Pasta with home made tomato basil sauce): ₹220
- Aglio e Olio (Garlic, butter, chilli, parmesan): ₹220
- Falafel Burger (Crispy falafel with mediterranean flavours): ₹220
- Chia Seed Pudding (Chia seeds soaked in coconut milk and fruits): ₹180
- Cubano (Bold espresso with Cuban-style twist): ₹150
- Affogato (Espresso poured over ice cream): ₹180

--- CO-WORKING PLANS ---

HOT DESKS (Flexible, no long-term commitment):
- Day Pass: ₹750
- Week Pass: ₹2,500
- Month Pass: ₹7,500

PRIVATE OFFICES (Lockable, plug-and-play for teams):
- 4 Members: ₹20,000/month
- 6 Members: ₹25,000/month
- 8 Members: ₹30,000/month

--- CO-WORKING AMENITIES ---
- Air Conditioning
- High Speed WiFi (fiber internet)
- Daily Housekeeping
- Coffee & Tea included
- Printing Service available (at a fee)

--- CAFE AMENITIES ---
- Free WiFi
- Cozy Seating (sofas, armchairs, window seats)
- Natural Light (large windows)
- Photo-friendly aesthetic space
- Book Corner

--- TIMINGS ---
- Cafe: 8 AM – 10 PM (daily)
- Co-Working: 24/7

--- LOCATION & CONTACT ---
- Address: Bauxite Road, Valbhay Nagar, Belagavi, Karnataka
- Phone: +91 99225 57733

--- IMPORTANT NOTES ---
- The menu is 100% vegetarian.
- Black coffee is available as Americano (₹130 hot, ₹180 iced) — closest to a plain black coffee.
- Espresso is the purest black coffee option at ₹130.
- For co-working enquiries or bookings, visitors can fill the contact form on the website or call directly.
";

        public ChatController(IHttpClientFactory httpClientFactory, ILogger<ChatController> logger)
        {
            _http = httpClientFactory.CreateClient();
            _logger = logger;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
                return BadRequest(new { error = "Message is required." });

            var baseUrl = Environment.GetEnvironmentVariable("AI_INTEGRATIONS_OPENAI_BASE_URL");
            var apiKey  = Environment.GetEnvironmentVariable("AI_INTEGRATIONS_OPENAI_API_KEY");

            if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(apiKey))
            {
                _logger.LogError("AI_INTEGRATIONS_OPENAI_BASE_URL or AI_INTEGRATIONS_OPENAI_API_KEY not set.");
                return StatusCode(503, new { error = "AI service is not configured." });
            }

            var messages = new List<object>
            {
                new { role = "system", content = SystemPrompt }
            };

            if (request.History != null)
            {
                foreach (var h in request.History)
                    messages.Add(new { role = h.Role, content = h.Content });
            }

            messages.Add(new { role = "user", content = request.Message });

            var payload = new
            {
                model = "gpt-5-mini",
                messages,
                max_completion_tokens = 512
            };

            var url = baseUrl.TrimEnd('/') + "/chat/completions";

            var httpReq = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                )
            };
            httpReq.Headers.Add("Authorization", $"Bearer {apiKey}");

            HttpResponseMessage res;
            try
            {
                res = await _http.SendAsync(httpReq);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call AI API");
                return StatusCode(500, new { error = "Could not reach AI service." });
            }

            var body = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("AI API error {Status}: {Body}", res.StatusCode, body);
                return StatusCode(500, new { error = "AI service returned an error." });
            }

            using var doc = JsonDocument.Parse(body);
            var reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Sorry, I couldn't generate a response.";

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
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }
}
