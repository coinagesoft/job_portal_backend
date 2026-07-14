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
            var grey = new DeviceRgb(102, 120, 156);
            var lightGrey = new DeviceRgb(225, 229, 238);

            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            void SectionHeader(string title)
            {
                document.Add(
                    new Paragraph(title.ToUpperInvariant())
                        .SetFont(boldFont)
                        .SetFontSize(12.5f)
                        .SetFontColor(navy)
                        .SetMarginTop(6)
                        .SetMarginBottom(6)
                        .SetPaddingBottom(4)
                        .SetBorderBottom(new SolidBorder(lightGrey, 1f)));
            }

            // ── Header ──────────────────────────────────────────────
            document.Add(
                new Paragraph((profile.FullName ?? "Candidate").ToUpperInvariant())
                    .SetFont(boldFont)
                    .SetFontSize(22)
                    .SetFontColor(navy)
                    .SetMarginBottom(2));

            if (!string.IsNullOrWhiteSpace(profile.Role))
            {
                document.Add(
                    new Paragraph(profile.Role!.ToUpperInvariant())
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetFontColor(grey)
                        .SetMarginBottom(8));
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
                document.Add(
                    new Paragraph(string.Join("     ", contactParts))
                        .SetFont(regularFont)
                        .SetFontSize(10)
                        .SetFontColor(grey)
                        .SetPaddingBottom(10)
                        .SetBorderBottom(new SolidBorder(navy, 1.2f)));
            }

            // ── Summary ─────────────────────────────────────────────
            var summary = profile.About ?? profile.ProfessionalSummary;
            if (!string.IsNullOrWhiteSpace(summary))
            {
                SectionHeader("Professional Summary");
                document.Add(
                    new Paragraph(summary)
                        .SetFont(regularFont)
                        .SetFontSize(10.5f)
                        .SetMarginBottom(6));
            }

            // ── Work Experience ─────────────────────────────────────
            if (profile.WorkHistories?.Any() == true)
            {
                SectionHeader("Work Experience");

                foreach (var w in profile.WorkHistories.OrderByDescending(x => x.StartDate))
                {
                    var titleLine = new Paragraph().SetMarginBottom(1);
                    titleLine.Add(new Text(w.JobTitle ?? "").SetFont(boldFont).SetFontSize(11));
                    if (!string.IsNullOrWhiteSpace(w.CompanyName))
                        titleLine.Add(new Text($"  —  {w.CompanyName}").SetFont(regularFont).SetFontSize(11));
                    document.Add(titleLine);

                    var metaParts = new List<string>();
                    var dateRange = FormatDateRange(w.StartDate, w.EndDate, w.IsCurrent);
                    if (!string.IsNullOrWhiteSpace(dateRange)) metaParts.Add(dateRange);
                    if (!string.IsNullOrWhiteSpace(w.WorkLocation)) metaParts.Add(w.WorkLocation);

                    if (metaParts.Count > 0)
                    {
                        document.Add(
                            new Paragraph(string.Join("   •   ", metaParts))
                                .SetFont(regularFont)
                                .SetFontSize(9.5f)
                                .SetFontColor(grey)
                                .SetMarginBottom(4));
                    }

                    if (!string.IsNullOrWhiteSpace(w.JobDescription))
                    {
                        document.Add(
                            new Paragraph(w.JobDescription)
                                .SetFont(regularFont)
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
                    var line = new Paragraph().SetMarginBottom(1);
                    line.Add(new Text(e.EducationLevel ?? "").SetFont(boldFont).SetFontSize(10.5f));
                    if (!string.IsNullOrWhiteSpace(e.InstituteName))
                        line.Add(new Text($"  —  {e.InstituteName}").SetFont(regularFont).SetFontSize(10.5f));
                    document.Add(line);

                    if (e.PassoutYear.HasValue)
                    {
                        document.Add(
                            new Paragraph($"Year: {e.PassoutYear}")
                                .SetFont(regularFont)
                                .SetFontSize(9.5f)
                                .SetFontColor(grey)
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
                    new Paragraph(string.Join("   •   ", skills))
                        .SetFont(regularFont)
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
                        new Paragraph($"{l.SkillName}{abilityText}")
                            .SetFont(regularFont)
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

        private static string FormatDateRange(DateOnly? start, DateOnly? end, bool isCurrent)
        {
            string Fmt(DateOnly? d) => d.HasValue ? d.Value.ToString("MMM yyyy") : "Unknown";
            var startText = Fmt(start);
            var endText = isCurrent ? "Present" : Fmt(end);
            return $"{startText} – {endText}";
        }
    }
}