using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Domain.Entities;

public class CandidateCv
{
    public Guid CvId { get; set; }

    public Guid CandidateId { get; set; }

    // ===========================
    // Uploaded Resume
    // ===========================
    public string? CvFileUrl { get; set; }

    // Cloudinary Public Id
    public string? CvPublicId { get; set; }

    // Optional PDF generated from DOCX
    public string? CvPdfUrl { get; set; }

    // ===========================
    // Affinda
    // ===========================
    public string? AffindaJobId { get; set; }

    public decimal? AiConfidenceScore { get; set; }

    // ===========================
    // Parsed Basic Details
    // ===========================
    public string? ParsedName { get; set; }

    public string? ParsedEmail { get; set; }

    public string? ParsedPhone { get; set; }

    public string? ParsedTrade { get; set; }

    public int? ParsedExperienceYrs { get; set; }

    public string? ParsedSummary { get; set; }

    public string? ParsedCity { get; set; }

    public string? ParsedState { get; set; }

    public string? ParsedCountry { get; set; }

    // ===========================
    // Parsed JSON Data
    // ===========================

    // ["ASP.NET","C#","SQL"]
    public string? ParsedSkillsJson { get; set; }

    // Complete education list
    public string? ParsedEducationJson { get; set; }

    // Complete work history
    public string? ParsedWorkHistoryJson { get; set; }

    // Languages
    public string? ParsedLanguagesJson { get; set; }

    // Certifications
    public string? ParsedCertificatesJson { get; set; }

    // Projects (optional)
    public string? ParsedProjectsJson { get; set; }

    // Raw Affinda response (optional but useful)
    public string? ParsedRawJson { get; set; }

    // ===========================
    // Import Tracking
    // ===========================
    public bool IsImportedToProfile { get; set; } = false;

    public DateTime? ImportedAt { get; set; }

    // ===========================
    // Audit
    // ===========================
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ===========================
    // Navigation
    // ===========================
    [ForeignKey(nameof(CandidateId))]
    public virtual CandidateProfile CandidateProfile { get; set; } = default!;
}