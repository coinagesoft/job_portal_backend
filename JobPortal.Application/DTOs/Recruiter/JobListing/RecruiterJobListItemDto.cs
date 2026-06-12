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

        public string Location { get; set; } = string.Empty;

        public JobType JobType { get; set; }

        public string JobStatus { get; set; } = string.Empty;

        public int AppliedCount { get; set; }

        public int Vacancies { get; set; }

        public int SalaryMin { get; set; }

        public int SalaryMax { get; set; }

        public DateOnly ApplicationDeadline { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
