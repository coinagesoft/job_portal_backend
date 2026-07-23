using JobPortal.Application.DTOs.Recruiter.CompanyDocuments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface ICompanyDocumentService
    {
        Task<CompanyDocumentResponseDto?> UploadAsync(
            Guid employerId, UploadCompanyDocumentRequestDto request);

        Task<List<CompanyDocumentResponseDto>> GetMyDocumentsAsync(Guid employerId);

        Task<CompanyDocumentResponseDto?> GetByIdAsync(Guid employerId, Guid documentId);

        Task<CompanyDocumentResponseDto?> UpdateAsync(
            Guid employerId, Guid documentId, UpdateCompanyDocumentRequestDto request);

        Task<bool> DeleteAsync(Guid employerId, Guid documentId);
    }
}
