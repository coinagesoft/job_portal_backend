using JobPortal.Application.DTOs.Recruiter.JobListing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{
    public class ApplicantDetailsResponseDto
    {
        public Guid ApplicationId { get; set; }

        public Guid CandidateId { get; set; }

        public Guid JobId { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string? ProfilePhotoUrl { get; set; }

        public string? PrimaryTrade { get; set; }

        public int TotalExperienceYears { get; set; }

        public string? CurrentCity { get; set; }

        public string? CurrentState { get; set; }

        public string? ProfessionalSummary { get; set; }

        public string ApplicationStatus { get; set; } = string.Empty;

        public bool IsShortlisted { get; set; }

        public DateTime AppliedAt { get; set; }

        public DateTime? ViewedAt { get; set; }

        public List<ApplicantEducationDto> Educations { get; set; }
            = new();

        public List<ApplicantWorkHistoryDto> WorkHistories { get; set; }
            = new();

        public List<ApplicantSkillDto> Skills { get; set; }
            = new();

        public List<ApplicantCvDto> Cvs { get; set; }
            = new();
    }
}
