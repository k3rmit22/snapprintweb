using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace snapprintweb.Controllers
{
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> _logger;

        public UploadController(ILogger<UploadController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index(string sessionId)
        {
            ViewBag.SessionId = sessionId;
            return View();
        }

        [HttpGet]
        public IActionResult Error()
        {
            // Log the error details
            _logger.LogError("An error occurred.");
            return View("Error"); // Renders the Error.cshtml view from Views/Upload/Error.cshtml
        }
    }
}
