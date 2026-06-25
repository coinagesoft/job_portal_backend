using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate
{
    public class ApplyJobDetailsResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid JobId { get; set; }

        // Company

        public string? CompanyName { get; set; }

        public string? CompanyLogoUrl { get; set; }

        public bool IsConfidentialCompany { get; set; }

        // Job

        public string JobTitle { get; set; } = string.Empty;

        public string EmploymentType { get; set; } = string.Empty;

        public string? Department { get; set; }

        public string Location { get; set; } = string.Empty;

        // Requirements

        public byte? AgeMin { get; set; }

        public byte? AgeMax { get; set; }

        public string GenderPreferred { get; set; } = "Any";

        public bool DisabilityEligible { get; set; }

        public bool PassportRequired { get; set; }

        public List<string> LanguagesRequired { get; set; } = new();

        public List<string> CertificatesRequired { get; set; } = new();

        public List<string> ScreeningQuestions { get; set; } = new();

        // Candidate

        public bool HasUploadedCv { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string? CandidatePhotoUrl { get; set; }
    }
}
