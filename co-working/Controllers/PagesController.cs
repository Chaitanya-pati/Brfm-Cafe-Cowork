using System.Diagnostics;
using co_working.Models;
using Microsoft.AspNetCore.Mvc;

namespace co_working.Controllers
{
    public class PagesController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public PagesController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Cafe()
        {
            return View();
        }
        public IActionResult Coworking()
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
