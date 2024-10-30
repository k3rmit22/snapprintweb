using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UploadApiController : ControllerBase
{
    private readonly string _uploadDirectory = "C:\\SnapPrintKiosk\\Uploads"; // Ensure this matches your settings

    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile file, string sessionId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        // Ensure the directory exists
        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }

        // Save the file
        var filePath = Path.Combine(_uploadDirectory, file.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { message = "File uploaded successfully!" });
    }
}
