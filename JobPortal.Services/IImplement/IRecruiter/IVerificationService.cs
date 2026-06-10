using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IVerificationService
    {
        Task<VerificationDashboardResponseDto?> GetVerificationDashboardAsync(
            Guid employerId);

        Task<bool> UploadDocumentAsync(
            Guid employerId,
            UploadVerificationDocumentRequestDto request);

        Task<DocumentViewResponseDto?> GetDocumentAsync(
            Guid employerId,
            string documentType);
    }
}
