using JobPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Common
{
    public static class ProfileCompletionHelper
    {
        public static byte CalculateProfileCompletionScore(
     EmployerProfile profile,
     bool hasAllRequiredDocuments)
        {
            int totalFields = 33;
            int completed = 0;

            bool HasValue(string? value) => !string.IsNullOrWhiteSpace(value);

            // Company Details
            if (HasValue(profile.LegalName)) completed++;
            if (HasValue(profile.TradeName)) completed++;
            if (HasValue(profile.CompanyDisplayName)) completed++;
            if (HasValue(profile.CompanyDescription)) completed++;

            // Images
            if (HasValue(profile.CompanyLogoUrl)) completed++;
            if (HasValue(profile.CoverImageUrl)) completed++;

            // Social Links
            if (HasValue(profile.WebsiteUrl)) completed++;
            if (HasValue(profile.LinkedInUrl)) completed++;
            if (HasValue(profile.InstagramUrl)) completed++;
            if (HasValue(profile.FacebookUrl)) completed++;

            // Company Info
            if (profile.CompanySize.HasValue) completed++;
            if (profile.YearEstablished.HasValue) completed++;
            if (profile.TotalEmployees > 0) completed++;
            if (HasValue(profile.BusinessType)) completed++;
            if (HasValue(profile.IndustryType)) completed++;

            // Legal Details

            // GST Registered field itself is always answered (Yes/No)
            completed++;

            // GSTIN is required only when GST Registered = true
            if (profile.GstRegistered)
            {
                if (HasValue(profile.Gstin))
                    completed++;
            }
            else
            {
                // GSTIN is not applicable, so remove it from total fields
                totalFields--;
            }

            if (HasValue(profile.Pan)) completed++;
            if (HasValue(profile.Cin)) completed++;

            // Address
            if (HasValue(profile.AddressLine1)) completed++;
            if (HasValue(profile.City)) completed++;
            if (HasValue(profile.State)) completed++;
            if (HasValue(profile.Pincode)) completed++;
            if (HasValue(profile.Country)) completed++;
            if (HasValue(profile.OfficeAddress)) completed++;

            // Contact
            if (HasValue(profile.ContactPhone)) completed++;
            if (HasValue(profile.ContactEmailPublic)) completed++;
            if (HasValue(profile.ContactPersonName)) completed++;
            if (HasValue(profile.Designation)) completed++;

            // Others
            if (HasValue(profile.OperatingHours)) completed++;
            if (profile.CompanyHighlights != null && profile.CompanyHighlights.Any()) completed++;
            if (HasValue(profile.TimeZone)) completed++;

            // Required Documents (excluding "Other Document")
            if (hasAllRequiredDocuments)
                completed++;

            return (byte)Math.Round((double)completed * 100 / totalFields);
        }
    }
}
