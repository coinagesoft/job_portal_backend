using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class PassportVerification
{
    public Guid PassportVerId { get; set; }
    public Guid CandidateId { get; set; }
    public string PassportImageUrl { get; set; } = default!;
    public string? AiExtractedPassportNo { get; set; }
    public string? AiExtractedNationality { get; set; }
    public DateOnly? AiExtractedExpiryDate { get; set; }
    public string? AiExtractedFullName { get; set; }
    public bool ExpiryAutoFlagged { get; set; } = false;
    public decimal? AiConfidenceScore { get; set; }
    public string AdminDecision { get; set; } = "Pending";
    public string? RejectionReason { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;
    public AdminUser? Reviewer { get; set; }
}
