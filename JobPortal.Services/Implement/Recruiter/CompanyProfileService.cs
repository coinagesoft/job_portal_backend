using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Recruiter
{
    public class CompanyProfileService : ICompanyProfileService
    {
        private readonly AppDbContext _context;

        public CompanyProfileService(AppDbContext context)
        {
            _context = context;
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

                    WebsiteUrl = x.WebsiteUrl,

                    CompanySize = x.CompanySize,
                    YearEstablished = x.YearEstablished,

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

                    ContactPhone = x.ContactPhone,
                    ContactEmailPublic = x.ContactEmailPublic,
                    ContactPersonName = x.ContactPersonName,
                    Designation = x.Designation,
                    OperatingHours = x.OperatingHours,

                    AccountStatus = x.AccountStatus,
                    ProfileCompletionScore = x.ProfileCompletionScore,
                    TrialExpiresAt = x.TrialExpiresAt
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

            if (!string.IsNullOrWhiteSpace(request.TradeName))
                profile.TradeName = request.TradeName;

            if (!string.IsNullOrWhiteSpace(request.CompanyDisplayName))
                profile.CompanyDisplayName = request.CompanyDisplayName;

            if (!string.IsNullOrWhiteSpace(request.CompanyDescription))
                profile.CompanyDescription = request.CompanyDescription;

            if (!string.IsNullOrWhiteSpace(request.WebsiteUrl))
                profile.WebsiteUrl = request.WebsiteUrl;

            if (request.CompanySize.HasValue)
                profile.CompanySize = request.CompanySize;

            if (request.YearEstablished.HasValue)
                profile.YearEstablished = request.YearEstablished;

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

            if (!string.IsNullOrWhiteSpace(request.ContactPhone))
                profile.ContactPhone = request.ContactPhone;

            if (!string.IsNullOrWhiteSpace(request.ContactEmailPublic))
                profile.ContactEmailPublic = request.ContactEmailPublic;

            if (!string.IsNullOrWhiteSpace(request.ContactPersonName))
                profile.ContactPersonName = request.ContactPersonName;

            if (!string.IsNullOrWhiteSpace(request.Designation))
                profile.Designation = request.Designation;

            if (!string.IsNullOrWhiteSpace(request.OperatingHours))
                profile.OperatingHours = request.OperatingHours;

            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}
