using JobPortal.Domain.Enums.RecruiterEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.JobListing
{

    public class RecruiterJobListItemDto
    {
        public Guid JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string TradeCategory { get; set; } = string.Empty;
        
        public string? Role { get; set; }

        public bool? IsOilField { get; set; }

        public string Location { get; set; } = string.Empty;

        public string JobType { get; set; }

        public string JobStatus { get; set; } = string.Empty;

        public int AppliedCount { get; set; }

        public int Vacancies { get; set; }

        public int SalaryMin { get; set; }

        public int SalaryMax { get; set; }
        public string? Department { get; set; }

        public string? EmploymentType { get; set; }

        public string? EmploymentMode { get; set; }

        public bool IsActive { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsUrgentHiring { get; set; }

        public int ViewCount { get; set; }

        public byte ExperienceMinYears { get; set; }

        public byte ExperienceMaxYears { get; set; }

        public string? SalaryCurrency { get; set; }

        public string? SalaryDisplayOption { get; set; }

        public string? LocationType { get; set; }

        public DateOnly ApplicationDeadline { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
