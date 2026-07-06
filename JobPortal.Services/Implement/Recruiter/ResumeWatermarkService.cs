// ============================================================
//  JobPortal.Services/Implement/Recruiter/
//  ResumeWatermarkService.cs
// ============================================================
//
//  NuGet package required (add to JobPortal.Services.csproj):
//  <PackageReference Include="itext7" Version="8.0.5" />
//
// ============================================================

using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    public class ResumeWatermarkService : IResumeWatermarkService
    {
        private readonly ILogger<ResumeWatermarkService> _logger;

        // IHostEnvironment is used to resolve the wwwroot path for
        // resumes stored locally (CvFileUrl = "resumes/abc.pdf").
        private readonly IHostEnvironment _env;

        public ResumeWatermarkService(
            ILogger<ResumeWatermarkService> logger,
            IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        // ────────────────────────────────────────────────────────────
        // Public API
        // ────────────────────────────────────────────────────────────
        public async Task<byte[]> AddWatermarkAsync(
            string cvFileUrl,
            string employerName,
            Guid employerId,
            DateTime downloadedAt)
        {
            // 1. Resolve the PDF bytes from local storage or URL
            var originalBytes = await ReadPdfBytesAsync(cvFileUrl);

            // 2. Apply watermark in memory and return the result
            return ApplyWatermark(originalBytes, employerName, employerId, downloadedAt);
        }

        // ────────────────────────────────────────────────────────────
        // Step 1 – Read original PDF bytes
        // ────────────────────────────────────────────────────────────
        private async Task<byte[]> ReadPdfBytesAsync(string cvFileUrl)
        {
            // If the url is relative (local storage), build the full path.
            // Absolute URLs (http/https) are downloaded directly.
            if (Uri.IsWellFormedUriString(cvFileUrl, UriKind.Absolute))
            {
                using var http = new HttpClient();
                return await http.GetByteArrayAsync(cvFileUrl);
            }

            // Local path: wwwroot/uploads/resumes/abc.pdf
            var wwwroot = System.IO.Path.Combine(_env.ContentRootPath, "wwwroot");
            var fullPath = System.IO.Path.Combine(wwwroot, "uploads", cvFileUrl.TrimStart('/'));

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Resume not found at path: {fullPath}");

            return await File.ReadAllBytesAsync(fullPath);
        }

        // ────────────────────────────────────────────────────────────
        // Step 2 – Stamp every page with a diagonal watermark
        // ────────────────────────────────────────────────────────────
        private static byte[] ApplyWatermark(
            byte[] pdfBytes,
            string employerName,
            Guid employerId,
            DateTime downloadedAt)
        {
            using var inputStream = new MemoryStream(pdfBytes);
            using var outputStream = new MemoryStream();

            using var reader = new PdfReader(inputStream);
            using var writer = new PdfWriter(outputStream);
            using var pdf = new PdfDocument(reader, writer);

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var pageCount = pdf.GetNumberOfPages();

            // Watermark lines shown on every page
            var lines = new[]
            {
                "JOB PORTAL",
                "CONFIDENTIAL",
                $"Downloaded by: {employerName}",
                $"Employer ID: {employerId.ToString()[..8].ToUpper()}",
                $"Downloaded On: {downloadedAt:dd-MMM-yyyy HH:mm} UTC",
                "www.jobportal.com"
            };

            for (int i = 1; i <= pageCount; i++)
            {
                var page = pdf.GetPage(i);
                var pageSize = page.GetPageSize();
                var canvas = new PdfCanvas(page);

                // Save graphics state, set opacity to ~25 %
                canvas.SaveState();
                var extGState = new iText.Kernel.Pdf.Extgstate.PdfExtGState();
                extGState.SetFillOpacity(0.18f);
                canvas.SetExtGState(extGState);

                // Rotate 45° around the page centre (diagonal)
                float cx = pageSize.GetWidth() / 2f;
                float cy = pageSize.GetHeight() / 2f;

                canvas.ConcatMatrix(
                    AffineTransformOf45Degrees(cx, cy));

                // Draw each line centred around the rotation pivot
                float fontSize = 22f;
                float lineGap = 30f;
                float totalH = lines.Length * lineGap;
                float startY = cy + totalH / 2f;

                canvas.SetFillColor(new DeviceRgb(105, 105, 105)); // dark-grey
                canvas.SetFontAndSize(font, fontSize);

                foreach (var line in lines)
                {
                    float textWidth = font.GetWidth(line, fontSize);
                    float x = cx - textWidth / 2f;

                    canvas.BeginText()
                          .MoveText(x, startY)
                          .ShowText(line)
                          .EndText();

                    startY -= lineGap;
                }

                canvas.RestoreState();
            }

            pdf.Close();
            return outputStream.ToArray();
        }

        // Build a 45-degree rotation matrix around (cx, cy)
        private static iText.Kernel.Geom.AffineTransform AffineTransformOf45Degrees(
            float cx, float cy)
        {
            double angle = Math.PI / 4; // 45°
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            // Rotate around the centre point:
            //  translate(-cx, -cy), rotate, translate(cx, cy)
            return new iText.Kernel.Geom.AffineTransform(
                cos, sin, -sin, cos,
                cx * (1 - cos) + cy * sin,
                cy * (1 - cos) - cx * sin);
        }
    }
}