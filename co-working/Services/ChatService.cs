using System.Text.RegularExpressions;

namespace co_working.Services
{
    public interface IChatService
    {
        string GetReply(string userMessage, List<ChatTurn>? history = null);
    }

    public class ChatTurn
    {
        public string Role    { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public class LocalChatService : IChatService
    {
        // ── Knowledge base ──────────────────────────────────────────────────

        private static readonly Dictionary<string, string> MenuPrices = new(StringComparer.OrdinalIgnoreCase)
        {
            // Hot Beverages
            ["espresso"]               = "₹130",
            ["americano"]              = "₹130 (hot) / ₹180 (iced)",
            ["cappuccino"]             = "₹180 (regular) / ₹220 (large)",
            ["machiatto"]              = "₹180",
            ["macchiato"]              = "₹180",
            ["latte"]                  = "₹220",
            ["flat white"]             = "₹220 (hot) / ₹220 (iced)",
            ["mocha"]                  = "₹220 (hot) / ₹220 (iced)",
            ["hot chocolate"]          = "₹180",
            ["hibiscus rose tea"]      = "₹150",
            ["mint tea"]               = "₹150",
            ["hibiscus tea"]           = "₹150",
            ["matcha classic"]         = "₹180",
            ["matcha strawberry"]      = "₹220",
            ["matcha"]                 = "₹180 (classic) / ₹220 (strawberry)",
            // Cold Beverages
            ["iced americano"]         = "₹180",
            ["iced cappuccino"]        = "₹180",
            ["iced latte"]             = "₹180",
            ["iced flat white"]        = "₹220",
            ["iced mocha"]             = "₹220",
            ["iced machiatto"]         = "₹220",
            ["iced macchiato"]         = "₹220",
            ["iced frappuccino"]       = "₹220",
            ["frappuccino"]            = "₹220",
            ["cold brew"]              = "₹180",
            ["lemonade"]               = "₹180",
            ["peach coconut refresher"]= "₹180",
            ["peach coconut"]          = "₹180",
            ["valencia orange juice"]  = "₹220",
            ["orange juice"]           = "₹220",
            // Food
            ["chilli cheese toast"]    = "₹170",
            ["chili cheese toast"]     = "₹170",
            ["marinara"]               = "₹140",
            ["corn sandwich"]          = "₹150",
            ["chutney sandwich"]       = "₹150",
            ["korean bun"]             = "₹150",
            ["tomato basil soup"]      = "₹150",
            ["fresh tomato basil soup"]= "₹150",
            ["french fries"]           = "₹150",
            ["fries"]                  = "₹150",
            ["nachos"]                 = "₹120",
            ["nachos & salsa"]         = "₹120",
            ["nachos and salsa"]       = "₹120",
            // Specials
            ["mediterranean bowl"]     = "₹220",
            ["granola bowl"]           = "₹280",
            ["penne arrabiata"]        = "₹220",
            ["penne arrabbiata"]       = "₹220",
            ["aglio e olio"]           = "₹220",
            ["falafel burger"]         = "₹220",
            ["chia seed pudding"]      = "₹180",
            ["chia pudding"]           = "₹180",
            ["cubano"]                 = "₹150",
            ["affogato"]               = "₹180",
        };

        private static readonly Dictionary<string, string> ItemDescriptions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["mediterranean bowl"] = "Barley, pickled veggies, hummus and seasonal produce",
            ["granola bowl"]       = "In-house granola with nuts, oats, honey, greek yoghurt and seasonal fruits",
            ["penne arrabiata"]    = "Pasta served with home made tomato basil sauce",
            ["aglio e olio"]       = "Garlic, butter, chilli, parmesan — a smooth, spicy twist on the classic",
            ["falafel burger"]     = "Crispy falafel with bold mediterranean vibes, freshness and crunch",
            ["chia seed pudding"]  = "Healthy all-day treat with chia seeds soaked in coconut milk and fruits",
            ["cubano"]             = "Bold espresso with a Cuban-style twist",
            ["affogato"]           = "Espresso poured over ice cream — simple, indulgent",
        };

        // ── Intent keywords ──────────────────────────────────────────────────

        private static readonly string[] GreetWords   = { "hi", "hello", "hey", "hiya", "howdy", "namaste", "good morning", "good afternoon", "good evening", "sup", "whats up", "what's up" };
        private static readonly string[] ByeWords     = { "bye", "goodbye", "see you", "cya", "thanks bye", "thank you bye", "ok thanks", "okay thanks", "cheers", "take care" };
        private static readonly string[] ThankWords   = { "thank", "thanks", "thank you", "thankyou", "thx", "ty" };

        private static readonly string[] TimingWords  = { "timing", "timings", "time", "hours", "open", "close", "opening", "closing", "schedule", "when", "24/7", "24 7", "available", "always open", "what time" };
        private static readonly string[] LocationWords= { "where", "location", "address", "directions", "how to reach", "how to get", "place", "area", "belagavi", "valbhay", "bauxite", "map", "navigate" };
        private static readonly string[] ContactWords = { "contact", "phone", "call", "number", "reach", "mobile", "telephone", "email" };
        private static readonly string[] MenuWords    = { "menu", "food", "eat", "drink", "beverage", "serve", "available", "offer", "have", "get", "order", "items" };
        private static readonly string[] CoffeeWords  = { "coffee", "espresso", "latte", "cappuccino", "americano", "mocha", "flat white", "cold brew", "black coffee", "iced coffee", "machiatto", "macchiato" };
        private static readonly string[] TeaWords     = { "tea", "matcha", "hibiscus", "mint", "green tea" };
        private static readonly string[] FoodWords    = { "food", "eat", "sandwich", "toast", "soup", "fries", "nachos", "burger", "pasta", "bowl", "snack", "meal", "bun", "korean" };
        private static readonly string[] SpecialsWords= { "special", "specials", "chef", "recommend", "signature", "best", "popular", "must try", "favourite", "favorite", "affogato", "cubano", "granola", "mediterranean", "penne", "aglio", "falafel", "chia" };
        private static readonly string[] PriceWords   = { "price", "cost", "how much", "rate", "charge", "fee", "expensive", "cheap", "affordable", "budget" };
        private static readonly string[] CoworkWords  = { "cowork", "co-work", "co work", "desk", "office", "workspace", "work space", "membership", "plan", "hot desk", "private office", "working space", "coworking", "working" };
        private static readonly string[] WifiWords    = { "wifi", "wi-fi", "internet", "connection", "network", "broadband", "speed" };
        private static readonly string[] AmenityWords = { "amenity", "amenities", "facilities", "facility", "ac", "air condition", "print", "printer", "parking", "seat", "seating", "book", "library" };
        private static readonly string[] VegWords     = { "veg", "vegetarian", "non veg", "non-veg", "meat", "chicken", "egg", "vegan", "dairy", "pure veg" };

        // ── Main logic ───────────────────────────────────────────────────────

        public string GetReply(string userMessage, List<ChatTurn>? history = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return "Please type a question and I'll be happy to help! ☕";

            var msg = Normalize(userMessage);

            // 1. Greetings
            if (HasAny(msg, GreetWords) && msg.Length < 30)
                return GetGreeting();

            // 2. Farewells
            if (HasAny(msg, ByeWords))
                return "It was great chatting! Hope to see you at Brew & Work soon. ☕";

            // 3. Thanks-only message
            if (HasAny(msg, ThankWords) && msg.Length < 20)
                return "You're welcome! Feel free to ask if you have any other questions. 😊";

            // 4. Check for a specific menu item price query
            var itemMatch = FindItemInMessage(msg);
            if (itemMatch != null)
            {
                var price = MenuPrices[itemMatch];
                var desc  = ItemDescriptions.ContainsKey(itemMatch) ? $"\n_{ItemDescriptions[itemMatch]}_" : "";
                var hasPrice = HasAny(msg, PriceWords) || HasAny(msg, new[] { "how much", "cost", "rate" });
                var hasAvail = HasAny(msg, new[] { "do you have", "have", "available", "serve", "get", "is there" });
                if (hasPrice)
                    return $"**{Titleize(itemMatch)}** is priced at **{price}**.{desc}";
                if (hasAvail)
                    return $"Yes, we do have **{Titleize(itemMatch)}**! It's priced at **{price}**.{desc}\n\nAnything else you'd like to know?";
                return $"**{Titleize(itemMatch)}** — {price}.{desc}";
            }

            // 5. "Do you have black coffee?"
            if (ContainsAll(msg, new[] { "black", "coffee" }))
                return "Yes! The closest options to a plain black coffee are:\n• **Espresso** — ₹130 (purest black coffee)\n• **Americano** — ₹130 (hot) / ₹180 (iced)\n• **Cold Brew** — ₹180\n\nYou can also add an extra shot for ₹60.";

            // 6. Timings
            if (HasAny(msg, TimingWords))
                return GetTimings(msg);

            // 7. Location / address
            if (HasAny(msg, LocationWords) && !HasAny(msg, CoffeeWords))
                return GetLocation();

            // 8. Contact
            if (HasAny(msg, ContactWords) && !HasAny(msg, CoworkWords))
                return "You can reach us at:\n📞 **+91 99225 57733**\n\nOr fill in the contact form on our website and we'll get back to you within 24 hours!";

            // 9. Veg / dietary questions
            if (HasAny(msg, VegWords))
                return "✅ Our entire menu is **100% Pure Vegetarian**. Everything is handcrafted with love — from our coffee to our food. We do not serve any non-vegetarian items.";

            // 10. WiFi
            if (HasAny(msg, WifiWords))
                return "📶 Yes! We offer **free WiFi** in the cafe for all customers.\n\nThe co-working space has **high-speed fiber internet** included in all membership plans.";

            // 11. Co-working enquiries
            if (HasAny(msg, CoworkWords))
                return GetCoworkingInfo(msg);

            // 12. Pricing in general
            if (HasAny(msg, PriceWords) && !HasAny(msg, MenuWords) && !HasAny(msg, FoodWords) && !HasAny(msg, CoffeeWords))
                return GetPriceSummary(msg);

            // 13. Coffee menu
            if (HasAny(msg, CoffeeWords))
                return GetCoffeeMenu(msg);

            // 14. Tea / matcha
            if (HasAny(msg, TeaWords))
                return "🍵 Our tea options:\n• **Hibiscus Rose / Mint Tea** — ₹150\n• **Matcha Classic** — ₹180\n• **Matcha Strawberry** — ₹220\n\nAll beverages can have Oat or Almond Milk added for ₹60.";

            // 15. Chef's specials
            if (HasAny(msg, SpecialsWords))
                return GetSpecials();

            // 16. Food menu
            if (HasAny(msg, FoodWords))
                return GetFoodMenu();

            // 17. General menu query
            if (HasAny(msg, MenuWords))
                return GetFullMenuSummary();

            // 18. Amenities
            if (HasAny(msg, AmenityWords))
                return GetAmenities(msg);

            // 19. Add-ons
            if (HasAny(msg, new[] { "addon", "add on", "add-on", "extra", "syrup", "flavour", "flavor", "oat milk", "almond milk", "whipped cream", "vanilla", "caramel", "hazelnut" }))
                return "🧁 **Cafe Add-ons:**\n• Vanilla / Caramel / Hazelnut / Whipped Cream — **+₹50**\n• Oat Milk / Almond Milk — **+₹60**\n• Extra Espresso Shot — **+₹60**";

            // 20. Bookings / reservations
            if (HasAny(msg, new[] { "book", "reserve", "reservation", "appointment", "visit", "come", "drop in", "drop by", "enquire", "enquiry" }))
                return "To book a co-working desk or office, you can:\n📞 Call us: **+91 99225 57733**\n📬 Or fill in the contact form on our website.\n\nFor the cafe, no reservation is needed — just walk in during our hours (**8 AM – 10 PM**)!";

            // 21. About / general
            if (HasAny(msg, new[] { "about", "tell me", "what is", "who are", "describe", "overview" }))
                return "☕ **Brew & Work** is a specialty cafe and co-working space in **Belagavi, Karnataka**.\n\nWe're a place where great coffee meets a productive work environment. The cafe serves handcrafted beverages and 100% vegetarian food, while the co-working space offers flexible desks and private offices — open **24/7**.";

            // 22. Catch-all
            return GetFallback();
        }

        // ── Response builders ────────────────────────────────────────────────

        private static string GetGreeting()
        {
            var greets = new[]
            {
                "Hi there! Welcome to Brew & Work ☕ How can I help you today? You can ask me about our menu, timings, co-working plans, or anything else!",
                "Hello! 👋 I'm the Brew & Work assistant. Feel free to ask about our menu, prices, location, or co-working space!",
                "Hey! Great to have you here. What can I help you with — cafe menu, co-working plans, timings, or something else? ☕",
            };
            return greets[DateTime.Now.Second % greets.Length];
        }

        private static string GetTimings(string msg)
        {
            if (HasAny(msg, CoworkWords))
                return "🕐 The **Co-Working space** is open **24/7** — yes, round the clock, every day!";
            if (HasAny(msg, new[] { "cafe", "coffee", "drink", "eat", "food" }))
                return "🕗 The **Cafe** is open **8 AM – 10 PM** daily.";
            return "🕗 **Cafe:** 8 AM – 10 PM (daily)\n🕐 **Co-Working Space:** 24/7 (always open!)\n\nStep in anytime — we'd love to have you!";
        }

        private static string GetLocation()
            => "📍 **Brew & Work**\nBauxite Road, Valbhay Nagar, Belagavi, Karnataka.\n\n📞 **+91 99225 57733**\n\nLook us up on Google Maps or call us for directions!";

        private static string GetCoffeeMenu(string msg)
        {
            var iced = HasAny(msg, new[] { "iced", "cold", "ice" });
            var hot  = HasAny(msg, new[] { "hot", "warm" });

            if (iced && !hot)
                return "🧊 **Cold Coffee Options:**\n• Iced Americano — ₹180\n• Iced Cappuccino — ₹180\n• Iced Latte — ₹180\n• Iced Flat White — ₹220\n• Iced Mocha — ₹220\n• Iced Machiatto — ₹220\n• Iced Frappuccino — ₹220\n• Cold Brew — ₹180\n\n_Add Oat/Almond Milk +₹60 | Extra Shot +₹60_";

            if (hot && !iced)
                return "☕ **Hot Coffee Options:**\n• Espresso — ₹130\n• Americano — ₹130\n• Cappuccino — ₹180 / ₹220\n• Machiatto — ₹180\n• Latte — ₹220\n• Flat White — ₹220\n• Mocha — ₹220\n\n_Add Vanilla/Caramel/Hazelnut/Whipped Cream +₹50_";

            return "☕ **Hot Coffee:**\n• Espresso — ₹130\n• Americano — ₹130\n• Cappuccino — ₹180/₹220\n• Machiatto — ₹180\n• Latte — ₹220\n• Flat White — ₹220\n• Mocha — ₹220\n\n🧊 **Cold Coffee:**\n• Iced Americano — ₹180\n• Iced Latte — ₹180\n• Cold Brew — ₹180\n• Iced Frappuccino — ₹220\n• Iced Mocha — ₹220\n\n_Add-ons available — syrups, oat/almond milk, extra shot_";
        }

        private static string GetFoodMenu()
            => "🍽️ **Food Menu** (100% Vegetarian):\n• Chilli Cheese Toast — ₹170\n• Marinara — ₹140\n• Corn Sandwich — ₹150\n• Chutney Sandwich — ₹150\n• Korean Bun — ₹150\n• Fresh Tomato Basil Soup — ₹150\n• French Fries — ₹150\n• Nachos & Salsa — ₹120\n\n🌟 **Chef's Specials** also available — ask me about those!";

        private static string GetSpecials()
            => "🌟 **Chef's Specials:**\n• Mediterranean Bowl — ₹220\n  _Barley, pickled veggies, hummus, seasonal produce_\n• Granola Bowl — ₹280\n  _In-house granola, nuts, oats, honey, greek yoghurt, fruits_\n• Penne Arrabiata — ₹220\n  _Pasta with home made tomato basil sauce_\n• Aglio e Olio — ₹220\n  _Garlic, butter, chilli, parmesan_\n• Falafel Burger — ₹220\n  _Crispy falafel with mediterranean flavours_\n• Chia Seed Pudding — ₹180\n  _Coconut milk and fruits — healthy & delicious_\n• Cubano — ₹150\n  _Bold espresso with a Cuban twist_\n• Affogato — ₹180\n  _Espresso poured over ice cream — simple, indulgent_";

        private static string GetFullMenuSummary()
            => "☕ **Our Menu at a glance:**\n\n**Hot Beverages** — ₹130 to ₹220\n**Cold Beverages** — ₹180 to ₹220\n**Food** — ₹120 to ₹170\n**Chef's Specials** — ₹150 to ₹280\n\n100% Pure Vegetarian. Ask me about a specific item for the exact price!";

        private static string GetCoworkingInfo(string msg)
        {
            if (HasAny(msg, new[] { "hot desk", "hot-desk", "hotdesk", "day pass", "daily", "flexible" }))
                return "💺 **Hot Desk Plans** (Flexible, no long-term commitment):\n• Day Pass — **₹750**\n• Week Pass — **₹2,500**\n• Month Pass — **₹7,500**\n\nJust show up and get to work! Call **+91 99225 57733** to confirm availability.";

            if (HasAny(msg, new[] { "private office", "private-office", "team", "office", "dedicated" }))
                return "🏢 **Private Office Plans** (Lockable, exclusively yours):\n• 4 Members — **₹20,000/month**\n• 6 Members — **₹25,000/month**\n• 8 Members — **₹30,000/month**\n\nPlug-and-play, ready from day one. Call **+91 99225 57733** to enquire.";

            if (HasAny(msg, PriceWords))
                return GetPriceSummary(msg);

            return "🏢 **Co-Working Space at Brew & Work:**\n\n**Hot Desks** (flexible):\n• Day Pass — ₹750\n• Week Pass — ₹2,500\n• Month Pass — ₹7,500\n\n**Private Offices** (for teams):\n• 4 Members — ₹20,000/mo\n• 6 Members — ₹25,000/mo\n• 8 Members — ₹30,000/mo\n\n⏰ Open **24/7** | 📶 High-speed WiFi | ❄️ AC | ☕ Coffee & Tea included\n\nCall **+91 99225 57733** to book or enquire!";
        }

        private static string GetPriceSummary(string msg)
        {
            if (HasAny(msg, CoworkWords) || HasAny(msg, new[] { "desk", "office", "membership" }))
                return "💰 **Co-Working Pricing:**\n\n**Hot Desks:**\n• Day — ₹750\n• Week — ₹2,500\n• Month — ₹7,500\n\n**Private Offices:**\n• 4 Members — ₹20,000/mo\n• 6 Members — ₹25,000/mo\n• 8 Members — ₹30,000/mo";

            return "💰 **Quick Price Guide:**\n\n• Coffee (hot) — ₹130–₹220\n• Coffee (iced) — ₹180–₹220\n• Food — ₹120–₹170\n• Chef's Specials — ₹150–₹280\n• Hot Desk (daily) — ₹750\n• Private Office — from ₹20,000/mo\n\nAsk about any specific item and I'll give you the exact price!";
        }

        private static string GetAmenities(string msg)
        {
            if (HasAny(msg, CoworkWords))
                return "✨ **Co-Working Amenities:**\n• ❄️ Air Conditioning\n• 📶 High-Speed Fiber WiFi\n• 🧹 Daily Housekeeping\n• ☕ Coffee & Tea included\n• 🖨️ Printing Service (fee applicable)";

            return "✨ **Cafe Amenities:**\n• 📶 Free WiFi\n• 🛋️ Cozy Seating (sofas, armchairs, window seats)\n• ☀️ Natural Light & large windows\n• 📸 Photo-friendly aesthetic space\n• 📚 Book Corner\n\n**Co-Working also offers:** AC, fiber internet, daily housekeeping, coffee/tea, and printing services.";
        }

        private static string GetFallback()
        {
            var options = new[]
            {
                "I'm here to help with everything about Brew & Work! You can ask me about:\n• ☕ Menu & prices\n• 🏢 Co-working plans\n• ⏰ Timings\n• 📍 Location\n• 🌟 Chef's specials\n\nWhat would you like to know?",
                "Not sure I caught that! I can tell you about our **menu**, **co-working plans**, **timings**, or **location**. What are you looking for?",
                "I specialise in all things Brew & Work! Try asking about a specific menu item, our co-working prices, opening hours, or where to find us. 😊",
            };
            return options[DateTime.Now.Millisecond % options.Length];
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string Normalize(string s)
            => Regex.Replace(s.ToLowerInvariant().Trim(), @"[^\w\s\-\/]", " ").Trim();

        private static bool HasAny(string msg, IEnumerable<string> words)
            => words.Any(w => msg.Contains(w, StringComparison.OrdinalIgnoreCase));

        private static bool ContainsAll(string msg, IEnumerable<string> words)
            => words.All(w => msg.Contains(w, StringComparison.OrdinalIgnoreCase));

        private static string? FindItemInMessage(string msg)
        {
            // Check longest matches first to avoid partial hits
            return MenuPrices.Keys
                .OrderByDescending(k => k.Length)
                .FirstOrDefault(k => msg.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private static string Titleize(string s)
            => string.Join(" ", s.Split(' ').Select(w => char.ToUpper(w[0]) + w[1..]));
    }
}
