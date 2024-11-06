using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;


[ApiController]
[Route("api/[controller]")]
public class UploadApiController : ControllerBase
{
    private readonly string _uploadDirectory;

    public UploadApiController(IConfiguration configuration)
    {
        // Set a default path if the configuration setting is missing
        _uploadDirectory = configuration["FileSettings:UploadDirectory"] ?? "C:\\SnapPrintKiosk\\Uploads";
    }

    [HttpPost]
    public async Task<IActionResult> UploadFile(IFormFile file, string sessionId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }

        var fileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{sessionId}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(_uploadDirectory, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { message = "File uploaded successfully!" });
    }
}

