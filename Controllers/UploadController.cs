using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using snapprintweb.Hubs;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf;
using Microsoft.Extensions.Logging;
using System;

namespace snapprintweb.Controllers
{
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IHubContext<FileUploadHub> _hubContext;

        public UploadController(ILogger<UploadController> logger, IHubContext<FileUploadHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
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
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = HttpContext.Session.GetString("SessionId") ?? string.Empty;
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                TempData["ErrorMessage"] = "Session ID is required.";
                return RedirectToAction("Index");
            }

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "No file uploaded.";
                return RedirectToAction("Index");
            }

            if ((file.ContentType != "application/pdf") && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Please upload a valid PDF file.";
                return RedirectToAction("Index");
            }

            if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
            {
                TempData["ErrorMessage"] = "Only PDF files are allowed.";
                return RedirectToAction("Index");
            }

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var fileNameWithSessionId = $"{sessionId}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadPath, fileNameWithSessionId);

            // Notify clients that the file upload is starting
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", $"File upload started for session {sessionId}");

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                _logger.LogInformation($" uploaded file at: {filePath}");
                await file.CopyToAsync(fileStream);
            }

            int pageCount;
            string pageSizeType;
            try
            {
                using (var pdfReader = PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly))
                {
                    pageCount = pdfReader.PageCount;
                    var firstPage = pdfReader.Pages[0];
                    pageSizeType = GetPageSizeType(firstPage.Width, firstPage.Height);

                    if (pageCount > 10)
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

            TempData["FilePath"] = filePath;
            TempData["PageCount"] = pageCount;
            TempData["PageSize"] = pageSizeType;
            TempData["SuccessMessage"] = "File uploaded successfully!";

            // Notify clients that the file upload is complete
            await _hubContext.Clients.All.SendAsync("ReceiveMessage", $"File uploaded successfully for session {sessionId}");

            return RedirectToAction("Index");
        }

        private string GetPageSizeType(double width, double height)
        {
            const double LetterWidth = 612;
            const double LetterHeight = 792;
            const double LegalWidth = 612;
            const double LegalHeight = 1008;
            const double A4Width = 595.28;
            const double A4Height = 841.89;
            const double tolerance = 0.5;

            if (Math.Abs(width - LetterWidth) < tolerance && Math.Abs(height - LetterHeight) < tolerance)
                return "Letter";
            else if (Math.Abs(width - LegalWidth) < tolerance && Math.Abs(height - LegalHeight) < tolerance)
                return "Legal";
            else if (Math.Abs(width - A4Width) < tolerance && Math.Abs(height - A4Height) < tolerance)
                return "A4";
            else
                return "Unknown size";
        }

        [HttpGet("api/upload/getfileinfo")]
        public IActionResult GetFileInfo(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Session ID is required.");
            }

            var filePath = TempData["FilePath"] as string;
            var fileName = Path.GetFileName(filePath);
            var pageCount = TempData["PageCount"] as int?;
            var pageSize = TempData["PageSize"] as string;

            if (filePath == null || pageCount == null || pageSize == null)
            {
                return NotFound("File information not found.");
            }

            var fileInfo = new
            {
                SessionId = sessionId,
                FilePath = filePath,
                FileName = fileName,
                PageCount = pageCount,
                PageSize = pageSize
            };

            _logger.LogInformation("File Info: {@FileInfo}", fileInfo);
            return Ok(fileInfo);
        }

        [HttpGet("upload/error")]
        public IActionResult Error()
        {
            _logger.LogError("An error occurred.");
            return View("Error");
        }
    }
}
