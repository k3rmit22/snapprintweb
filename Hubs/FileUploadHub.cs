using Microsoft.AspNetCore.SignalR;
using PdfSharpCore;
using System.Threading.Tasks;

namespace snapprintweb.Hubs
{
    public class FileUploadHub :Hub 
    {
        public async Task SendFileDetails(string sessionId, string filePath, string fileName, string pageSize, int pageCount)
        {
            // Notify clients about the uploaded file details
            await Clients.All.SendAsync("ReceiveFileDetails", sessionId, filePath,fileName, pageSize, pageCount);
        }
    }
}
