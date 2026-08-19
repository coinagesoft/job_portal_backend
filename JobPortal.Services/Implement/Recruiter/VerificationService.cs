using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using JobPortal.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobPortal.Domain.Enums.RecruiterEnums;

namespace JobPortal.Services.Implement.Recruiter
{
    public class VerificationService : IVerificationService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<VerificationService> _logger;
        private readonly IGeminiCompanyDocumentParserService _geminiCompanyDocumentParserService;

public VerificationService(
    AppDbContext context,
    IFileStorageService fileStorageService,
    IGeminiCompanyDocumentParserService geminiCompanyDocumentParserService,
    ILogger<VerificationService> logger)
{
    _context = context;
    _fileStorageService = fileStorageService;
    _geminiCompanyDocumentParserService = geminiCompanyDocumentParserService;
    _logger = logger;
}



        public async Task<VerificationDashboardResponseDto?> GetVerificationDashboardAsync(
        Guid employerId)
        {
            // ===========================================================
            // CHECK EMPLOYER
            // ===========================================================

            var employerExists = await _context.EmployerProfiles
                .AsNoTracking()
                .AnyAsync(x => x.EmployerId == employerId);

            if (!employerExists)
                return null;


            var response = new VerificationDashboardResponseDto();


            // ===========================================================
            // LOAD ALL ACTIVE MASTER DOCUMENTS
            // ===========================================================
            //
            // Includes:
            // Mandatory
            // Optional
            //
            // We decide what to display using IsMandatory.
            //
            // ===========================================================

            var masters = await _context.VerificationDocumentMasters
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();


            // ===========================================================
            // LOAD EMPLOYER UPLOADED DOCUMENTS
            // ===========================================================

            var uploadedDocuments = await _context.EmployerVerificationDocuments
                .AsNoTracking()
                .Where(x =>
                    x.EmployerId == employerId &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.UploadedAt)
                .ToListAsync();


            // ===========================================================
            // LOAD ADMIN REQUESTED DOCUMENTS
            // ===========================================================
            //
            // Important:
            // A request can exist even when recruiter has not uploaded.
            //
            // ===========================================================

            var documentRequests = await _context.EmployerDocumentRequests
                .AsNoTracking()
                .Where(x =>
                    x.EmployerId == employerId &&
                    x.Status != "Cancelled")
                .OrderByDescending(x => x.RequestedAt)
                .ToListAsync();


            // ===========================================================
            // 1. MASTER DOCUMENTS
            // ===========================================================

            foreach (var master in masters)
            {
                var uploaded = uploadedDocuments
                    .Where(x =>
                        x.DocumentTypeId.HasValue &&
                        x.DocumentTypeId.Value ==
                            master.DocumentTypeId)
                    .OrderByDescending(x => x.UploadedAt)
                    .FirstOrDefault();


                // =======================================================
                // MANDATORY
                // =======================================================

                if (master.IsMandatory)
                {
                    var status = uploaded == null
                        ? "Not Uploaded"
                        : uploaded.Status.ToString();


                    // ---------------------------------------------------
                    // BADGE ONLY FOR MANDATORY
                    // ---------------------------------------------------

                    response.Badges.Add(
                        new VerificationBadgeDto
                        {
                            BadgeName =
                                master.DocumentName,

                            Status =
                                status,

                            Description =
                                master.Description
                                ?? $"{master.DocumentName} verification."
                        });


                    // ---------------------------------------------------
                    // DOCUMENT
                    // ---------------------------------------------------

                    response.Documents.Add(
                        new VerificationDocumentDto
                        {
                            // IDs
                            DocumentId =
                                uploaded?.DocumentId,

                            DocumentTypeId =
                                uploaded?.DocumentTypeId
                                ?? master.DocumentTypeId,

                            RequestId =
                                uploaded?.RequestId,

                            // DOCUMENT INFORMATION
                            DocumentName =
                                master.DocumentName,

                            DocumentType =
                                master.Code,

                            Category =
                                "Mandatory",

                            DocumentTypeCategory =
                                master.Category,

                            // VERIFICATION
                            Status =
                                status,

                            // OTHER EXISTING DATA
                            Message =
                                "No Message",

                            FileUrl =
                                uploaded?.FileUrl,

                            UploadedAt =
                                uploaded?.UploadedAt
                        });
                }


                // =======================================================
                // OPTIONAL
                // =======================================================
                //
                // Optional document is displayed ONLY if uploaded.
                //
                // No badge.
                //
                // =======================================================

                else
                {
                    if (uploaded == null)
                        continue;


                    response.Documents.Add(
                        new VerificationDocumentDto
                        {
                            // IDs
                            DocumentId =
                                uploaded.DocumentId,

                            DocumentTypeId =
                                uploaded.DocumentTypeId
                                ?? master.DocumentTypeId,

                            RequestId =
                                uploaded.RequestId,

                            // DOCUMENT INFORMATION
                            DocumentName =
                                master.DocumentName,

                            DocumentType =
                                master.Code,

                            Category =
                                "Optional",

                            DocumentTypeCategory =
                                master.Category,

                            // VERIFICATION
                            Status =
                                uploaded.Status.ToString(),

                            // OTHER EXISTING DATA
                            Message =
                                "No Message",

                            FileUrl =
                                uploaded.FileUrl,

                            UploadedAt =
                                uploaded.UploadedAt
                        });
                }
            }


            // ===========================================================
            // 2. NORMAL ADDITIONAL DOCUMENTS
            // ===========================================================
            //
            // These are recruiter-uploaded documents.
            //
            // DocumentTypeId = NULL
            // RequestId      = NULL
            // Category       = Additional
            //
            // They are shown only after upload.
            //
            // No badge.
            //
            // ===========================================================

            var additionalDocuments = uploadedDocuments
                .Where(x =>
                    !x.DocumentTypeId.HasValue &&
                    !x.RequestId.HasValue &&
                    string.Equals(
                        x.Category,
                        "Additional",
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.UploadedAt)
                .ToList();


            foreach (var doc in additionalDocuments)
            {
                var documentName =
                    doc.CustomDocumentName
                    ?? doc.DetectedDocumentType
                    ?? doc.FileName;


                response.Documents.Add(
                    new VerificationDocumentDto
                    {
                        // IDs
                        DocumentId =
                            doc.DocumentId,

                        DocumentTypeId =
                            doc.DocumentTypeId,

                        RequestId =
                            doc.RequestId,

                        // DOCUMENT INFORMATION
                        DocumentName =
                            documentName,

                        DocumentType =
                            documentName,

                        Category =
                            "Additional",

                        DocumentTypeCategory =
                            doc.Category,

                        // VERIFICATION
                        Status =
                            doc.Status.ToString(),

                        // OTHER EXISTING DATA
                        Message =
                            "No Message",

                        FileUrl =
                            doc.FileUrl,

                        UploadedAt =
                            doc.UploadedAt
                    });
            }


            // ===========================================================
            // 3. REQUESTED DOCUMENTS
            // ===========================================================
            //
            // These come from EmployerDocumentRequests.
            //
            // They MUST be displayed even before recruiter uploads.
            //
            // No badge.
            //
            // ===========================================================

            foreach (var documentRequest in documentRequests)
            {
                // -------------------------------------------------------
                // Find uploaded document for this exact request
                // -------------------------------------------------------

                var uploaded = uploadedDocuments
                    .Where(x =>
                        x.RequestId.HasValue &&
                        x.RequestId.Value ==
                            documentRequest.RequestId)
                    .OrderByDescending(x => x.UploadedAt)
                    .FirstOrDefault();


                // -------------------------------------------------------
                // Determine document name
                // -------------------------------------------------------

                string documentName;

                VerificationDocumentMaster? requestedMaster = null;


                // =======================================================
                // REQUESTED EXISTING OPTIONAL MASTER
                // =======================================================

                if (documentRequest.DocumentTypeId.HasValue)
                {
                    requestedMaster =
                        masters.FirstOrDefault(x =>
                            x.DocumentTypeId ==
                            documentRequest.DocumentTypeId.Value);


                    documentName =
                        requestedMaster?.DocumentName
                        ?? documentRequest.CustomDocumentName
                        ?? "Requested Document";
                }


                // =======================================================
                // REQUESTED CUSTOM DOCUMENT
                // =======================================================

                else
                {
                    documentName =
                        documentRequest.CustomDocumentName
                        ?? "Requested Additional Document";
                }


                // -------------------------------------------------------
                // STATUS
                // -------------------------------------------------------
                //
                // Request exists but no uploaded document:
                //
                //     Not Uploaded
                //
                // Uploaded:
                //
                //     Pending / Approved / Rejected /
                //     Expired / Resubmission
                //
                // -------------------------------------------------------

                var status = uploaded == null
                    ? "Not Uploaded"
                    : uploaded.Status.ToString();


                // -------------------------------------------------------
                // ADD TO DOCUMENTS
                // -------------------------------------------------------

                response.Documents.Add(
                    new VerificationDocumentDto
                    {
                        // ==================================================
                        // IDS
                        // ==================================================

                        // Actual uploaded document ID.
                        // Null when recruiter has not uploaded yet.
                        DocumentId =
                            uploaded?.DocumentId,

                        // Use uploaded DocumentTypeId when available.
                        // For an unuploaded requested existing document,
                        // use the request's DocumentTypeId.
                        DocumentTypeId =
                            uploaded?.DocumentTypeId
                            ?? documentRequest.DocumentTypeId,

                        // IMPORTANT:
                        // Request ID comes from the actual request.
                        RequestId =
                            documentRequest.RequestId,


                        // ==================================================
                        // DOCUMENT INFORMATION
                        // ==================================================

                        DocumentName =
                            documentName,

                        DocumentType =
                            requestedMaster?.Code
                            ?? "Other",

                        Category =
                            "RequestedAdditional",

                        DocumentTypeCategory =
                            requestedMaster?.Category
                            ?? "Other",


                        // ==================================================
                        // VERIFICATION
                        // ==================================================

                        Status =
                            status,


                        // ==================================================
                        // OTHER EXISTING DATA
                        // ==================================================

                        Message =
                            documentRequest.Message,

                        FileUrl =
                            uploaded?.FileUrl,

                        UploadedAt =
                            uploaded?.UploadedAt
                    });
            }


            // ===========================================================
            // RETURN
            // ===========================================================

            return response;
        }


        public async Task<bool> UploadDocumentAsync(
        Guid employerId,
        UploadVerificationDocumentRequestDto request)
        {
            try
            {
                // ===========================================================
                // CHECK EMPLOYER
                // ===========================================================

                var employer = await _context.EmployerProfiles
                    .FirstOrDefaultAsync(x =>
                        x.EmployerId == employerId);

                if (employer == null)
                    return false;


                // ===========================================================
                // VALIDATE REQUEST
                // ===========================================================

                if (request == null)
                    throw new Exception("Upload request is required.");

                if (request.File == null ||
                    request.File.Length == 0)
                {
                    throw new Exception("File is required.");
                }


                // ===========================================================
                // VALIDATE MASTER DOCUMENT
                // ===========================================================

                VerificationDocumentMaster? master = null;

                if (request.DocumentTypeId.HasValue)
                {
                    master =
                        await _context.VerificationDocumentMasters
                            .FirstOrDefaultAsync(x =>
                                x.DocumentTypeId ==
                                    request.DocumentTypeId.Value &&
                                x.IsActive);

                    if (master == null)
                    {
                        throw new Exception(
                            "Invalid document type.");
                    }


                    // -------------------------------------------------------
                    // A master document must NOT also be a custom request.
                    // -------------------------------------------------------

                    if (request.RequestId.HasValue)
                    {
                        var documentRequest =
                            await _context.EmployerDocumentRequests
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x =>
                                    x.RequestId ==
                                        request.RequestId.Value &&
                                    x.EmployerId ==
                                        employerId &&
                                    x.Status != "Cancelled");

                        if (documentRequest == null)
                        {
                            throw new Exception(
                                "Document request not found.");
                        }

                        if (!documentRequest.DocumentTypeId.HasValue ||
                            documentRequest.DocumentTypeId.Value !=
                                request.DocumentTypeId.Value)
                        {
                            throw new Exception(
                                "Selected document type does not match the requested document.");
                        }
                    }
                }


                // ===========================================================
                // REQUESTED ADDITIONAL DOCUMENT
                // ===========================================================
                //
                // DocumentTypeId = NULL
                // RequestId      = EXISTS
                //
                // This means admin requested this document.
                //
                // ===========================================================

                EmployerDocumentRequest? documentRequestEntity = null;

                if (!request.DocumentTypeId.HasValue &&
                    request.RequestId.HasValue)
                {
                    documentRequestEntity =
                        await _context.EmployerDocumentRequests
                            .FirstOrDefaultAsync(x =>
                                x.RequestId ==
                                    request.RequestId.Value &&
                                x.EmployerId ==
                                    employerId &&
                                x.Status != "Cancelled");

                    if (documentRequestEntity == null)
                    {
                        throw new Exception(
                            "Requested document was not found.");
                    }


                    // -------------------------------------------------------
                    // If the request points to an existing master document,
                    // the uploaded DocumentTypeId must match it.
                    // -------------------------------------------------------

                    if (documentRequestEntity.DocumentTypeId.HasValue)
                    {
                        throw new Exception(
                            "This request belongs to an existing document type. Please upload it using the requested document type.");
                    }


                    // -------------------------------------------------------
                    // For a custom requested document, CustomDocumentName
                    // must exist.
                    // -------------------------------------------------------

                    if (string.IsNullOrWhiteSpace(
                        documentRequestEntity.CustomDocumentName))
                    {
                        throw new Exception(
                            "Requested document name is missing.");
                    }
                }


                // ===========================================================
                // NORMAL ADDITIONAL / CUSTOM DOCUMENT
                // ===========================================================
                //
                // DocumentTypeId = NULL
                // RequestId      = NULL
                //
                // This is a normal Additional document.
                //
                // Example:
                //
                // Pollution Certificate
                // Safety Certificate
                // Other custom document
                //
                // ===========================================================

                if (!request.DocumentTypeId.HasValue &&
                    !request.RequestId.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(
                        request.CustomDocumentName))
                    {
                        throw new Exception(
                            "Additional document name is required.");
                    }
                }


                // ===========================================================
                // UPLOAD FILE
                // ===========================================================

                var uploadResult =
                    await _fileStorageService.UploadDocumentAsync(
                        request.File,
                        "verification-documents");

                if (string.IsNullOrWhiteSpace(
                    uploadResult.Url))
                {
                    throw new Exception(
                        "Failed to upload document.");
                }


                // ===========================================================
                // GEMINI PARSING
                // ===========================================================
                //
                // Gemini only detects information from the file.
                //
                // It does NOT determine:
                //
                // DocumentTypeId
                // RequestId
                // CustomDocumentName
                //
                // ===========================================================

                GeminiCompanyDocumentParseResponse? parsed = null;

                try
                {
                    parsed =
                        await _geminiCompanyDocumentParserService
                            .ParseDocumentAsync(request.File);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Gemini parsing failed for {FileName}",
                        request.File.FileName);
                }


                // ===========================================================
                // FIND EXISTING DOCUMENT
                // ===========================================================

                EmployerVerificationDocument? existing = null;


                // ===========================================================
                // CASE 1: MASTER DOCUMENT
                // ===========================================================

                if (request.DocumentTypeId.HasValue)
                {
                    existing =
                        await _context
                            .EmployerVerificationDocuments
                            .FirstOrDefaultAsync(x =>
                                x.EmployerId ==
                                    employerId &&

                                x.DocumentTypeId ==
                                    request.DocumentTypeId.Value &&

                                !x.IsDeleted);
                }


                // ===========================================================
                // CASE 2: REQUESTED ADDITIONAL DOCUMENT
                // ===========================================================

                else if (request.RequestId.HasValue)
                {
                    existing =
                        await _context
                            .EmployerVerificationDocuments
                            .FirstOrDefaultAsync(x =>
                                x.EmployerId ==
                                    employerId &&

                                x.RequestId ==
                                    request.RequestId.Value &&

                                !x.IsDeleted);
                }


                // ===========================================================
                // CASE 3: NORMAL ADDITIONAL DOCUMENT
                // ===========================================================
                //
                // IMPORTANT:
                //
                // Do NOT search using DetectedDocumentType.
                //
                // The selected CustomDocumentName identifies the document.
                //
                // ===========================================================

                else
                {
                    var customName =
                        request.CustomDocumentName!.Trim();

                    existing =
                        await _context
                            .EmployerVerificationDocuments
                            .FirstOrDefaultAsync(x =>
                                x.EmployerId ==
                                    employerId &&

                                x.DocumentTypeId == null &&

                                x.RequestId == null &&

                                x.Category == "Additional" &&

                                x.CustomDocumentName != null &&

                                x.CustomDocumentName.ToLower() ==
                                    customName.ToLower() &&

                                !x.IsDeleted);
                }


                // ===========================================================
                // UPDATE EXISTING DOCUMENT
                // ===========================================================

                if (existing != null)
                {
                    // -------------------------------------------------------
                    // Delete old file
                    // -------------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(
                        existing.PublicId))
                    {
                        await _fileStorageService
                            .DeleteAsync(existing.PublicId);
                    }


                    // -------------------------------------------------------
                    // File information
                    // -------------------------------------------------------

                    existing.FileName =
                        request.File.FileName;

                    existing.FileUrl =
                        uploadResult.Url;

                    existing.PublicId =
                        uploadResult.PublicId;

                    existing.UploadedAt =
                        DateTime.UtcNow;


                    // -------------------------------------------------------
                    // Preserve / update relationship
                    // -------------------------------------------------------

                    if (request.RequestId.HasValue)
                    {
                        existing.RequestId =
                            request.RequestId.Value;
                    }


                    if (request.DocumentTypeId.HasValue)
                    {
                        existing.DocumentTypeId =
                            request.DocumentTypeId.Value;

                        existing.Category =
                            master?.Category;
                    }
                    else
                    {
                        existing.DocumentTypeId = null;

                        existing.Category =
                            "Additional";

                        if (!request.RequestId.HasValue)
                        {
                            existing.CustomDocumentName =
                                request.CustomDocumentName?.Trim();
                        }
                    }


                    // -------------------------------------------------------
                    // Gemini parsed information
                    // -------------------------------------------------------

                    existing.DocumentNumber =
                        parsed?.DocumentNumber;

                    existing.IssuingAuthority =
                        parsed?.IssuingAuthority;

                    existing.IssueDate =
                        parsed?.IssueDate;

                    existing.ExpiryDate =
                        parsed?.ExpiryDate;

                    existing.DetectedDocumentType =
                        parsed?.DocumentType;

                    existing.ParsedDataJson =
                        parsed?.ParsedData?.GetRawText();

                    existing.AiConfidenceScore =
                        parsed?.AiConfidenceScore;


                    // -------------------------------------------------------
                    // RESET VERIFICATION
                    // -------------------------------------------------------

                    existing.Status =
                        VerificationDocumentStatus.Pending;

                    existing.VerifiedAt = null;

                    existing.VerifiedBy = null;

                    existing.Remarks = null;

                    existing.IsDeleted = false;
                }


                // ===========================================================
                // CREATE NEW DOCUMENT
                // ===========================================================

                else
                {
                    var newDocument =
                        new EmployerVerificationDocument
                        {
                            DocumentId =
                                Guid.NewGuid(),

                            EmployerId =
                                employerId,


                            // ------------------------------------------------
                            // Master document
                            // ------------------------------------------------

                            DocumentTypeId =
                                request.DocumentTypeId,


                            // ------------------------------------------------
                            // Requested Additional
                            // ------------------------------------------------

                            RequestId =
                                request.RequestId,


                            // ------------------------------------------------
                            // Custom document name
                            // ------------------------------------------------

                            CustomDocumentName =
                                request.DocumentTypeId.HasValue
                                    ? null
                                    : request.RequestId.HasValue
                                        ? documentRequestEntity
                                            ?.CustomDocumentName
                                        : request.CustomDocumentName?
                                            .Trim(),


                            // ------------------------------------------------
                            // Category
                            // ------------------------------------------------

                            Category =
                                request.DocumentTypeId.HasValue
                                    ? master?.Category
                                    : "Additional",


                            // ------------------------------------------------
                            // File
                            // ------------------------------------------------

                            FileName =
                                request.File.FileName,

                            FileUrl =
                                uploadResult.Url,

                            PublicId =
                                uploadResult.PublicId,


                            // ------------------------------------------------
                            // Parsed information
                            // ------------------------------------------------

                            DetectedDocumentType =
                                parsed?.DocumentType,

                            DocumentNumber =
                                parsed?.DocumentNumber,

                            IssuingAuthority =
                                parsed?.IssuingAuthority,

                            IssueDate =
                                parsed?.IssueDate,

                            ExpiryDate =
                                parsed?.ExpiryDate,

                            ParsedDataJson =
                                parsed?.ParsedData?.GetRawText(),

                            AiConfidenceScore =
                                parsed?.AiConfidenceScore,


                            // ------------------------------------------------
                            // Verification
                            // ------------------------------------------------

                            Status =
                                VerificationDocumentStatus.Pending,

                            UploadedAt =
                                DateTime.UtcNow,

                            IsDeleted =
                                false
                        };


                    _context.EmployerVerificationDocuments
                        .Add(newDocument);
                }


                // ===========================================================
                // UPDATE REQUEST STATUS
                // ===========================================================
                //
                // Only requested documents should update
                // EmployerDocumentRequests.
                //
                // ===========================================================

                if (request.RequestId.HasValue)
                {
                    var requestEntity =
                        documentRequestEntity
                        ?? await _context
                            .EmployerDocumentRequests
                            .FirstOrDefaultAsync(x =>
                                x.RequestId ==
                                    request.RequestId.Value &&
                                x.EmployerId ==
                                    employerId);

                    if (requestEntity != null)
                    {
                        requestEntity.Status =
                            "Uploaded";
                    }
                }


                // ===========================================================
                // UPDATE EMPLOYER
                // ===========================================================

                employer.UpdatedAt =
                    DateTime.UtcNow;


                // ===========================================================
                // SAVE
                // ===========================================================

                await _context.SaveChangesAsync();


                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "UploadDocumentAsync error. EmployerId:{EmployerId}",
                    employerId);

                return false;
            }
        }

        public async Task<List<DocumentViewResponseDto>> GetDocumentTypesAsync()
        {
            return await _context.VerificationDocumentMasters
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new DocumentViewResponseDto
                {
                    DocumentTypeId = x.DocumentTypeId,
                    DocumentName = x.DocumentName,
                    Category = x.IsMandatory
                        ? "Mandatory"
                        : "Optional",
                    IsMandatory = x.IsMandatory
                })
                .ToListAsync();
        }

    }
}