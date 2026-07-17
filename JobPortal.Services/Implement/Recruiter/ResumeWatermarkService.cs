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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    public class ResumeWatermarkService : IResumeWatermarkService
    {
        private readonly ILogger<ResumeWatermarkService> _logger;

        // IWebHostEnvironment.WebRootPath is the exact same property
        // LocalFileStorageService uses when it writes the file — using it
        // here too guarantees both sides agree on where "wwwroot" is,
        // regardless of how ContentRootPath happens to resolve for the
        // current launch profile / working directory.
        private readonly IWebHostEnvironment _env;

        public ResumeWatermarkService(
            ILogger<ResumeWatermarkService> logger,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        // ────────────────────────────────────────────────────────────
        // Public API
        // ────────────────────────────────────────────────────────────
        public async Task<byte[]> AddWatermarkAsync(
            string cvFileUrl,
            string downloadedByLabel,
            Guid referenceId,
            DateTime downloadedAt)
        {
            // 1. Resolve the PDF bytes from local storage or URL
            var originalBytes = await ReadPdfBytesAsync(cvFileUrl);

            // 2. Apply watermark in memory and return the result
            return ApplyWatermark(originalBytes, downloadedByLabel, referenceId, downloadedAt);
        }

        // ────────────────────────────────────────────────────────────
        // Step 1 – Read original PDF bytes
        // ────────────────────────────────────────────────────────────
        private async Task<byte[]> ReadPdfBytesAsync(string cvFileUrl)
        {
            var uploadsRoot = System.IO.Path.Combine(_env.WebRootPath, "uploads");

            // If the url is relative (local storage), build the full path.
            // Absolute URLs (http/https) are downloaded directly — unless
            // they actually point at this app's own /uploads/ folder, in
            // which case we read the file straight from disk instead.
            if (Uri.IsWellFormedUriString(cvFileUrl, UriKind.Absolute))
            {
                var uri = new Uri(cvFileUrl);
                const string localSegment = "/uploads/";
                var idx = uri.AbsolutePath.IndexOf(localSegment, StringComparison.OrdinalIgnoreCase);

                if (idx >= 0)
                {
                    // Locally-generated files (Portal CVs, uploaded resumes)
                    // are saved under wwwroot/uploads/... by LocalFileStorageService,
                    // which stamps the URL with whatever scheme/host answered
                    // the original request. Looping back over HttpClient to
                    // fetch that same URL is fragile — it silently 404s
                    // whenever static-file serving isn't wired up for that
                    // exact host/port, or the file was generated behind a
                    // different hostname (reverse proxy, tunnel, other dev
                    // port) than the one currently serving this request.
                    // Reading the file directly from disk sidesteps all of that.
                    var relativePath = uri.AbsolutePath[(idx + localSegment.Length)..]
                        .Replace('/', System.IO.Path.DirectorySeparatorChar);

                    var fullLocalPath = System.IO.Path.Combine(uploadsRoot, relativePath);

                    if (File.Exists(fullLocalPath))
                        return await File.ReadAllBytesAsync(fullLocalPath);

                    // The expected exact path doesn't exist — as a last resort,
                    // search for a file with the same name anywhere under
                    // wwwroot/uploads (covers a mismatched sub-folder without
                    // giving up entirely). The filename itself is a GUID, so
                    // a match here is effectively unambiguous.
                    var fileName = System.IO.Path.GetFileName(relativePath);
                    var found = Directory.Exists(uploadsRoot)
                        ? Directory.EnumerateFiles(uploadsRoot, fileName, SearchOption.AllDirectories).FirstOrDefault()
                        : null;

                    if (found != null)
                        return await File.ReadAllBytesAsync(found);

                    _logger.LogWarning(
                        "Portal CV URL {Url} looked local but no file named {FileName} was found under {UploadsRoot}; falling back to HTTP fetch.",
                        cvFileUrl,
                        fileName,
                        uploadsRoot);
                }

                using var http = new HttpClient();
                return await http.GetByteArrayAsync(cvFileUrl);
            }

            // Local path: wwwroot/uploads/resumes/abc.pdf
            var fullPath = System.IO.Path.Combine(uploadsRoot, cvFileUrl.TrimStart('/'));

            if (File.Exists(fullPath))
                return await File.ReadAllBytesAsync(fullPath);

            var bareFileName = System.IO.Path.GetFileName(cvFileUrl);
            var fallbackFound = Directory.Exists(uploadsRoot)
                ? Directory.EnumerateFiles(uploadsRoot, bareFileName, SearchOption.AllDirectories).FirstOrDefault()
                : null;

            if (fallbackFound != null)
                return await File.ReadAllBytesAsync(fallbackFound);

            throw new FileNotFoundException($"Resume not found at path: {fullPath}");
        }

        // ────────────────────────────────────────────────────────────
        // Step 2 – Stamp every page with a diagonal watermark
        // ────────────────────────────────────────────────────────────
        private static byte[] ApplyWatermark(
            byte[] pdfBytes,
            string downloadedByLabel,
            Guid referenceId,
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
                $"Downloaded by: {downloadedByLabel}",
                $"Reference ID: {referenceId.ToString()[..8].ToUpper()}",
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