using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using PdfSharpCore.Pdf.IO; // PdfSharp library for PDF page count
using System;

namespace snapprintweb.Controllers
{
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> _logger;

        public UploadController(ILogger<UploadController> logger)
        {
            _logger = logger;
        }

        // Display the file upload form
        [HttpGet]
        public IActionResult Index(string sessionId)
        {
            _logger.LogInformation($"Session ID received in URL: {sessionId}");

            // Store sessionId in HttpContext.Session if it's provided in the URL
            if (!string.IsNullOrEmpty(sessionId))
            {
                HttpContext.Session.SetString("SessionId", sessionId); // Store in session
                _logger.LogInformation($"Session ID stored in session: {sessionId}");
            }

            // Retrieve the session ID from HttpContext.Session (if available)
            ViewBag.SessionId = HttpContext.Session.GetString("SessionId");

            _logger.LogInformation($"Session ID retrieved from session: {ViewBag.SessionId}");

            // Display any error message from TempData
            ViewBag.ErrorMessage = TempData["ErrorMessage"] as string;

            // Temp data to store file path (for later use)
            ViewBag.TempFilePath = TempData["FilePath"] as string;

            return View();
        }



        // Handle file upload
        [HttpPost("api/upload/uploadfile")]
        public async Task<IActionResult> UploadFile(IFormFile file, string sessionId)
        {
            // Check if sessionId is provided via form, or retrieve it from the session if not provided
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = HttpContext.Session.GetString("SessionId") ?? string.Empty; // Retrieve from session
            }

            // If no sessionId found, show an error
            if (string.IsNullOrEmpty(sessionId))
            {
                TempData["ErrorMessage"] = "Session ID is required.";
                return RedirectToAction("Index");
            }

            // If no file is uploaded, return error
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "No file uploaded.";
                return RedirectToAction("Index");
            }

            // Validate file type (only PDF allowed)
            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
            {
                TempData["ErrorMessage"] = "Only PDF files are allowed.";
                return RedirectToAction("Index");
            }

            // Define the path where the file will be saved
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            // Create the uploads folder if it doesn't exist
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Construct a filename that includes the session ID for unique identification
            var fileNameWithSessionId = $"{sessionId}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadPath, fileNameWithSessionId);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            try
            {
                // Check PDF pages (max 10 pages)
                using (var pdfReader = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly))
                {
                    if (pdfReader.PageCount > 10)
                    {
                        TempData["ErrorMessage"] = "PDF file must have 10 or fewer pages.";
                        return RedirectToAction("Index");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error reading PDF file: " + ex.Message);
                TempData["ErrorMessage"] = "An error occurred while processing the PDF file.";
                return RedirectToAction("Index");
            }

            // Store the file path in TempData (for any additional processing if needed)
            TempData["FilePath"] = filePath;


            // Redirect to the Index view
            return RedirectToAction("Index");
        }

        // Error handling page
        [HttpGet("upload/error")]
        public IActionResult Error()
        {
            _logger.LogError("An error occurred.");
            return View("Error");
        }
    }
}