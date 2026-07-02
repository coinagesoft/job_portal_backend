using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate
{
    public class PublicCompanyCardDto
    {
        public Guid EmployerId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string? CompanyLogoUrl { get; set; }

        public string? CoverImageUrl { get; set; }

        public string Industry { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? State { get; set; }

        public bool IsVerified { get; set; }

        public int OpenJobsCount { get; set; }

        public int ReviewCount { get; set; }
    }
    public class PublicCompanyListResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public List<PublicCompanyCardDto> Companies { get; set; } = new();
    }

    public class PublicCompanyDetailResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        // Company

        public Guid EmployerId { get; set; }

        public string CompanyName { get; set; } = string.Empty;

        public string? CompanyLogoUrl { get; set; }

        public string? CoverImageUrl { get; set; }

        public string? CompanyDescription { get; set; }

        public string Industry { get; set; } = string.Empty;

        public string? BusinessType { get; set; }

        public string? CompanySize { get; set; }

        public short? YearEstablished { get; set; }

        // Contact

        public string? WebsiteUrl { get; set; }

        public string? LinkedInUrl { get; set; }

        public string? InstagramUrl { get; set; }

        public string? FacebookUrl { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        // Address

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public string? Pincode { get; set; }

        /// <summary>Full "AddressLine1, City, State, Country" string for display.</summary>
        public string? FullLocation { get; set; }

        /// <summary>Google Maps embeddable URL built from the company's real address — no API key required.</summary>
        public string? MapEmbedUrl { get; set; }

        // Verification

        public bool GstRegistered { get; set; }

        public bool HasPoeLicence { get; set; }

        public bool HasRpslLicence { get; set; }

        public bool IsVerified { get; set; }

        // Company Highlights

        public List<string> CompanyHighlights { get; set; } = new();

        // Statistics

        public int TotalEmployees { get; set; }

        public int OpenJobsCount { get; set; }

        public int ReviewCount { get; set; }

        // Open Jobs

        public List<CandidateJobListItemDto> Jobs { get; set; } = new();
    }
}