using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace JobPortal.Domain.Entities;

public class ItiCertificateReview
{
    public Guid ItiReviewId { get; set; }
    public Guid CandidateId { get; set; }
    public string ItiCertImageUrl { get; set; } = default!;
    public string? AiExtractedTrade { get; set; }
    public string? AiExtractedInstitute { get; set; }
    public short? AiExtractedYear { get; set; }
    public string? AiExtractedCertNo { get; set; }
    public decimal? AiConfidenceScore { get; set; }
    public string? AdminNote { get; set; }

    public string? ItiCertPublicId { get; set; }

    public bool IsImportedToProfile { get; set; }

    public DateTime? ImportedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; }

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;
}
