using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class CompanyProfileResponseDto
    {
        public Guid EmployerId { get; set; }

        public string LegalName { get; set; } = default!;
        public string? TradeName { get; set; }
        public string CompanyDisplayName { get; set; } = default!;
        public string? CompanyDescription { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }              // NEW

        public string? WebsiteUrl { get; set; }
        public string? LinkedInUrl { get; set; }                // NEW
        public string? InstagramUrl { get; set; }                // NEW
        public string? FacebookUrl { get; set; }                  // NEW

        public CompanySize? CompanySize { get; set; }
        public short? YearEstablished { get; set; }
        public int? TotalEmployees { get; set; }                 // NEW

        public BusinessType BusinessType { get; set; }
        public IndustryType IndustryType { get; set; }

        public bool GstRegistered { get; set; }
        public string? Gstn { get; set; }
        public string? Pan { get; set; }
        public string? Cin { get; set; }

        public string AddressLine1 { get; set; } = default!;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = default!;
        public string? State { get; set; }
        public string Pincode { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string? OfficeAddress { get; set; }                // NEW

        public string ContactPhone { get; set; } = default!;
        public string? ContactEmailPublic { get; set; }
        public string ContactPersonName { get; set; } = default!;
        public string Designation { get; set; } = default!;
        public string? OperatingHours { get; set; }

        public List<string>? CompanyHighlights { get; set; }      // NEW
        public string? TimeZone { get; set; }                     // NEW

        public AccountStatus AccountStatus { get; set; }
        public byte ProfileCompletionScore { get; set; }
        public DateTime? TrialExpiresAt { get; set; }
        public int ReviewCount { get; set; }                      // NEW

        public DateTime CreatedAt { get; set; }                    // NEW
        public DateTime UpdatedAt { get; set; }                    // NEW
    }
}
