using System.Diagnostics;
using System.Text.Json;
using co_working.Models;
using Microsoft.AspNetCore.Mvc;

namespace co_working.Controllers
{
    public class PagesController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PagesController(ILogger<HomeController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Cafe()
        {
            var path = Path.Combine(_env.ContentRootPath, "Data", "cafe-menu.json");
            var json = System.IO.File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<CafeData>(json, _jsonOptions) ?? new CafeData();
            return View(data);
        }

        public IActionResult Coworking()
        {
            var path = Path.Combine(_env.ContentRootPath, "Data", "coworking-plans.json");
            var json = System.IO.File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<CoworkingData>(json, _jsonOptions) ?? new CoworkingData();
            return View(data);
        }

        public IActionResult PrivacyPolicy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
