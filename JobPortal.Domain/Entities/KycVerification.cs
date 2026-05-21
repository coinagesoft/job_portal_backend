using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class KycVerification
{
    public Guid VerificationId { get; set; }
    public Guid CandidateId { get; set; }
    public string IdType { get; set; } = default!;  // Aadhaar|Emirates_ID|PhilSys|Passport|Manual
    public string IdFrontImageUrl { get; set; } = default!;
    public string? IdBackImageUrl { get; set; }
    public string? AiExtractedName { get; set; }
    public DateOnly? AiExtractedDob { get; set; }
    public string? AiExtractedAddress { get; set; }
    public decimal? AiConfidenceScore { get; set; }
    public string IdHash { get; set; } = default!;   // SHA-256
    public decimal? OcrConfidence { get; set; }
    public string AdminDecision { get; set; } = "Pending";
    public string? RejectionReason { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;
    public AdminUser? Reviewer { get; set; }
}
