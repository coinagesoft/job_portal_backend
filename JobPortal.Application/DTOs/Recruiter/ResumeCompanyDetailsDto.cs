using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class ResumeCompanyDetailsDto
    {
        public string? LegalName { get; set; }
        public string? TradeName { get; set; }
        public string? CompanyDisplayName { get; set; }
        public string? BusinessType { get; set; }
        public string? CompanySize { get; set; }
        public string? Cin { get; set; }
        public string? Gstn { get; set; }
        public string? Pan { get; set; }
        public DateOnly? GstnRegistrationDate { get; set; }
        public string? IndustryType { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Pincode { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? CompanyLogoUrl { get; set; }
    }
}
