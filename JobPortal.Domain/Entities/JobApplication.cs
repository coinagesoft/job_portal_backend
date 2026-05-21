using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace JobPortal.Domain.Entities;

public class JobApplication
{
    public Guid ApplicationId { get; set; }
    public Guid JobId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid EmployerId { get; set; }
    public DateTime AppliedAt { get; set; }
    public string ApplicationStatus { get; set; } = "Applied";
    public DateTime StatusUpdatedAt { get; set; }
    public Guid? StatusChangedBy { get; set; }
    public DateTime? ViewedAt { get; set; }
    public string? EmployerInternalNote { get; set; }
    public bool RejectionAutoNotify { get; set; } = true;
    public bool WithdrawalAllowed { get; set; } = true;
    public bool PassportGatePassed { get; set; } = true;

    // Navigation
    public JobPosting JobPosting { get; set; } = default!;
    public CandidateProfile CandidateProfile { get; set; } = default!;
    public EmployerProfile EmployerProfile { get; set; } = default!;
}
