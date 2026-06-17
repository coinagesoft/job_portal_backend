using JobPortal.Domain.Enums.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class UpdateCompanyProfileDto
    {
        public string? TradeName { get; set; }

        public string? CompanyDisplayName { get; set; }

        public string? CompanyDescription { get; set; }

        public string? WebsiteUrl { get; set; }

        public CompanySize? CompanySize { get; set; }

        public short? YearEstablished { get; set; }

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Pincode { get; set; }

        public string? Country { get; set; }

        public string? ContactPhone { get; set; }

        public string? ContactEmailPublic { get; set; }

        public string? ContactPersonName { get; set; }

        public string? Designation { get; set; }

        public string? OperatingHours { get; set; }
    }
}
