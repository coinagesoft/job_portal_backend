using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class PassportVerification
{
    public Guid VerificationId { get; set; }

    public Guid CandidateId { get; set; }

    public string FrontImageUrl { get; set; } = default!;

    public string? BackImageUrl { get; set; }

    public string? AiExtractedName { get; set; }

    public DateOnly? AiExtractedDob { get; set; }

    public decimal? AiConfidenceScore { get; set; }

    public string AdminDecision { get; set; } = "Pending";

    public string? RejectionReason { get; set; }

    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public CandidateProfile CandidateProfile { get; set; } = default!;

    public AdminUser? Reviewer { get; set; }
}
