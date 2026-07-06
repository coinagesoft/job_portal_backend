using System;

namespace JobPortal.Domain.Entities;

public class KycVerification
{
    public Guid VerificationId { get; set; }

    public Guid CandidateId { get; set; }

    // Aadhaar | PAN | Emirates_ID | PhilSys | Manual
    public string IdType { get; set; } = default!;

    // ===========================
    // Uploaded Documents
    // ===========================

    public string IdFrontImageUrl { get; set; } = default!;

    public string? IdFrontPublicId { get; set; }

    public string? IdBackImageUrl { get; set; }

    public string? IdBackPublicId { get; set; }

    // ===========================
    // OCR / AI Extracted Data
    // ===========================

    public string? AiExtractedName { get; set; }

    public DateOnly? AiExtractedDob { get; set; }

    public string? AiExtractedAddress { get; set; }

    public string? AiExtractedDocumentNumber { get; set; }

    public string? AiExtractedGender { get; set; }

    public decimal? AiConfidenceScore { get; set; }

    public decimal? OcrConfidence { get; set; }

    // Duplicate detection
    public string IdHash { get; set; } = default!;

    // ===========================
    // Verification
    // ===========================

    public bool IsImportedToProfile { get; set; }

    public DateTime? ImportedAt { get; set; }

    public string AdminDecision { get; set; } = "Pending";

    public string? RejectionReason { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    // ===========================
    // Audit
    // ===========================

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;

    public AdminUser? Reviewer { get; set; }
}