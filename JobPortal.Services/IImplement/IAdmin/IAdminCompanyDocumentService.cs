using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAdminCompanyDocumentService
    {
        Task<List<PendingCompanyDocumentDto>> GetPendingAsync();

        Task<bool> VerifyAsync(Guid adminUserId, Guid documentId, VerifyCompanyDocumentRequestDto request);
    }
}
