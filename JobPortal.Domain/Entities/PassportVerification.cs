using System;

namespace JobPortal.Domain.Entities;

public class PassportVerification
{
    public Guid VerificationId { get; set; }

    public Guid CandidateId { get; set; }

    // ===========================
    // Uploaded Images
    // ===========================

    public string FrontImageUrl { get; set; } = default!;

    public string? FrontPublicId { get; set; }

    public string? BackImageUrl { get; set; }

    public string? BackPublicId { get; set; }

    // ===========================
    // OCR
    // ===========================

    public string? AiExtractedName { get; set; }

    public DateOnly? AiExtractedDob { get; set; }

    public string? AiExtractedPassportNumber { get; set; }

    public string? AiExtractedNationality { get; set; }

    public DateOnly? AiExpiryDate { get; set; }

    public decimal? AiConfidenceScore { get; set; }

    public bool IsImportedToProfile { get; set; }

    public DateTime? ImportedAt { get; set; }

    // ===========================
    // Verification
    // ===========================

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