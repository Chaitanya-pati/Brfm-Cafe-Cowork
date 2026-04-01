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

    // ──────────────────────────────────────────────────────────────────────────
    // Knowledge entry — the building block of the knowledge base
    // ──────────────────────────────────────────────────────────────────────────
    public class KnowledgeEntry
    {
        /// <summary>Words/phrases that boost this entry's score (each match = +1).</summary>
        public string[] Keywords    { get; init; } = [];
        /// <summary>High-value phrases that strongly indicate this intent (+3 each).</summary>
        public string[] StrongPhrases { get; init; } = [];
        /// <summary>If ANY of these appear the entry is disqualified (score = 0).</summary>
        public string[] Blockers    { get; init; } = [];
        /// <summary>Minimum score required to consider this entry a match.</summary>
        public int      MinScore    { get; init; } = 1;
        /// <summary>One or more response variants (chosen randomly for variety).</summary>
        public string[] Responses   { get; init; } = [];
        /// <summary>Optional dynamic response that overrides Responses when set.</summary>
        public Func<string, string>? DynamicResponse { get; init; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // The custom AI service — fully self-contained, no external APIs
    // ──────────────────────────────────────────────────────────────────────────
    public class LocalChatService : IChatService
    {
        // ── Menu item lookup (item name → price) ────────────────────────────
        private static readonly Dictionary<string, string> MenuItems = new(StringComparer.OrdinalIgnoreCase)
        {
            // Hot Beverages
            ["espresso"]                = "₹130",
            ["americano"]               = "₹130 (hot) / ₹180 (iced)",
            ["cappuccino"]              = "₹180 (regular) / ₹220 (large)",
            ["machiatto"]               = "₹180",
            ["macchiato"]               = "₹180",
            ["latte"]                   = "₹220",
            ["flat white"]              = "₹220",
            ["mocha"]                   = "₹220 (hot or iced)",
            ["hot chocolate"]           = "₹180",
            ["hibiscus rose tea"]       = "₹150",
            ["mint tea"]                = "₹150",
            ["matcha classic"]          = "₹180",
            ["matcha strawberry"]       = "₹220",
            ["matcha"]                  = "₹180 (classic) / ₹220 (strawberry)",
            // Cold Beverages
            ["iced americano"]          = "₹180",
            ["iced cappuccino"]         = "₹180",
            ["iced latte"]              = "₹180",
            ["iced flat white"]         = "₹220",
            ["iced mocha"]              = "₹220",
            ["iced machiatto"]          = "₹220",
            ["iced macchiato"]          = "₹220",
            ["iced frappuccino"]        = "₹220",
            ["frappuccino"]             = "₹220",
            ["cold brew"]               = "₹180",
            ["lemonade"]                = "₹180",
            ["peach coconut refresher"] = "₹180",
            ["orange juice"]            = "₹220",
            ["valencia orange juice"]   = "₹220",
            // Food
            ["chilli cheese toast"]     = "₹170",
            ["chili cheese toast"]      = "₹170",
            ["marinara"]                = "₹140",
            ["corn sandwich"]           = "₹150",
            ["chutney sandwich"]        = "₹150",
            ["korean bun"]              = "₹150",
            ["tomato basil soup"]       = "₹150",
            ["french fries"]            = "₹150",
            ["fries"]                   = "₹150",
            ["nachos"]                  = "₹120",
            // Specials
            ["mediterranean bowl"]      = "₹220",
            ["granola bowl"]            = "₹280",
            ["penne arrabiata"]         = "₹220",
            ["penne arrabbiata"]        = "₹220",
            ["aglio e olio"]            = "₹220",
            ["falafel burger"]          = "₹220",
            ["chia seed pudding"]       = "₹180",
            ["chia pudding"]            = "₹180",
            ["cubano"]                  = "₹150",
            ["affogato"]                = "₹180",
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

        // ── Knowledge base ───────────────────────────────────────────────────
        private static readonly List<KnowledgeEntry> KB = new()
        {
            // ── Greetings ────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["hello", "hi", "hey", "namaste", "good morning", "good afternoon",
                                 "good evening", "howdy", "hiya", "whats up", "what's up"],
                Blockers = ["price", "menu", "coffee", "food", "desk", "room", "wifi", "location", "timing"],
                MinScore = 3,
                Responses =
                [
                    "Hi there! Welcome to Brew & Work ☕ Ask me anything — menu, timings, co-working plans, meeting rooms, or our location!",
                    "Hello! 👋 I'm the Brew & Work assistant. What can I help you with today?",
                    "Hey! Great to have you here. Ask me about our menu, workspace plans, meeting rooms, or anything else. ☕",
                ]
            },

            // ── Farewells ────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["bye", "goodbye", "see you", "see ya", "cya", "take care", "good night"],
                Keywords      = ["thanks bye", "thank you bye", "ok thanks", "okay thanks", "cheers"],
                MinScore      = 3,
                Responses     = ["It was great chatting! Hope to see you at Brew & Work soon ☕", "Take care! See you soon 👋"]
            },

            // ── Thank you ────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["thank you", "thanks", "thankyou", "thx", "ty"],
                Blockers      = ["bye", "goodbye"],
                MinScore      = 3,
                Responses     =
                [
                    "You're welcome! Feel free to ask anything else 😊",
                    "Happy to help! Is there anything else you'd like to know?",
                ]
            },

            // ── Black coffee (special case — very common question) ────────────
            new()
            {
                StrongPhrases = ["black coffee"],
                Keywords      = ["black", "plain coffee", "simple coffee", "filter coffee", "no milk"],
                MinScore      = 3,
                Responses     =
                [
                    "Yes! The closest to a plain black coffee are:\n• **Espresso** — ₹130 (purest shot)\n• **Americano** — ₹130 (hot) / ₹180 (iced)\n• **Cold Brew** — ₹180\n\nYou can also add an extra shot for ₹60. ☕",
                ]
            },

            // ── Timings — co-working specific ───────────────────────────────
            new()
            {
                StrongPhrases = ["coworking timing", "cowork timing", "office timing", "workspace timing",
                                 "co-working time", "co working time", "open at night", "open 24 hours",
                                 "open 24/7", "always open", "24 hours", "overnight", "late night"],
                Keywords      = ["cowork", "co-work", "desk", "workspace", "open 24", "night", "overnight"],
                MinScore      = 2,
                Responses     =
                [
                    "🕐 Our **Co-Working space** is open **24/7** — round the clock, every single day. Come and work whenever suits you!",
                ]
            },

            // ── Timings — cafe specific ──────────────────────────────────────
            new()
            {
                StrongPhrases = ["cafe timing", "cafe hours", "cafe open", "when does the cafe"],
                Keywords      = ["cafe", "coffee shop", "drink", "eat", "food", "timing", "hours", "open", "close"],
                MinScore      = 2,
                Responses     =
                [
                    "🕗 The **Cafe** is open **8 AM – 10 PM** daily. Come in for a great coffee anytime within those hours!",
                ]
            },

            // ── Timings — general ────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["timing", "timings", "opening hours", "opening time", "closing time",
                                 "what time", "when are you open", "when do you open", "when do you close"],
                Keywords      = ["open", "close", "hours", "schedule", "time", "available"],
                MinScore      = 3,
                Responses     =
                [
                    "🕗 **Cafe:** 8 AM – 10 PM (daily)\n🕐 **Co-Working Space:** 24/7 (always open!)\n📅 **Meeting / Conference Room:** Available 8 AM – 10 PM (advance booking required)\n\nStep in anytime — we'd love to have you!",
                ]
            },

            // ── Location / address ────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["where are you", "where is it", "your address", "your location",
                                 "how to reach", "how to find", "how do i get", "directions",
                                 "where is brew", "where is the cafe", "where exactly",
                                 "located", "location"],
                Keywords      = ["address", "area", "belagavi", "valbhay", "bauxite",
                                 "map", "navigate", "nearby", "place", "where"],
                MinScore      = 3,
                Responses     =
                [
                    "📍 **Brew & Work**\nBauxite Road, Valbhay Nagar, Belagavi, Karnataka.\n\n📞 **+91 99225 57733**\n\nSearch us on Google Maps or give us a call for directions!",
                ]
            },

            // ── Contact ───────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["contact number", "phone number", "call you", "your number",
                                 "how to contact", "get in touch", "contact you", "contact us",
                                 "contact", "reach you"],
                Keywords      = ["phone", "call", "reach", "mobile", "number", "telephone"],
                Blockers      = ["meeting", "book", "reserve", "cowork", "desk"],
                MinScore      = 3,
                Responses     =
                [
                    "📞 Reach us at **+91 99225 57733**\n\nOr fill in the contact form on our website — we reply within 24 hours!",
                ]
            },

            // ── Meeting / Conference Room ─────────────────────────────────────
            new()
            {
                StrongPhrases = ["meeting room", "conference room", "boardroom", "board room",
                                 "meeting hall", "conference hall", "seminar room"],
                Keywords      = ["meeting", "conference", "board", "presentation", "seminar",
                                 "event", "client meeting", "team meeting", "workshop", "training"],
                MinScore      = 3,
                Responses     =
                [
                    "📋 Yes! We have a **Meeting / Conference Room** available for bookings.\n\n✅ Ideal for client meetings, team discussions, presentations, and workshops\n📶 High-speed WiFi included\n❄️ Fully air-conditioned\n☕ Coffee & tea can be arranged\n🖨️ Printing & projector support available\n\n📅 **Available:** 8 AM – 10 PM (advance booking required)\n📞 Call us at **+91 99225 57733** or fill the contact form to check availability and pricing!",
                    "🏛️ Absolutely! Our **Conference / Meeting Room** is bookable for:\n• Client presentations\n• Team meetings\n• Workshops & training sessions\n• Seminars\n\nEquipped with WiFi, AC, and projector support. Coffee/tea can be arranged.\n\nCall **+91 99225 57733** to check availability and book!",
                ]
            },

            // ── WiFi ─────────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["wifi", "wi-fi", "internet", "internet speed", "network connection"],
                Keywords      = ["broadband", "fiber", "connection", "speed", "online"],
                MinScore      = 3,
                Responses     =
                [
                    "📶 Yes! We have **free WiFi** in the cafe for all customers.\n\nThe co-working space has **high-speed fiber internet** included in all membership plans. The meeting room also has dedicated WiFi.",
                    "📶 **WiFi at Brew & Work:**\n• Cafe — Free WiFi for all customers\n• Co-Working — High-speed fiber included in all plans\n• Meeting Room — Dedicated WiFi connection",
                ]
            },

            // ── Vegetarian / dietary ─────────────────────────────────────────
            new()
            {
                StrongPhrases = ["vegetarian", "non veg", "non-veg", "veg menu", "pure veg",
                                 "do you serve non", "any meat", "vegan"],
                Keywords      = ["veg", "meat", "chicken", "egg", "dairy", "plant", "diet"],
                MinScore      = 3,
                Responses     =
                [
                    "✅ Our entire menu is **100% Pure Vegetarian** — handcrafted with love from coffee to food. We do not serve any non-vegetarian items.",
                ]
            },

            // ── Parking ───────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["parking", "car park", "bike parking", "vehicle", "park my car", "where to park"],
                Keywords      = ["park", "parking"],
                MinScore      = 3,
                Responses     =
                [
                    "🚗 For parking details, please give us a call at **+91 99225 57733** — our team will guide you to the nearest available options near Bauxite Road, Valbhay Nagar.",
                ]
            },

            // ── Add-ons ───────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["add on", "addon", "add-on", "extra syrup", "oat milk", "almond milk",
                                 "whipped cream", "extra shot", "flavour", "flavor", "vanilla", "caramel", "hazelnut"],
                Keywords      = ["extra", "add", "syrup", "milk", "cream", "shot"],
                MinScore      = 3,
                Responses     =
                [
                    "🧁 **Cafe Add-ons:**\n• Vanilla / Caramel / Hazelnut / Whipped Cream — **+₹50**\n• Oat Milk / Almond Milk — **+₹60**\n• Extra Espresso Shot — **+₹60**",
                ]
            },

            // ── Hot beverages ─────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["hot coffee", "hot beverage", "hot drink", "hot drinks menu"],
                Keywords      = ["hot", "warm", "espresso", "cappuccino", "latte", "americano",
                                 "machiatto", "flat white", "mocha"],
                Blockers      = ["iced", "cold", "cold brew"],
                MinScore      = 2,
                Responses     =
                [
                    "☕ **Hot Beverages:**\n• Espresso — ₹130\n• Americano — ₹130\n• Cappuccino — ₹180 / ₹220\n• Machiatto — ₹180\n• Latte — ₹220\n• Flat White — ₹220\n• Mocha — ₹220\n• Hot Chocolate — ₹180\n• Hibiscus Rose / Mint Tea — ₹150\n• Matcha Classic — ₹180\n• Matcha Strawberry — ₹220\n\n_Syrups & Whipped Cream +₹50 | Add flavour shots any time_",
                ]
            },

            // ── Cold beverages ────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["cold coffee", "iced coffee", "cold drink", "cold beverage",
                                 "cold drinks menu", "iced drinks"],
                Keywords      = ["iced", "cold", "cold brew", "frappuccino", "refresher"],
                Blockers      = ["hot", "warm"],
                MinScore      = 2,
                Responses     =
                [
                    "🧊 **Cold Beverages:**\n• Iced Americano — ₹180\n• Iced Cappuccino — ₹180\n• Iced Latte — ₹180\n• Iced Flat White — ₹220\n• Iced Mocha — ₹220\n• Iced Machiatto — ₹220\n• Iced Frappuccino — ₹220\n• Cold Brew — ₹180\n• Lemonade — ₹180\n• Peach Coconut Refresher — ₹180\n• Valencia Orange Juice — ₹220\n\n_Oat / Almond Milk +₹60 | Extra Shot +₹60_",
                ]
            },

            // ── Tea / matcha ──────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["tea", "matcha", "hibiscus", "mint tea", "green tea"],
                Keywords      = ["tea", "matcha", "herbal", "hibiscus", "mint"],
                Blockers      = ["coffee", "latte", "cappuccino"],
                MinScore      = 3,
                Responses     =
                [
                    "🍵 **Our Tea & Matcha:**\n• Hibiscus Rose / Mint Tea — ₹150\n• Matcha Classic — ₹180\n• Matcha Strawberry — ₹220\n\n_Oat or Almond Milk can be added for +₹60_",
                ]
            },

            // ── Food menu ─────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["food menu", "what food", "something to eat", "any snacks",
                                 "what do you serve to eat", "food options"],
                Keywords      = ["food", "eat", "snack", "sandwich", "toast", "soup", "fries",
                                 "nachos", "bun", "meal"],
                Blockers      = ["special", "specials", "chef"],
                MinScore      = 2,
                Responses     =
                [
                    "🍽️ **Food Menu** (100% Vegetarian):\n• Chilli Cheese Toast — ₹170\n• Marinara — ₹140\n• Corn Sandwich — ₹150\n• Chutney Sandwich — ₹150\n• Korean Bun — ₹150\n• Fresh Tomato Basil Soup — ₹150\n• French Fries — ₹150\n• Nachos & Salsa — ₹120\n\nWe also have 🌟 **Chef's Specials** — want to see those?",
                ]
            },

            // ── Chef's specials ───────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["chef special", "chef's special", "specials", "signature dish",
                                 "must try", "best dish", "recommend", "popular dish",
                                 "affogato", "cubano", "granola bowl", "falafel", "penne", "aglio"],
                Keywords      = ["special", "signature", "recommend", "favourite", "popular",
                                 "best", "mediterranean", "granola", "penne", "aglio", "falafel",
                                 "chia", "cubano", "affogato"],
                MinScore      = 2,
                Responses     =
                [
                    "🌟 **Chef's Specials:**\n• Mediterranean Bowl — ₹220\n  _Barley, pickled veggies, hummus, seasonal produce_\n• Granola Bowl — ₹280\n  _In-house granola, nuts, oats, honey, greek yoghurt, fruits_\n• Penne Arrabiata — ₹220\n  _Pasta with home made tomato basil sauce_\n• Aglio e Olio — ₹220\n  _Garlic, butter, chilli, parmesan_\n• Falafel Burger — ₹220\n  _Crispy falafel with mediterranean vibes_\n• Chia Seed Pudding — ₹180\n  _Coconut milk and fruits — healthy & indulgent_\n• Cubano — ₹150\n  _Bold espresso with a Cuban twist_\n• Affogato — ₹180\n  _Espresso poured over ice cream_",
                ]
            },

            // ── Full menu ─────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["full menu", "complete menu", "entire menu", "whole menu",
                                 "all menu", "what do you have", "what do you serve",
                                 "what's on the menu", "whats on the menu", "show me the menu"],
                Keywords      = ["menu", "offer", "available", "serve", "have"],
                MinScore      = 4,
                Responses     =
                [
                    "☕ **Brew & Work Menu — Quick Overview:**\n\n**Hot Beverages** — ₹130 to ₹220\n(Espresso, Americano, Cappuccino, Latte, Flat White, Mocha, Hot Chocolate, Tea, Matcha)\n\n**Cold Beverages** — ₹180 to ₹220\n(Iced coffees, Cold Brew, Frappuccino, Lemonade, Refreshers, OJ)\n\n**Food** — ₹120 to ₹170\n(Sandwiches, Toast, Soup, Fries, Nachos, Korean Bun, Marinara)\n\n**Chef's Specials** — ₹150 to ₹280\n(Bowls, Pasta, Falafel Burger, Affogato, Cubano, Chia Pudding)\n\n_100% Vegetarian. Ask me about any specific item for the exact price!_",
                ]
            },

            // ── Hot desk plans ────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["hot desk", "hot-desk", "hotdesk", "day pass", "daily pass",
                                 "weekly pass", "monthly pass", "flexible desk", "drop in"],
                Keywords      = ["hot desk", "flexible", "day", "daily", "week", "monthly", "drop"],
                MinScore      = 3,
                Responses     =
                [
                    "💺 **Hot Desk Plans** (Flexible, no long-term commitment):\n• **Day Pass** — ₹750\n• **Week Pass** — ₹2,500\n• **Month Pass** — ₹7,500\n\nJust show up and get to work! Includes high-speed WiFi, AC & coffee/tea.\n📞 Call **+91 99225 57733** to confirm availability.",
                ]
            },

            // ── Private office plans ──────────────────────────────────────────
            new()
            {
                StrongPhrases = ["private office", "dedicated office", "team office", "office for team",
                                 "lockable office", "exclusive office"],
                Keywords      = ["private", "office", "team", "dedicated", "lockable", "exclusive"],
                Blockers      = ["meeting room", "conference"],
                MinScore      = 3,
                Responses     =
                [
                    "🏢 **Private Office Plans** (Lockable & exclusively yours):\n• **4 Members** — ₹20,000/month\n• **6 Members** — ₹25,000/month\n• **8 Members** — ₹30,000/month\n\nPlug-and-play, ready from day one. Includes WiFi, AC, housekeeping & coffee/tea.\n📞 Enquire: **+91 99225 57733**",
                ]
            },

            // ── All co-working plans (general) ────────────────────────────────
            new()
            {
                StrongPhrases = ["coworking plans", "co-working plans", "workspace plans",
                                 "membership plans", "coworking prices", "workspace pricing",
                                 "tell me about coworking", "coworking options"],
                Keywords      = ["cowork", "co-work", "workspace", "membership", "plan", "pricing",
                                 "working space"],
                Blockers      = ["meeting room", "conference", "cafe", "coffee", "food"],
                MinScore      = 3,
                Responses     =
                [
                    "🏢 **Co-Working at Brew & Work:**\n\n**💺 Hot Desks** (Flexible):\n• Day Pass — ₹750\n• Week Pass — ₹2,500\n• Month Pass — ₹7,500\n\n**🏢 Private Offices** (For teams):\n• 4 Members — ₹20,000/mo\n• 6 Members — ₹25,000/mo\n• 8 Members — ₹30,000/mo\n\n**📋 Meeting / Conference Room** — Available on booking\n\n⏰ Open **24/7** | 📶 Fiber WiFi | ❄️ AC | ☕ Coffee & Tea | 🖨️ Printing\n\n📞 Call **+91 99225 57733** to book or enquire!",
                ]
            },

            // ── Co-working amenities ──────────────────────────────────────────
            new()
            {
                StrongPhrases = ["coworking amenities", "workspace amenities", "coworking facilities",
                                 "what facilities", "what amenities", "included in plan", "what's included"],
                Keywords      = ["amenity", "amenities", "facility", "facilities", "include",
                                 "ac", "air condition", "print", "printer", "housekeep"],
                MinScore      = 3,
                Responses     =
                [
                    "✨ **Co-Working Amenities:**\n• ❄️ Air Conditioning\n• 📶 High-Speed Fiber WiFi\n• 🧹 Daily Housekeeping\n• ☕ Coffee & Tea included\n• 🖨️ Printing & Scanning (fee applicable)\n• 📋 Meeting / Conference Room (bookable)\n\n**Cafe Extras:** Free WiFi, cozy seating, natural light, book corner & photo-friendly space.",
                ]
            },

            // ── Booking ───────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["how to book", "how do i book", "how to reserve", "make a booking",
                                 "book a desk", "book a room", "book a meeting", "book an office",
                                 "reserve a desk", "want to book"],
                Keywords      = ["book", "reserve", "booking", "reservation", "appointment", "enquire", "enquiry"],
                MinScore      = 3,
                Responses     =
                [
                    "📅 **To make a booking:**\n📞 Call us: **+91 99225 57733**\n📬 Or fill the contact form on our website — we'll get back within 24 hours.\n\nFor the **cafe**, no reservation is needed — just walk in between 8 AM and 10 PM!\nFor **co-working desks**, **private offices**, and the **meeting room**, advance booking is recommended.",
                ]
            },

            // ── About ─────────────────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["about brew", "what is brew and work", "tell me about you",
                                 "what is this place", "who are you", "describe yourself",
                                 "what do you do"],
                Keywords      = ["about", "overview", "describe", "who", "what is"],
                MinScore      = 4,
                Responses     =
                [
                    "☕ **Brew & Work** is a specialty cafe and co-working space in **Belagavi, Karnataka**.\n\nWe're a place where great coffee meets a productive work environment:\n• 🍽️ 100% vegetarian cafe with handcrafted beverages & food\n• 💺 Flexible hot desks & private offices (24/7)\n• 📋 Meeting / conference room available for bookings\n• 📶 High-speed WiFi, AC, daily housekeeping\n• ☕ Cafe open 8 AM – 10 PM\n\n📍 Bauxite Road, Valbhay Nagar, Belagavi | 📞 +91 99225 57733",
                ]
            },

            // ── Pricing overview ─────────────────────────────────────────────
            new()
            {
                StrongPhrases = ["how much", "price list", "pricing", "how expensive", "what does it cost",
                                 "rates", "price range", "cost of"],
                Keywords      = ["price", "cost", "rate", "charge", "fee", "expensive", "cheap", "affordable"],
                MinScore      = 3,
                Responses     =
                [
                    "💰 **Quick Price Guide:**\n\n**Cafe:**\n• Coffee (hot) — ₹130–₹220\n• Coffee (iced) — ₹180–₹220\n• Food — ₹120–₹170\n• Chef's Specials — ₹150–₹280\n\n**Co-Working:**\n• Hot Desk Day — ₹750\n• Hot Desk Month — ₹7,500\n• Private Office — from ₹20,000/mo\n\n**Meeting Room:** Call for rates — **+91 99225 57733**\n\nAsk about any specific item for the exact price!",
                ]
            },
        };

        // ── Scoring engine ───────────────────────────────────────────────────

        public string GetReply(string userMessage, List<ChatTurn>? history = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return "Please type your question and I'll be happy to help! ☕";

            var msg = Normalize(userMessage);

            // 1. Check for a specific named menu item first (highest priority)
            var itemReply = TryMenuItemLookup(msg, userMessage);
            if (itemReply != null) return itemReply;

            // 2. Score every knowledge entry
            var best = KB
                .Select(e => (entry: e, score: Score(msg, e)))
                .Where(x => x.score >= x.entry.MinScore)
                .OrderByDescending(x => x.score)
                .FirstOrDefault();

            if (best.entry != null)
            {
                var responses = best.entry.Responses;
                return responses[Math.Abs(DateTime.Now.Millisecond) % responses.Length];
            }

            // 3. Fallback
            return PickFallback();
        }

        private static int Score(string msg, KnowledgeEntry entry)
        {
            // Any blocker present → disqualify
            if (entry.Blockers.Any(b => msg.Contains(b, StringComparison.OrdinalIgnoreCase)))
                return 0;

            var score = 0;

            // Strong phrases are worth 3 points each
            foreach (var phrase in entry.StrongPhrases)
                if (msg.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                    score += 3;

            // Keywords are worth 1 point each
            foreach (var kw in entry.Keywords)
                if (msg.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    score += 1;

            return score;
        }

        // ── Menu item lookup ─────────────────────────────────────────────────

        private static string? TryMenuItemLookup(string msg, string original)
        {
            // Find the longest matching item name in the message
            var match = MenuItems.Keys
                .OrderByDescending(k => k.Length)
                .FirstOrDefault(k => msg.Contains(k, StringComparison.OrdinalIgnoreCase));

            if (match == null) return null;

            var price = MenuItems[match];
            var desc  = ItemDescriptions.TryGetValue(match, out var d) ? $"\n_\"{d}\"_" : "";
            var name  = Titleize(match);

            // Detect intent nuance
            var isPriceQ = ContainsAny(msg, ["price", "cost", "how much", "rate", "charge"]);
            var isAvailQ = ContainsAny(msg, ["do you have", "have", "available", "serve", "get", "is there", "any"]);

            if (isPriceQ)
                return $"**{name}** is priced at **{price}**.{desc}";

            if (isAvailQ)
                return $"Yes, we have **{name}** — priced at **{price}**.{desc}\n\nAnything else you'd like to know?";

            return $"**{name}** — {price}.{desc}";
        }

        // ── Fallback ──────────────────────────────────────────────────────────

        private static string PickFallback()
        {
            var options = new[]
            {
                "I'm here to help with everything about Brew & Work! Ask me about:\n• ☕ Menu & prices\n• 💺 Co-working plans\n• 📋 Meeting / conference room\n• ⏰ Timings\n• 📍 Location\n\nWhat would you like to know?",
                "Hmm, I didn't quite catch that! I can help with our **menu**, **co-working & meeting room bookings**, **timings**, or **location**. What are you looking for?",
                "Not sure I understood that — try asking about our menu, coffee, food, workspace plans, meeting rooms, or where to find us! 😊",
            };
            return options[Math.Abs(DateTime.Now.Millisecond) % options.Length];
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        private static string Normalize(string s)
            => Regex.Replace(s.ToLowerInvariant().Trim(), @"[^\w\s\-\/]", " ").Trim();

        private static bool ContainsAny(string msg, IEnumerable<string> words)
            => words.Any(w => msg.Contains(w, StringComparison.OrdinalIgnoreCase));

        private static string Titleize(string s)
            => string.Join(" ", s.Split(' ').Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w[1..] : w));
    }
}
