using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using JobPortal.Application.DTOs.Candidate;
using JobPortal.Application.DTOs.Recruiter.CreditWallet;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Candidate
{
    /// <summary>
    /// Builds a "Portal CV" — a PDF resume generated from the candidate's
    /// current profile data (not the originally uploaded resume file) using
    /// a fixed default template. Kept entirely separate from the uploaded
    /// resume: CandidateProfile.GeneratedCvFileUrl vs CandidateCv.CvFileUrl.
    /// Regenerating replaces only the previous Portal CV file.
    /// </summary>
    public class CvGenerationService : ICvGenerationService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorage;
        private readonly IResumeWatermarkService _watermark;

        public CvGenerationService(
            AppDbContext context,
            IFileStorageService fileStorage,
            IResumeWatermarkService watermark)
        {
            _context = context;
            _fileStorage = fileStorage;
            _watermark = watermark;
        }

        public async Task<GenerateCvResponseDto> GenerateCvAsync(Guid candidateId)
        {
            var profile = await _context.CandidateProfiles
                .Include(p => p.User)
                .Include(p => p.WorkHistories)
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new GenerateCvResponseDto
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };

            byte[] pdfBytes;
            try
            {
                pdfBytes = BuildPdf(profile);
            }
            catch (Exception ex)
            {
                return new GenerateCvResponseDto
                {
                    Success = false,
                    Message = "Failed to generate CV: " + ex.Message
                };
            }

            // Remove the previous generated file (never the originally
            // uploaded resume — that lives in a separate table untouched).
            if (!string.IsNullOrWhiteSpace(profile.GeneratedCvPublicId))
            {
                await _fileStorage.DeleteAsync(profile.GeneratedCvPublicId);
            }

            var fileName = $"{Guid.NewGuid()}.pdf";
            var uploadResult = await _fileStorage.UploadBytesAsync(
                pdfBytes,
                "generated-cvs",
                fileName,
                "application/pdf");

            profile.GeneratedCvFileUrl = uploadResult.Url;
            profile.GeneratedCvPublicId = uploadResult.PublicId;
            profile.GeneratedCvUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new GenerateCvResponseDto
            {
                Success = true,
                Message = "Portal CV generated successfully.",
                GeneratedCvUrl = profile.GeneratedCvFileUrl,
                GeneratedAt = profile.GeneratedCvUpdatedAt
            };
        }

        public async Task<WatermarkedCvResult> DownloadOwnGeneratedCvAsync(Guid candidateId)
        {
            var profile = await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return new WatermarkedCvResult
                {
                    Success = false,
                    Message = "Candidate profile not found."
                };

            if (string.IsNullOrWhiteSpace(profile.GeneratedCvFileUrl))
                return new WatermarkedCvResult
                {
                    Success = false,
                    Message = "No Portal CV has been generated yet. Generate one first."
                };

            var bytes = await _watermark.AddWatermarkAsync(
                profile.GeneratedCvFileUrl,
                profile.FullName ?? "Candidate",
                candidateId,
                DateTime.UtcNow);

            var safeName = new string(
                (profile.FullName ?? "Candidate")
                    .Where(ch => char.IsLetterOrDigit(ch) || ch == ' ')
                    .ToArray())
                .Trim()
                .Replace(' ', '_');

            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "Candidate";

            return new WatermarkedCvResult
            {
                Success = true,
                Message = "Portal CV downloaded.",
                FileBytes = bytes,
                FileName = $"{safeName}_Portal_CV.pdf"
            };
        }

        // ────────────────────────────────────────────────────────────
        // PDF layout — default portal template
        // ────────────────────────────────────────────────────────────
        private static byte[] BuildPdf(CandidateProfile profile)
        {
            using var stream = new MemoryStream();
            using var writer = new PdfWriter(stream);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, PageSize.A4);
            document.SetMargins(40, 40, 40, 40);

            var navy = new DeviceRgb(18, 35, 89);
            var orange = new DeviceRgb(255, 163, 0);
            var grey = new DeviceRgb(102, 120, 156);
            var lightGrey = new DeviceRgb(225, 229, 238);
            var white = new DeviceRgb(255, 255, 255);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Helvetica has no Devanagari (Marathi / Hindi) glyphs, so any such
            // text rendered with it comes out blank. Those runs are drawn with an
            // embedded Noto Sans Devanagari instead (null if the font files are
            // missing, in which case behaviour is unchanged from before).
            var devRegular = LoadDevanagariFont("NotoSansDevanagari-Regular.ttf");
            var devBold = LoadDevanagariFont("NotoSansDevanagari-Bold.ttf") ?? devRegular;

            // Appends `text` to `target`, switching font per script run.
            void AddRuns(Paragraph target, string? text, bool bold, Color? color = null)
            {
                if (string.IsNullOrEmpty(text)) return;

                foreach (var (segment, isDevanagari) in SplitByScript(text))
                {
                    PdfFont font = isDevanagari && devRegular != null
                        ? (bold ? devBold! : devRegular)
                        : (bold ? boldFont : regularFont);

                    var run = new Text(segment).SetFont(font);
                    if (color != null) run.SetFontColor(color);
                    target.Add(run);
                }
            }

            Paragraph P(string? text, bool bold = false, Color? color = null)
            {
                var para = new Paragraph();
                AddRuns(para, text, bold, color);
                return para;
            }

            void SectionHeader(string title)
            {
                var header = new Div()
                    .SetMarginTop(14)
                    .SetMarginBottom(8)
                    .SetPaddingBottom(4)
                    .SetBorderBottom(new SolidBorder(lightGrey, 1f));

                header.Add(
                    new Paragraph()
                        .SetMargin(0)
                        .Add(new Text("| ").SetFont(boldFont).SetFontColor(orange).SetFontSize(12.5f))
                        .Add(new Text(title.ToUpperInvariant()).SetFont(boldFont).SetFontColor(navy).SetFontSize(12.5f)));

                document.Add(header);
            }

            // ── Header band (navy background, white text) ───────────
            var headerDiv = new Div()
                .SetBackgroundColor(navy)
                .SetPadding(18)
                .SetMarginBottom(16);

            headerDiv.Add(
                P((profile.FullName ?? "Candidate").ToUpperInvariant(), bold: true, color: white)
                    .SetFontSize(23)
                    .SetMargin(0)
                    .SetMarginBottom(2));

            if (!string.IsNullOrWhiteSpace(profile.Role))
            {
                headerDiv.Add(
                    P(profile.Role!.ToUpperInvariant(), bold: true, color: orange)
                        .SetFontSize(12)
                        .SetMargin(0)
                        .SetMarginBottom(10));
            }

            var contactParts = new List<string>();
            if (profile.User != null && !string.IsNullOrWhiteSpace(profile.User.MobileNumber))
                contactParts.Add($"Mobile: {profile.User.CountryCode} {profile.User.MobileNumber}");
            if (profile.User != null && !string.IsNullOrWhiteSpace(profile.User.Email))
                contactParts.Add($"Email: {profile.User.Email}");

            var location = string.Join(
                ", ",
                new[] { profile.CurrentCity, profile.CurrentState }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));
            if (!string.IsNullOrWhiteSpace(location))
                contactParts.Add($"Location: {location}");

            if (contactParts.Count > 0)
            {
                headerDiv.Add(
                    P(string.Join("     ", contactParts), color: new DeviceRgb(200, 212, 238))
                        .SetFontSize(10)
                        .SetMargin(0));
            }

            document.Add(headerDiv);

            // ── Summary ─────────────────────────────────────────────
            // The Personal tab's "Professional Summary" field is bound to
            // the About column end-to-end (see CandidateProfileService /
            // frontend Personal tab) — use it directly.
            var summary = profile.About;
            if (!string.IsNullOrWhiteSpace(summary))
            {
                SectionHeader("Professional Summary");
                document.Add(
                    P(summary)
                        .SetFontSize(10.5f)
                        .SetMarginBottom(6));
            }

            // ── Work Experience ─────────────────────────────────────
            if (profile.WorkHistories?.Any() == true)
            {
                SectionHeader("Work Experience");

                foreach (var w in profile.WorkHistories.OrderByDescending(x => x.StartDate))
                {
                    var titleLine = new Paragraph().SetFontSize(11).SetMarginBottom(1);
                    AddRuns(titleLine, w.JobTitle ?? "", bold: true);
                    if (!string.IsNullOrWhiteSpace(w.CompanyName))
                        AddRuns(titleLine, $"  -  {w.CompanyName}", bold: false);
                    document.Add(titleLine);

                    var metaParts = new List<string>();
                    var dateRange = FormatDateRange(w.StartDate, w.EndDate, w.IsCurrent);
                    if (!string.IsNullOrWhiteSpace(dateRange)) metaParts.Add(dateRange);
                    if (!string.IsNullOrWhiteSpace(w.WorkLocation)) metaParts.Add(w.WorkLocation);

                    if (metaParts.Count > 0)
                    {
                        document.Add(
                            P(string.Join("   |   ", metaParts), color: grey)
                                .SetFontSize(9.5f)
                                .SetMarginBottom(4));
                    }

                    if (!string.IsNullOrWhiteSpace(w.JobDescription))
                    {
                        document.Add(
                            P(w.JobDescription)
                                .SetFontSize(10)
                                .SetMarginBottom(8));
                    }
                }
            }

            // ── Education ────────────────────────────────────────────
            if (profile.Educations?.Any() == true)
            {
                SectionHeader("Education");

                foreach (var e in profile.Educations.OrderByDescending(x => x.PassoutYear))
                {
                    var line = new Paragraph().SetFontSize(10.5f).SetMarginBottom(1);
                    AddRuns(line, e.EducationLevel ?? "", bold: true);
                    if (!string.IsNullOrWhiteSpace(e.InstituteName))
                        AddRuns(line, $"  -  {e.InstituteName}", bold: false);
                    document.Add(line);

                    if (e.PassoutYear.HasValue)
                    {
                        document.Add(
                            P($"Year: {e.PassoutYear}", color: grey)
                                .SetFontSize(9.5f)
                                .SetMarginBottom(6));
                    }
                }
            }

            // ── Skills ───────────────────────────────────────────────
            var skills = profile.Skills?
                .Where(s => s.SkillType == "Skill")
                .Select(s => s.SkillName)
                .ToList();

            if (skills?.Count > 0)
            {
                SectionHeader("Core Skills");
                document.Add(
                    P(string.Join("   |   ", skills))
                        .SetFontSize(10.5f)
                        .SetMarginBottom(6));
            }

            // ── Languages ────────────────────────────────────────────
            var languages = profile.Skills?
                .Where(s => s.SkillType == "Language")
                .ToList();

            if (languages?.Count > 0)
            {
                SectionHeader("Languages");

                foreach (var l in languages)
                {
                    var abilities = new List<string>();
                    if (l.CanRead == true) abilities.Add("Read");
                    if (l.CanWrite == true) abilities.Add("Write");
                    if (l.CanSpeak == true) abilities.Add("Speak");
                    var abilityText = abilities.Count > 0 ? $" ({string.Join("/", abilities)})" : "";

                    document.Add(
                        P($"{l.SkillName}{abilityText}")
                            .SetFontSize(10.5f)
                            .SetMarginBottom(2));
                }
            }

            // ── Footer note ──────────────────────────────────────────
            document.Add(
                new Paragraph($"Generated via JobPortal on {DateTime.UtcNow:dd-MMM-yyyy}")
                    .SetFont(regularFont)
                    .SetFontSize(8)
                    .SetFontColor(grey)
                    .SetMarginTop(16));

            document.Close();
            return stream.ToArray();
        }

        // ────────────────────────────────────────────────────────────
        // Unicode font helpers
        // ────────────────────────────────────────────────────────────

        private static PdfFont? LoadDevanagariFont(string fileName)
        {
            try
            {
                var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts", fileName);
                if (!System.IO.File.Exists(path)) return null;

                return PdfFontFactory.CreateFont(
                    System.IO.File.ReadAllBytes(path),
                    PdfEncodings.IDENTITY_H,
                    PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsDevanagari(char c) =>
            (c >= '\u0900' && c <= '\u097F') || (c >= '\uA8E0' && c <= '\uA8FF');

        /// <summary>
        /// Splits text into runs of Devanagari vs everything else. Neutral
        /// characters (spaces, digits, punctuation) stay with the run they
        /// follow so words are not fragmented.
        /// </summary>
        private static IEnumerable<(string Text, bool IsDevanagari)> SplitByScript(string text)
        {
            var sb = new System.Text.StringBuilder();
            bool current = false;

            foreach (var ch in text)
            {
                bool neutral = char.IsWhiteSpace(ch) || char.IsDigit(ch) || char.IsPunctuation(ch)
                               || ch == '\u200C' || ch == '\u200D';
                bool dev = neutral ? current : IsDevanagari(ch);

                if (sb.Length > 0 && dev != current)
                {
                    yield return (sb.ToString(), current);
                    sb.Clear();
                }

                current = dev;
                sb.Append(ch);
            }

            if (sb.Length > 0)
                yield return (sb.ToString(), current);
        }

        private static string FormatDateRange(DateOnly? start, DateOnly? end, bool isCurrent)
        {
            string Fmt(DateOnly? d) => d.HasValue ? d.Value.ToString("MMM yyyy") : "Unknown";
            var startText = Fmt(start);
            var endText = isCurrent ? "Present" : Fmt(end);
            return $"{startText} – {endText}";
        }
    }
}