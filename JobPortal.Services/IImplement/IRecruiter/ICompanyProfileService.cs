using JobPortal.Application.DTOs.Recruiter.CompanyProfile;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface ICompanyProfileService
    {
        Task<CompanyProfileResponseDto?> GetCompanyProfileAsync(
            Guid employerId);

        Task<bool> UpdateCompanyProfileAsync(
            Guid employerId,
            UpdateCompanyProfileDto request);
    }
}