using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface IAdminDocumentTypeService
    {
        Task<List<DocumentTypeAdminDto>> GetAllAsync();

        Task<DocumentTypeAdminDto?> UpdateAsync(Guid id, UpdateDocumentTypeRequestDto request);
        Task<DocumentTypeAdminDto?> CreateAsync(CreateDocumentTypeRequestDto request);
        Task<bool> DeleteAsync(Guid id);
    }
}
