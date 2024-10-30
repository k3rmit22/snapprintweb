using Microsoft.AspNetCore.Mvc;

namespace snapprintweb.Controllers
{
    public class UploadController : Controller
    {
        [HttpGet]
        public IActionResult Index(string sessionId)
        {
            // You can pass the sessionId to the view if needed
            ViewBag.SessionId = sessionId;
            return View();
        }
    }
}
