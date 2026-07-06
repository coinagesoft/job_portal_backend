using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class UpdateCompanyProfileDto
    {
        public string? LegalName { get; set; }

        public string? TradeName { get; set; }

        public string? CompanyDisplayName { get; set; }

        public string? CompanyDescription { get; set; }

        public IFormFile? CompanyLogo { get; set; }

        public IFormFile? CoverImage { get; set; }

        public CompanySize? CompanySize { get; set; }

        public short? YearEstablished { get; set; }

        public string? WebsiteUrl { get; set; }

        public string? LinkedInUrl { get; set; }

        public string? InstagramUrl { get; set; }

        public string? FacebookUrl { get; set; }

        public int? TotalEmployees { get; set; }

        public BusinessType? BusinessType { get; set; }

        public IndustryType? IndustryType { get; set; }

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Pincode { get; set; }

        public string? Country { get; set; }

        public string? OfficeAddress { get; set; }

        public string? CompanyPhoneNo { get; set; }

        public string? CompanyEmail { get; set; }

        public string? ContactPersonName { get; set; }

        public string? Designation { get; set; }

        public string? OperatingHours { get; set; }

        public List<string>? CompanyHighlights { get; set; }

        public string? TimeZone { get; set; }
    }
}