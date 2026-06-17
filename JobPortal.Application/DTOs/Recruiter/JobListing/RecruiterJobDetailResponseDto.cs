using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.JobListing
{
    public class RecruiterJobDetailResponseDto
    {
        public Guid JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string JobDescription { get; set; } = string.Empty;

        public string TradeCategory { get; set; } = string.Empty;

        public string? Role { get; set; }

        public JobType JobType { get; set; }

        public string JobStatus { get; set; } = string.Empty;

        public int SalaryMin { get; set; }

        public int SalaryMax { get; set; }

        public string SalaryCurrency { get; set; } = string.Empty;

        public short Vacancies { get; set; }

        public byte ExperienceRequiredYears { get; set; }

        public string? EducationRequired { get; set; }

        public string? LanguageRequired { get; set; }

        public string? LicenceDocsRequired { get; set; }

        public string? KeySkills { get; set; }

        public string LocationType { get; set; } = string.Empty;

        public string? OnshoreCity { get; set; }

        public string? OnshoreState { get; set; }

        public string? OffshoreVesselName { get; set; }

        public string? OffshoreRegion { get; set; }

        public bool PassportRequired { get; set; }

        public DateOnly ApplicationDeadline { get; set; }

        public int AppliedCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
