using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class CandidateWorkHistory
{
    public Guid WorkId { get; set; }
    public Guid CandidateId { get; set; }
    public string CompanyName { get; set; } = default!;
    public string JobTitle { get; set; } = default!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsCurrent { get; set; } = false;
    public string? JobDescription { get; set; }
    public string? WorkLocation { get; set; }
    public bool IsOffshore { get; set; } = false;

    // Navigation
    public CandidateProfile CandidateProfile { get; set; } = default!;
}
