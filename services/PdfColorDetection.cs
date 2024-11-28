using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.IO;

namespace snapprintweb.services
{
    public static class PdfColorDetection
    {
        public static string DetectColorStatus(string filePath)
        {
            try
            {
                using (var reader = new PdfReader(filePath))
                {
                    bool hasColor = false; // Flag to check if any page has color content

                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        var pageDict = reader.GetPageN(i);
                        var resources = pageDict.GetAsDict(PdfName.RESOURCES);

                        // Check for color in images on the page
                        if (resources != null)
                        {
                            var xObject = resources.GetAsDict(PdfName.XOBJECT);
                            if (xObject != null)
                            {
                                foreach (var key in xObject.Keys)
                                {
                                    var obj = PdfReader.GetPdfObject(xObject.Get(key));
                                    if (obj is PRStream stream)
                                    {
                                        var subtype = stream.GetAsName(PdfName.SUBTYPE);
                                        if (PdfName.IMAGE.Equals(subtype))
                                        {
                                            var colorSpace = stream.GetAsName(PdfName.COLORSPACE);
                                            if (colorSpace != null)
                                            {
                                                if (PdfName.DEVICERGB.Equals(colorSpace) || PdfName.DEVICECMYK.Equals(colorSpace))
                                                {
                                                    return "Colored"; // Return immediately if any page has color
                                                }
                                                else if (PdfName.DEVICEGRAY.Equals(colorSpace))
                                                {
                                                    continue; // Grayscale detected, continue checking
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Check for color-setting operations in text/content streams on the page
                        var contentBytes = reader.GetPageContent(i);
                        string contentString = System.Text.Encoding.UTF8.GetString(contentBytes);

                        if (contentString.Contains("/DeviceRGB") || contentString.Contains("/DeviceCMYK") ||
                            contentString.Contains("setrgbcolor") || contentString.Contains("setcmykcolor") ||
                            contentString.Contains("rg") || contentString.Contains("k"))
                        {
                            return "Colored"; // Return immediately if color is found
                        }
                    }

                    // If no color is detected across all pages, assume the file is grayscale
                    return "Greyscale";
                }
            }
            catch
            {
                return "Unknown"; // Fallback for unexpected errors
            }
        }
    }
}
