using JobPortal.Application.DTOs;
using JobPortal.Application.DTOs.Admin;
using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAdminRecruiterService
    {
        Task<List<AdminRecruiterListItemDto>> GetRecruitersAsync();

        Task<bool> UpdateRecruiterStatusAsync(
    Guid employerId,
    string status,
    string? reason,
    Guid performedByAdminId,
    string ipAddress,
    string? userAgent
);

    Task<AdminRecruiterDetailDto?> GetRecruiterDetailAsync(Guid employerId);

    Task<AdminRecruiterDocumentsResponseDto?> GetRecruiterDocumentsAsync(Guid employerId);

     Task<bool> UpdateRecruiterDocumentStatusAsync(Guid documentId,UpdateRecruiterDocumentStatusRequestDto request,AdminAuditContext audit);

     Task<AdminRecruiterDocumentChecklistResponseDto?>GetRecruiterDocumentChecklistAsync(Guid employerId);

     Task<DocumentTypeAdminDto?> CreateOptionalDocumentTypeAsync(CreateOptionalDocumentTypeRequestDto request);

        Task<DocumentTypeAdminDto?> UpdateDocumentRequirementAsync(Guid documentTypeId,UpdateDocumentRequirementRequestDto request);

        Task<List<AdminDocumentRequirementDto>>
     GetDocumentRequirementsAsync();

    }
}
