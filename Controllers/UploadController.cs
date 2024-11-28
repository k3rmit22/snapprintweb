using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using snapprintweb.Hubs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Pdf.Content.Objects;
using iTextSharp.text.pdf;
using snapprintweb.services;


namespace snapprintweb.Controllers
{
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IHubContext<FileUploadHub> _hubContext;

        public static Dictionary<string, (string FilePath, int PageCount, string PageSize, string ColorStatus )> FileDetailsCache =
            new Dictionary<string, (string, int, string, string)>();

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

       




        [HttpPost("api/upload/uploadfile")]
        public async Task<IActionResult> UploadFile(IFormFile file, string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                TempData["ErrorMessage"] = "Session Id is Required";
                return RedirectToAction("Index");
            }

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "No file Uploaded";
                return RedirectToAction("Index");
            }

            // Validate file type
            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Only PDF files are allowed.";
                return RedirectToAction("Index");
            }

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var sanitizedFileName = Path.GetFileName(file.FileName);
            var fileNameWithSessionId = $"{sessionId}_{sanitizedFileName}";
            var filePath = Path.Combine(uploadPath, fileNameWithSessionId);
            

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                int pageCount;
                string pageSizeType;
                var colorStatus = PdfColorDetection.DetectColorStatus(filePath);




                using (var pdfReader = PdfSharpCore.Pdf.IO.PdfReader.Open(filePath, PdfDocumentOpenMode.ReadOnly))
                {
                    pageCount = pdfReader.PageCount;

                    // Check if the page count exceeds 10
                    if (pageCount > 10)
                    {
                        CleanupFile(filePath);
                        TempData["ErrorMessage"] = "PDF file must have 10 or fewer pages.";
                        return RedirectToAction("Index");
                    }

                    var page = pdfReader.Pages[0];
                    var width = page.Width;
                    var height = page.Height;

                    pageSizeType = GetPageSizeType(width, height);
                    

                }

                // Store file details in memory
                FileDetailsCache[sessionId] = (filePath, pageCount, pageSizeType, colorStatus);

                await _hubContext.Clients.All.SendAsync("ReceiveMessage", $"File uploaded successfully for session {sessionId}");
                TempData["SuccessMessage"] = "File uploaded successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process the PDF file.");
                CleanupFile(filePath);
                TempData["ErrorMessage"] = "An error occurred while processing the file. Please try again.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }


        private void CleanupFile(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete file: {filePath}");
            }
        }



        // Helper method to get the page size type
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
                return "Letter (Short)";
            else if (Math.Abs(width - LegalWidth) < tolerance && Math.Abs(height - LegalHeight) < tolerance)
                return "Legal (Long)";
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
                TempData["ErrorMessage"] = "Session ID is required";
                return RedirectToAction("Index");
            }

            if (FileDetailsCache.TryGetValue(sessionId, out var details))
            {
                var fileInfo = new
                {
                    SessionId = sessionId,
                    FilePath = details.FilePath,
                    FileName = Path.GetFileName(details.FilePath),
                    PageCount = details.PageCount,
                    PageSize = details.PageSize,
                    ColorStatus = details.ColorStatus


                };

                return Ok(fileInfo);
            }

            TempData["ErrorMessage"] = "File information not found";
            return RedirectToAction("Index");
        }
    }
}
