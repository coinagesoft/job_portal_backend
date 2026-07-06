using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate
{
    public class CandidateCompanyDetailResponseDto
    {
        public Guid EmployerId { get; set; }

        // Basic
        public string CompanyName { get; set; } = string.Empty;
        public string? TradeName { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }

        // About
        public string? CompanyDescription { get; set; }

        // Category
        public string? IndustryType { get; set; }
        public string? BusinessType { get; set; }
        public string? CompanySize { get; set; }
        public int TotalEmployees { get; set; }
        public short? YearEstablished { get; set; }

        // Location
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string? State { get; set; }
        public string Country { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;

        public string? OfficeAddress { get; set; }

        public string FullLocation { get; set; } = string.Empty;

        // Verification
        public bool IsVerified { get; set; }
        public bool HasPoeLicence { get; set; }
        public bool HasRpslLicence { get; set; }

        public List<string> VerificationBadges { get; set; } = new();

        // Company Stats
        public int OpenPositionsCount { get; set; }
        public int TotalJobsPosted { get; set; }

        // Contact
        public string? WebsiteUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }

        // Extra
        public byte ProfileCompletionScore { get; set; }
    }
}
