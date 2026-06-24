using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate
{
    public class CandidateJobListItemDto
    {
        public Guid JobId { get; set; }

        public string? CompanyLogoUrl { get; set; }

        public string? CompanyName { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string TradeCategory { get; set; } = string.Empty;

        public string? Department { get; set; }

        public string EmploymentType { get; set; } = string.Empty;

        public string EmploymentMode { get; set; } = string.Empty;

        public string JobType { get; set; } = string.Empty;

        public string JobLocation { get; set; } = string.Empty;

        public string CompanyLocation { get; set; } = string.Empty;

        public string SalaryDisplay { get; set; } = string.Empty;

        public string ExperienceDisplay { get; set; } = string.Empty;

        public short Vacancies { get; set; }

        public int ApplicationsCount { get; set; }

        public int ViewCount { get; set; }

        public DateTime? PostedOn { get; set; }

        public string TimeAgo { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<string> Skills { get; set; } = new();

        public bool IsFeatured { get; set; }

        public bool IsUrgentHiring { get; set; }

        public bool PassportRequired { get; set; }

        public bool IsInternational { get; set; }

        public int? AiMatchPercentage { get; set; }

        public bool CompanyVerified { get; set; }

        public DateOnly ApplicationDeadline { get; set; }

        public Guid EmployerId { get; set; }
    }
}
