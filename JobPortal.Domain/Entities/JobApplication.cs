using JobPortal.Domain.Enums.RecruiterEnums;
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
    public ApplicationStatus ApplicationStatus { get; set; } = ApplicationStatus.Applied;
    public DateTime StatusUpdatedAt { get; set; }
    public Guid? StatusChangedBy { get; set; }
    public DateTime? ViewedAt { get; set; }
    public string? EmployerInternalNote { get; set; }
    public bool RejectionAutoNotify { get; set; } = true;
    public bool WithdrawalAllowed { get; set; } = true;
    public bool PassportGatePassed { get; set; } = true;

    public bool IsShortlisted { get; set; }

    public DateTime? ShortlistedAt { get; set; }

    public DateTime? InterviewScheduledAt { get; set; }

    public DateTime? RejectedAt { get; set; }

    // NEW
    public string? MotivationMessage { get; set; }

    // NEW
    public List<string>? ScreeningAnswers { get; set; }
    // Navigation
    public JobPosting JobPosting { get; set; } = default!;
    public CandidateProfile CandidateProfile { get; set; } = default!;
    public EmployerProfile EmployerProfile { get; set; } = default!;

    // Recruiter Notes
    public ICollection<RecruiterNote> RecruiterNotes { get; set; }
        = new List<RecruiterNote>();
}
