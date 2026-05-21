using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class CandidateCv
{
    public Guid CvId { get; set; }
    public Guid CandidateId { get; set; }
    public string? CvFileUrl { get; set; }
    public string? CvPdfUrl { get; set; }
    public string? CvS3Url { get; set; }
    public string? AffindaJobId { get; set; }
    public string? ParsedName { get; set; }
    public string? ParsedPhone { get; set; }
    public string? ParsedEmail { get; set; }
    public string? ParsedTrade { get; set; }
    public int? ParsedExperienceYrs { get; set; }
    public string? ParsedSkills { get; set; }           // JSON array stored as string
    public decimal? AiConfidenceScore { get; set; }
    public DateTime? GeneratedAt { get; set; }

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;
}
