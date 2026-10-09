// ============================================================
//  JobPortal.Services/Implement/Recruiter/
//  ResumeWatermarkService.cs
// ============================================================
//
//  NuGet packages: itext7, AWSSDK.S3 (JobPortal.Services.csproj)
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
using Microsoft.Extensions.Logging;

namespace JobPortal.Services.Implement.Recruiter
{
    public class ResumeWatermarkService : IResumeWatermarkService
    {
        private readonly ILogger<ResumeWatermarkService> _logger;

        // Reads the original CV straight from the private S3 bucket
        // (via the EC2 IAM role) instead of from wwwroot/uploads on disk.
        private readonly S3FileStorageService _storage;

        public ResumeWatermarkService(
            ILogger<ResumeWatermarkService> logger,
            S3FileStorageService storage)
        {
            _logger = logger;
            _storage = storage;
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
            string key;

            if (Uri.IsWellFormedUriString(cvFileUrl, UriKind.Absolute))
            {
                var uri = new Uri(cvFileUrl);
                const string seg = "/uploads/";
                var idx = uri.AbsolutePath.IndexOf(seg, StringComparison.OrdinalIgnoreCase);

                if (idx < 0)
                {
                    // Not one of our own /uploads/ URLs -> download normally
                    using var http = new HttpClient();
                    return await http.GetByteArrayAsync(cvFileUrl);
                }

                // https://host/uploads/resumes/abc.pdf  ->  key "resumes/abc.pdf"
                key = Uri.UnescapeDataString(uri.AbsolutePath[(idx + seg.Length)..]);
            }
            else
            {
                // Relative value such as "resumes/abc.pdf" or "/uploads/resumes/abc.pdf"
                key = cvFileUrl.TrimStart('/');
                if (key.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
                    key = key["uploads/".Length..];
            }

            var bytes = await _storage.GetBytesAsync(key);

            if (bytes == null)
            {
                _logger.LogWarning("Resume not found in S3 for key {Key} (source {Url})", key, cvFileUrl);
                throw new FileNotFoundException($"Resume not found in S3: {key}");
            }

            return bytes;
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

            for (int i = 1; i <= pageCount; i++)
            {
                var page = pdf.GetPage(i);
                var pageSize = page.GetPageSize();

                var pdfCanvas = new PdfCanvas(page);
                var canvas = new Canvas(pdfCanvas, pageSize);

                var footer =
                    $"Downloaded from JOB PORTAL | Downloaded by: {downloadedByLabel} | " +
                    $"Ref: {referenceId.ToString()[..8].ToUpper()} | " +
                    $"{downloadedAt:dd-MMM-yyyy HH:mm} UTC";

                canvas.ShowTextAligned(
                    new Paragraph(footer)
                        .SetFont(font)
                        .SetFontSize(8)
                        .SetFontColor(ColorConstants.GRAY),
                    pageSize.GetWidth() / 2,
                    15,
                    TextAlignment.CENTER
                );

                canvas.Close();
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