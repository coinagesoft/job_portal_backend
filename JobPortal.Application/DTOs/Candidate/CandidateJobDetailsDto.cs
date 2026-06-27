using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate
{
    public class CandidateJobDetailsDto
    {
        public Guid JobId { get; set; }

        // Company
        public string? CompanyLogoUrl { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLocation { get; set; }
        public string? CompanyLocationMapLink { get; set; }

        // Verification
        public List<string> VerificationBadges { get; set; } = new();

        public int ReviewCount { get; set; }

        // AI
        public int? AiMatchPercentage { get; set; }

        // Job
        public string JobTitle { get; set; } = string.Empty;
        public string JobLevel { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string IndustryType { get; set; } = default!;
        public bool? IsOilField { get; set; }
        public List<string> RequiredLicencesCertificates { get; set; }
        public string EmploymentType { get; set; } = string.Empty;
        public string EmploymentMode { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty;

        public string? JobLocation { get; set; }
        public string LocationType { get; set; } = string.Empty;

        public string SalaryRange { get; set; } = string.Empty;
        public string SalaryVisibility { get; set; } = string.Empty;

        public int ApplicationCount { get; set; }
        public short OpeningCount { get; set; }

        public DateTime? PostedOn { get; set; }
        public DateOnly ApplicationDeadline { get; set; }

        // Experience
        public byte ExperienceMinYears { get; set; }
        public byte ExperienceMaxYears { get; set; }

        // Eligibility
        public string? EducationRequired { get; set; }
        public byte? AgeMin { get; set; }
        public byte? AgeMax { get; set; }
        public string GenderPreferred { get; set; } = string.Empty;

        public bool DisabilityFriendly { get; set; }

        // Offshore / Oilfield
        public bool IsInternational { get; set; }
        public bool PassportRequired { get; set; }

        // Employment
        public byte? DutyHoursPerDay { get; set; }
        public bool PaidOvertime { get; set; }

        // Language
        public string? LanguagePreferred { get; set; }

        // Certificates

        // Description
        public string JobDescription { get; set; } = string.Empty;

        public List<string> KeyResponsibilities { get; set; } = new();

        public List<string> ProfessionalSkills { get; set; } = new();

        public List<string> PerksAndBenefits { get; set; } = new();
    }
}