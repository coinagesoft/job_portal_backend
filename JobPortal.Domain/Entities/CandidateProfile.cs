using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class CandidateProfile
{
    public Guid CandidateId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = default!;
    public string? ProfilePhotoUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }                 // Male | Female | Prefer_Not_To_Say
    public string? Nationality { get; set; }
    public string? CurrentCity { get; set; }
    public string? CurrentState { get; set; }
    public string? PreferredWorkLocation { get; set; }
    public int? PreferredSalary { get; set; }
    public string AvailabilityStatus { get; set; } = "Available";
    public DateTime? AvailabilityUpdatedAt { get; set; }
    public bool DisabilityStatus { get; set; } = false;
    public string? DisabilityNote { get; set; }
    public string? PrimaryTrade { get; set; }
    public int TotalExperienceYears { get; set; } = 0;
    public bool ItiCertified { get; set; } = false;
    public string? ItiTrade { get; set; }
    public string? ItiMarks { get; set; }
    public string? ItiCollege { get; set; }
    public string? Band { get; set; }
    public byte? AiMatchScore { get; set; }
    public string ProfileStatus { get; set; } = "Active";
    public byte ProfileCompletionPct { get; set; } = 0;
    public string? ReengagementResponse { get; set; }
    public DateTime? LastAppliedAt { get; set; }
    public string? FcmToken { get; set; }
    public string? AdminNotes { get; set; }
    public bool WelcomeEmailSent { get; set; } = false;
    public bool NewsletterOptIn { get; set; } = false;
    public bool TempPasswordFlag { get; set; } = false;
    public string? Pincode { get; set; }

    public string? ProfessionalSummary { get; set; }

    public string? About { get; set; }

    public string? NoticePeriod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public User User { get; set; } = default!;
    public ICollection<CandidateEducation> Educations { get; set; } = new List<CandidateEducation>();
    public ICollection<CandidateWorkHistory> WorkHistories { get; set; } = new List<CandidateWorkHistory>();
    public ICollection<CandidateSkill> Skills { get; set; } = new List<CandidateSkill>();
    public ICollection<CandidateCv> Cvs { get; set; } = new List<CandidateCv>();
    public CandidateNotificationSetting? NotificationSetting { get; set; }

    public CandidatePreferenceSetting? PreferenceSetting { get; set; }

}


