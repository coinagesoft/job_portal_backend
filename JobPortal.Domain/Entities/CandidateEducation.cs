using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class CandidateEducation
{
    public Guid EducationId { get; set; }
    public Guid CandidateId { get; set; }
    public string EducationLevel { get; set; } = default!; // 10th|12th|ITI|Diploma|Graduate|Other
    public string? InstituteName { get; set; }
    public string? MarksPercentage { get; set; }
    public short? PassoutYear { get; set; }
    public string? CertificateUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? YearDetails { get; set; }
    public bool IsAiVerified { get; set; } = false;

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;
}
