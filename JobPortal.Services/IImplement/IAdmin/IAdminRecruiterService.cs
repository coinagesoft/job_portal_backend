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
    string? userAgent,
    string? jwtId = null
);

        Task<AdminRecruiterDetailDto?> GetRecruiterDetailAsync(Guid employerId);

        // Backs the "Transaction History" table on the recruiter detail page
        // (/admin/recruiters/details?id=). Same transaction+invoice rows as
        // GetRecruiterDetailAsync's Transactions field, exposed as its own
        // lightweight endpoint. Returns null if the recruiter doesn't exist.
        Task<List<RecruiterTransactionDto>?> GetRecruiterTransactionsAsync(Guid employerId);

        // Generates the GST-compliant invoice PDF for one of this recruiter's
        // transactions on demand (nothing is stored on S3 — same pattern as
        // RecruiterInvoiceService/AdminRevenueService). Returns null if the
        // transaction doesn't exist, doesn't belong to this employer, or has
        // no invoice on file.
        Task<(byte[] Bytes, string FileName)?> DownloadRecruiterInvoicePdfAsync(
            Guid employerId,
            Guid transactionId);

        Task<AdminRecruiterDocumentsResponseDto?> GetRecruiterDocumentsAsync(Guid employerId);

        Task<bool> UpdateRecruiterDocumentStatusAsync(Guid documentId, UpdateRecruiterDocumentStatusRequestDto request, AdminAuditContext audit);

        Task<AdminRecruiterDocumentChecklistResponseDto?> GetRecruiterDocumentChecklistAsync(Guid employerId);
        Task<List<AdminRecruiterDocumentVerificationListDto>> GetCompanyRequiredDocumentVerificationAsync(Guid employerId);
        Task<DocumentTypeAdminDto?> CreateOptionalDocumentTypeAsync(CreateOptionalDocumentTypeRequestDto request);

        Task<DocumentTypeAdminDto?> UpdateDocumentRequirementAsync(Guid documentTypeId, UpdateDocumentRequirementRequestDto request);

        Task<bool> DeleteDocumentTypeAsync(Guid documentTypeId);

        Task<List<AdminDocumentRequirementDto>> GetDocumentRequirementsAsync();

        Task<List<OptionalDocumentTypeDto>> GetOptionalDocumentNamesAsync();

        Task<EmployerDocumentRequestDto> RequestRecruiterDocumentAsync(Guid employerId, RequestRecruiterDocumentDto request, Guid adminId);

    }
}