using JobPortal.Application.DTOs.Recruiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{


    public interface IRecruiterRegistrationService
    {
        // Step 1 — GST check
        Task<GstCheckResponseDto> CheckGstAsync(
              GstCheckRequestDto request, string ipAddress);

        // Step 2 — Save company details
        Task<CompanyDetailsResponseDto> SaveCompanyDetailsAsync(
            CompanyDetailsRequestDto request, string sessionId);

        // Step 3A — Save contact + send OTP
        Task<ContactDetailsResponseDto> SaveContactAndSendOtpAsync(
            ContactDetailsRequestDto request, string sessionId);

        // Step 3B — Verify OTP
        Task<VerifyContactOtpResponseDto> VerifyContactOtpAsync(
            VerifyContactOtpRequestDto request, string sessionId);

        // Step 4 — Upload licences
        Task<LicencesResponseDto> UploadLicencesAsync(
            LicencesRequestDto request, string sessionId);

        // Step 5 — Final submit
        Task<ReviewSubmitResponseDto> SubmitRegistrationAsync(
            ReviewSubmitRequestDto request, string ipAddress);


        Task<ResumeSessionResponseDto> ResumeSessionAsync(
         string sessionId);
    }
}
