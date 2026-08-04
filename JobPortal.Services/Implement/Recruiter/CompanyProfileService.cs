using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using JobPortal.Domain.Common;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Recruiter
{
    public class CompanyProfileService : ICompanyProfileService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;


        public CompanyProfileService(AppDbContext context, IFileStorageService fileStorageService)
        {
            _context = context;
            _fileStorageService = fileStorageService;

        }

        public async Task<CompanyProfileResponseDto?> GetCompanyProfileAsync(
        Guid employerId)
        {
            return await _context.EmployerProfiles
                .AsNoTracking()
                .Where(x => x.EmployerId == employerId)
                .Select(x => new CompanyProfileResponseDto
                {
                    EmployerId = x.EmployerId,

                    LegalName = x.LegalName,
                    TradeName = x.TradeName,
                    CompanyDisplayName = x.CompanyDisplayName,
                    CompanyDescription = x.CompanyDescription,
                    CompanyLogoUrl = x.CompanyLogoUrl,
                    CoverImageUrl = x.CoverImageUrl,

                    WebsiteUrl = x.WebsiteUrl,
                    LinkedInUrl = x.LinkedInUrl,
                    InstagramUrl = x.InstagramUrl,
                    FacebookUrl = x.FacebookUrl,

                    CompanySize = x.CompanySize,
                    YearEstablished = x.YearEstablished,
                    TotalEmployees = x.TotalEmployees,

                    BusinessType = x.BusinessType,
                    IndustryType = x.IndustryType,

                    GstRegistered = x.GstRegistered,
                    Gstn = x.Gstin,
                    Pan = x.Pan,
                    Cin = x.Cin,

                    AddressLine1 = x.AddressLine1,
                    AddressLine2 = x.AddressLine2,
                    City = x.City,
                    State = x.State,
                    Pincode = x.Pincode,
                    Country = x.Country,
                    OfficeAddress = x.OfficeAddress,

                    CompanyPhoneNo = x.ContactPhone,
                    CompanyEmail = x.ContactEmailPublic,
                    ContactPersonName = x.ContactPersonName,
                    Designation = x.Designation,
                    OperatingHours = x.OperatingHours,

                    CompanyHighlights = x.CompanyHighlights,
                    TimeZone = x.TimeZone,

                    AccountStatus = x.AccountStatus,
                    ProfileCompletionScore = x.ProfileCompletionScore,
                    TrialExpiresAt = x.TrialExpiresAt,
                    ReviewCount = x.ReviewCount,

                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }


        public async Task<bool> UpdateCompanyProfileAsync(
        Guid employerId,
        UpdateCompanyProfileDto request)
        {
            var profile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (profile == null)
                return false;

            if (!string.IsNullOrWhiteSpace(request.LegalName))
                profile.LegalName = request.LegalName;

            if (!string.IsNullOrWhiteSpace(request.TradeName))
                profile.TradeName = request.TradeName;

            if (!string.IsNullOrWhiteSpace(request.CompanyDisplayName))
                profile.CompanyDisplayName = request.CompanyDisplayName;

            if (!string.IsNullOrWhiteSpace(request.CompanyDescription))
                profile.CompanyDescription = request.CompanyDescription;


            if (request.CompanyLogo != null && request.CompanyLogo.Length > 0)
            {
                var logoResult = await _fileStorageService.UploadImageAsync(
                    request.CompanyLogo, "company-logos");

                profile.CompanyLogoUrl = logoResult.Url;
                profile.CompanyLogoPublicId = logoResult.PublicId;
            }

            if (request.CoverImage != null && request.CoverImage.Length > 0)
            {
                var coverResult = await _fileStorageService.UploadImageAsync(
                    request.CoverImage, "company-covers");

                profile.CoverImageUrl = coverResult.Url;
            }
            if (request.CompanySize.HasValue)
                profile.CompanySize = request.CompanySize.Value;

            if (request.YearEstablished.HasValue)
                profile.YearEstablished = request.YearEstablished;

            if (!string.IsNullOrWhiteSpace(request.WebsiteUrl))
                profile.WebsiteUrl = request.WebsiteUrl;

            if (!string.IsNullOrWhiteSpace(request.LinkedInUrl))
                profile.LinkedInUrl = request.LinkedInUrl;

            if (!string.IsNullOrWhiteSpace(request.InstagramUrl))
                profile.InstagramUrl = request.InstagramUrl;

            if (!string.IsNullOrWhiteSpace(request.FacebookUrl))
                profile.FacebookUrl = request.FacebookUrl;

            if (request.TotalEmployees.HasValue)
                profile.TotalEmployees = request.TotalEmployees.Value;

            if (!string.IsNullOrWhiteSpace(request.BusinessType))
                profile.BusinessType = request.BusinessType;

            if (!string.IsNullOrWhiteSpace(request.IndustryType))
            {
                profile.IndustryType = request.IndustryType;
            }

            if (!string.IsNullOrWhiteSpace(request.AddressLine1))
                profile.AddressLine1 = request.AddressLine1;

            if (!string.IsNullOrWhiteSpace(request.AddressLine2))
                profile.AddressLine2 = request.AddressLine2;

            if (!string.IsNullOrWhiteSpace(request.City))
                profile.City = request.City;

            if (!string.IsNullOrWhiteSpace(request.State))
                profile.State = request.State;

            if (!string.IsNullOrWhiteSpace(request.Pincode))
                profile.Pincode = request.Pincode;

            if (!string.IsNullOrWhiteSpace(request.Country))
                profile.Country = request.Country;

            if (!string.IsNullOrWhiteSpace(request.OfficeAddress))
                profile.OfficeAddress = request.OfficeAddress;

            if (!string.IsNullOrWhiteSpace(request.CompanyPhoneNo))
                profile.ContactPhone = request.CompanyPhoneNo;

            if (!string.IsNullOrWhiteSpace(request.CompanyEmail))
                profile.ContactEmailPublic = request.CompanyEmail;

            if (!string.IsNullOrWhiteSpace(request.ContactPersonName))
                profile.ContactPersonName = request.ContactPersonName;

            if (!string.IsNullOrWhiteSpace(request.Designation))
                profile.Designation = request.Designation;

            if (!string.IsNullOrWhiteSpace(request.OperatingHours))
                profile.OperatingHours = request.OperatingHours;

            if (request.CompanyHighlights != null)
                profile.CompanyHighlights = request.CompanyHighlights;

            if (!string.IsNullOrWhiteSpace(request.TimeZone))
                profile.TimeZone = request.TimeZone;

            // Check whether all active standard documents (excluding "Other") are uploaded
            bool hasAllRequiredDocuments =
                await _context.VerificationDocumentMasters
                    .Where(x => x.IsActive)
                    .AllAsync(master =>
                        _context.EmployerVerificationDocuments.Any(doc =>
                            doc.EmployerId == employerId &&
                            !doc.IsDeleted &&
                            doc.DocumentTypeId == master.DocumentTypeId));

            profile.ProfileCompletionScore =
                ProfileCompletionHelper.CalculateProfileCompletionScore(
                    profile,
                    hasAllRequiredDocuments);

            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

   
    }
}
