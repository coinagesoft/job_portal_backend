using JobPortal.Application.DTOs.Admin.LegalPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IAdmin
{
    public interface ILegalDocumentService
    {
        /// <summary>Both documents (privacy + terms) for the Legal Pages admin screen.</summary>
        Task<List<LegalDocumentAdminDto>> GetAllAsync();

        Task<LegalDocumentAdminDto?> GetByTypeAsync(string type);

        /// <summary>Saves editor changes without publishing them.</summary>
        Task<LegalDocumentAdminDto?> SaveDraftAsync(string type, SaveLegalDocumentRequestDto request, Guid? adminId);

        /// <summary>Publishes the given content (replaces the live version shown publicly).</summary>
        Task<LegalDocumentAdminDto?> PublishAsync(string type, SaveLegalDocumentRequestDto request, Guid? adminId);

        /// <summary>Reverts any unsaved/unpublished draft edits back to the currently published version.</summary>
        Task<LegalDocumentAdminDto?> DiscardDraftAsync(string type);
    }
}