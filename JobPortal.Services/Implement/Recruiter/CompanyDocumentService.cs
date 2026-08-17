using JobPortal.Application.DTOs.Recruiter.CompanyDocuments;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Recruiter
{
    public class CompanyDocumentService : ICompanyDocumentService
    {
        private readonly AppDbContext _context;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<CompanyDocumentService> _logger;
        private readonly IGeminiCompanyDocumentParserService _geminiCompanyDocumentParserService;

        private const string StorageFolder = "company-documents";

        public CompanyDocumentService(
            AppDbContext context,
            IFileStorageService fileStorageService,
            ILogger<CompanyDocumentService> logger,
             IGeminiCompanyDocumentParserService geminiCompanyDocumentParserService)
        {
            _context = context;
            _fileStorageService = fileStorageService;
            _logger = logger;
            _geminiCompanyDocumentParserService = geminiCompanyDocumentParserService;
        }

        public async Task<CompanyDocumentResponseDto?> UploadAsync(
          Guid employerId,
          UploadCompanyDocumentRequestDto request)
        {
            try
            {
                // ==================================================
                // VALIDATE REQUEST
                // ==================================================

                if (request == null)
                {
                    throw new Exception("Request is required.");
                }

                if (request.File == null || request.File.Length == 0)
                {
                    throw new Exception("File is required.");
                }


                // ==================================================
                // UPLOAD FILE
                // ==================================================

                var upload =
                    await _fileStorageService.UploadDocumentAsync(
                        request.File,
                        StorageFolder);


                // ==================================================
                // PARSE DOCUMENT USING GEMINI
                // ==================================================

                var parsed =
                    await _geminiCompanyDocumentParserService
                        .ParseDocumentAsync(request.File);


                _logger.LogInformation(
                    "Gemini Result => Success:{Success}, Type:{Type}, Confidence:{Confidence}, Json:{Json}",
                    parsed.Success,
                    parsed.DocumentType,
                    parsed.AiConfidenceScore,
                    parsed.ParsedData?.GetRawText());


                if (!parsed.Success)
                {
                    throw new Exception(parsed.Message);
                }


                // ==================================================
                // MASTER DOCUMENT
                // ==================================================
                //
                // IMPORTANT:
                //
                // We ONLY READ VerificationDocumentMaster here.
                //
                // We NEVER CREATE a VerificationDocumentMaster.
                //
                // If DocumentTypeId is null:
                //     this is an Additional document.
                //
                // Additional documents are stored directly in
                // EmployerVerificationDocuments.
                //
                // ==================================================

                VerificationDocumentMaster? master = null;


                // ==================================================
                // CASE 1:
                // EXISTING MASTER DOCUMENT
                // ==================================================

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
                            "Document type not found.");
                    }
                }


                // ==================================================
                // CASE 2:
                // ADDITIONAL DOCUMENT
                // ==================================================
                //
                // When DocumentTypeId is null:
                //
                // master = null
                // DocumentTypeId = null
                // Category = Additional
                //
                // NO VerificationDocumentMaster is created.
                //
                // ==================================================

                var isAdditionalDocument =
                    master == null;


                // ==================================================
                // HANDLE EXISTING MASTER DOCUMENT
                // ==================================================
                //
                // This logic applies ONLY to master documents.
                //
                // Additional documents do not use the master
                // AllowMultipleUploads configuration.
                //
                // ==================================================

                if (master != null)
                {
                    if (!master.AllowMultipleUploads)
                    {
                        var existingDocuments =
                            await _context
                                .EmployerVerificationDocuments
                                .Where(x =>
                                    x.EmployerId == employerId &&
                                    x.DocumentTypeId ==
                                        master.DocumentTypeId &&
                                    !x.IsDeleted)
                                .ToListAsync();


                        foreach (var existing in existingDocuments)
                        {
                            // --------------------------------------------------
                            // Delete old file from storage
                            // --------------------------------------------------

                            if (!string.IsNullOrWhiteSpace(
                                existing.PublicId))
                            {
                                await _fileStorageService
                                    .DeleteAsync(existing.PublicId);
                            }


                            // --------------------------------------------------
                            // Mark old document as deleted
                            // --------------------------------------------------

                            existing.IsDeleted = true;


                            // --------------------------------------------------
                            // Reset related badges
                            // --------------------------------------------------

                            var badges =
                                await _context.EmployerBadges
                                    .Where(x =>
                                        x.VerificationDocumentId ==
                                        existing.DocumentId)
                                    .ToListAsync();


                            foreach (var badge in badges)
                            {
                                badge.BadgeStatus =
                                    BadgeStatus.Pending;
                            }
                        }
                    }
                }


                // ==================================================
                // CREATE EMPLOYER DOCUMENT
                // ==================================================

                var entity =
                    new EmployerVerificationDocument
                    {
                        DocumentId =
                            Guid.NewGuid(),

                        EmployerId =
                            employerId,


                        // --------------------------------------------------
                        // DOCUMENT TYPE
                        // --------------------------------------------------
                        //
                        // Master document:
                        //     master.DocumentTypeId
                        //
                        // Additional:
                        //     null
                        //
                        DocumentTypeId =
                            master?.DocumentTypeId,


                        // --------------------------------------------------
                        // REQUEST ID
                        // --------------------------------------------------
                        //
                        // This method is for normal uploads.
                        //
                        // Requested documents should be uploaded through
                        // the request-specific upload flow so RequestId
                        // can be stored correctly.
                        //
                        RequestId =
                            null,


                        // --------------------------------------------------
                        // CUSTOM DOCUMENT NAME
                        // --------------------------------------------------
                        //
                        // Only Additional documents need a custom name.
                        //
                        CustomDocumentName =
                            isAdditionalDocument
                                ? (
                                    !string.IsNullOrWhiteSpace(
                                        request.DocumentName)
                                        ? request.DocumentName
                                        : parsed.DocumentType
                                  )
                                : null,


                        // --------------------------------------------------
                        // CATEGORY
                        // --------------------------------------------------
                        //
                        // Existing master:
                        //     master.Category
                        //
                        // Additional:
                        //     Additional
                        //
                        Category =
                            isAdditionalDocument
                                ? "Additional"
                                : master!.Category,


                        // --------------------------------------------------
                        // DOCUMENT DETAILS FROM GEMINI
                        // --------------------------------------------------

                        DocumentNumber =
                            parsed.DocumentNumber,

                        IssuingAuthority =
                            parsed.IssuingAuthority,

                        IssueDate =
                            parsed.IssueDate,

                        ExpiryDate =
                            parsed.ExpiryDate,


                        // --------------------------------------------------
                        // AI PARSED DATA
                        // --------------------------------------------------

                        ParsedDataJson =
                            parsed.ParsedData?.GetRawText(),

                        AiConfidenceScore =
                            parsed.AiConfidenceScore,

                        DetectedDocumentType =
                            parsed.DocumentType,


                        // --------------------------------------------------
                        // FILE DETAILS
                        // --------------------------------------------------

                        FileName =
                            request.File.FileName,

                        FileUrl =
                            upload.Url,

                        PublicId =
                            upload.PublicId,


                        // --------------------------------------------------
                        // VERIFICATION STATUS
                        // --------------------------------------------------

                        Status =
                            VerificationDocumentStatus.Pending,

                        UploadedAt =
                            DateTime.UtcNow,

                        IsDeleted =
                            false,


                        // --------------------------------------------------
                        // NAVIGATION
                        // --------------------------------------------------

                        DocumentType =
                            master
                    };


                // ==================================================
                // SAVE DOCUMENT
                // ==================================================

                _context.EmployerVerificationDocuments
                    .Add(entity);


                // ==================================================
                // CREATE BADGE
                // ==================================================

                _context.EmployerBadges.Add(
                    new EmployerBadge
                    {
                        BadgeId =
                            Guid.NewGuid(),

                        EmployerId =
                            employerId,

                        VerificationDocumentId =
                            entity.DocumentId,

                        BadgeStatus =
                            BadgeStatus.Pending,

                        IssuedAt =
                            DateTime.UtcNow
                    });


                // ==================================================
                // SAVE CHANGES
                // ==================================================

                await _context.SaveChangesAsync();


                // ==================================================
                // RESPONSE
                // ==================================================

                return Map(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "UploadAsync error. EmployerId:{EmployerId}",
                    employerId);

                return null;
            }
        }

        public async Task<List<CompanyDocumentResponseDto>> GetMyDocumentsAsync(Guid employerId)
        {
            var docs = await _context.EmployerVerificationDocuments
                .AsNoTracking()
                .Include(x => x.DocumentType)
                .Where(x =>
                    x.EmployerId == employerId &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.UploadedAt)
                .ToListAsync();

            return docs.Select(x => new CompanyDocumentResponseDto
            {
                DocumentId = x.DocumentId,

                DocumentTypeId = x.DocumentTypeId,

                DocumentName =
                    x.DocumentType?.DocumentName
                    ?? x.CustomDocumentName
                    ?? x.DetectedDocumentType
                    ?? "Additional Document",

                Category =
                    x.DocumentType?.Category
                    ?? x.Category,

                DocumentNumber = x.DocumentNumber,

                IssuingAuthority = x.IssuingAuthority,

                IssueDate = x.IssueDate,

                ExpiryDate = x.ExpiryDate,

                FileName = x.FileName,

                FileUrl = x.FileUrl,

                PublicId = x.PublicId,

                // Convert enum number to string
                Status = x.Status.ToString(),

                VerifiedBy = x.VerifiedBy,

                UploadedAt = x.UploadedAt,

                VerifiedAt = x.VerifiedAt,

                Remarks = x.Remarks,

                DetectedDocumentType = x.DetectedDocumentType,


                // Use IsMandatory instead of IsMasterDocument
                IsMandatory = x.DocumentType?.IsMandatory ?? false
            }).ToList();
        }

        public async Task<CompanyDocumentResponseDto?> GetByIdAsync(
        Guid employerId,
        Guid documentId)
        {
            var doc = await _context.EmployerVerificationDocuments
                .AsNoTracking()
                .Include(x => x.DocumentType)
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId &&
                    x.DocumentId == documentId &&
                    !x.IsDeleted);

            if (doc == null)
            {
                return null;
            }

            return new CompanyDocumentResponseDto
            {
                DocumentId = doc.DocumentId,

                DocumentTypeId = doc.DocumentTypeId,

                DocumentName =
                    doc.DocumentType?.DocumentName
                    ?? doc.CustomDocumentName
                    ?? doc.DetectedDocumentType
                    ?? "Additional Document",

                Category =
                    doc.DocumentType?.Category
                    ?? doc.Category,

                DocumentNumber = doc.DocumentNumber,

                IssuingAuthority = doc.IssuingAuthority,

                IssueDate = doc.IssueDate,

                ExpiryDate = doc.ExpiryDate,

                FileName = doc.FileName,

                FileUrl = doc.FileUrl,

                PublicId = doc.PublicId,

                DetectedDocumentType =
                    doc.DetectedDocumentType,

                AiConfidenceScore =
                    doc.AiConfidenceScore,

                Status =
                    doc.Status.ToString(),

                VerifiedBy =
                    doc.VerifiedBy,

                UploadedAt =
                    doc.UploadedAt,

                VerifiedAt =
                    doc.VerifiedAt,

                Remarks =
                    doc.Remarks,

                IsMandatory =
                    doc.DocumentType?.IsMandatory ?? false
            };
        }

        public async Task<CompanyDocumentResponseDto?> UpdateAsync(
         Guid employerId,
         Guid documentId,
         UpdateCompanyDocumentRequestDto request)
        {
            try
            {
                // ==================================================
                // FIND EXISTING DOCUMENT
                // ==================================================

                var doc = await _context.EmployerVerificationDocuments
                    .Include(x => x.DocumentType)
                    .FirstOrDefaultAsync(x =>
                        x.EmployerId == employerId &&
                        x.DocumentId == documentId &&
                        !x.IsDeleted);

                if (doc == null)
                {
                    return null;
                }


                // ==================================================
                // KEEP EXISTING DOCUMENT TYPE / CATEGORY
                // ==================================================
                //
                // IMPORTANT:
                //
                // We do NOT create or change a
                // VerificationDocumentMaster here.
                //
                // Existing master document:
                //     DocumentTypeId remains unchanged.
                //
                // Normal Additional:
                //     DocumentTypeId remains NULL.
                //     Category remains Additional.
                //
                // Requested Additional:
                //     RequestId remains unchanged.
                //
                // ==================================================


                // ==================================================
                // REPLACE UPLOADED FILE
                // ==================================================

                if (request.File != null &&
                    request.File.Length > 0)
                {
                    // --------------------------------------------------
                    // DELETE OLD FILE
                    // --------------------------------------------------

                    if (!string.IsNullOrWhiteSpace(doc.PublicId))
                    {
                        await _fileStorageService
                            .DeleteAsync(doc.PublicId);
                    }


                    // --------------------------------------------------
                    // UPLOAD NEW FILE
                    // --------------------------------------------------

                    var upload =
                        await _fileStorageService
                            .UploadDocumentAsync(
                                request.File,
                                StorageFolder);


                    // --------------------------------------------------
                    // PARSE NEW FILE USING GEMINI
                    // --------------------------------------------------

                    var parsed =
                        await _geminiCompanyDocumentParserService
                            .ParseDocumentAsync(request.File);


                    if (!parsed.Success)
                    {
                        throw new Exception(parsed.Message);
                    }


                    // --------------------------------------------------
                    // UPDATE FILE INFORMATION
                    // --------------------------------------------------

                    doc.FileName =
                        request.File.FileName;

                    doc.FileUrl =
                        upload.Url;

                    doc.PublicId =
                        upload.PublicId;


                    // --------------------------------------------------
                    // UPDATE GEMINI INFORMATION
                    // --------------------------------------------------
                    //
                    // Gemini information is only information about
                    // the uploaded file.
                    //
                    // It must NOT create/change DocumentTypeId.
                    //
                    // --------------------------------------------------

                    doc.ParsedDataJson =
                        parsed.ParsedData?.GetRawText();

                    doc.AiConfidenceScore =
                        parsed.AiConfidenceScore;

                    doc.DetectedDocumentType =
                        parsed.DocumentType;


                    // --------------------------------------------------
                    // UPDATE PARSED DOCUMENT DETAILS
                    // --------------------------------------------------

                    doc.DocumentNumber =
                        parsed.DocumentNumber;

                    doc.IssuingAuthority =
                        parsed.IssuingAuthority;

                    doc.IssueDate =
                        parsed.IssueDate;

                    doc.ExpiryDate =
                        parsed.ExpiryDate;
                }


                // ==================================================
                // PRESERVE DOCUMENT CATEGORY
                // ==================================================
                //
                // Do NOT change Additional into a Master document.
                //
                // If it is already an Additional document:
                //
                //     Category = Additional
                //     DocumentTypeId = null
                //
                // If it is RequestedAdditional:
                //
                //     RequestId != null
                //
                // Keep the existing relationship.
                //
                // ==================================================

                if (!doc.RequestId.HasValue &&
                    string.Equals(
                        doc.Category,
                        "Additional",
                        StringComparison.OrdinalIgnoreCase))
                {
                    doc.DocumentTypeId = null;
                    doc.Category = "Additional";
                }


                // ==================================================
                // RESET VERIFICATION
                // ==================================================

                doc.Status =
                    VerificationDocumentStatus.Pending;

                doc.VerifiedAt =
                    null;

                doc.VerifiedBy =
                    null;

                doc.Remarks =
                    null;

                doc.UploadedAt =
                    DateTime.UtcNow;


                // ==================================================
                // UPDATE BADGES
                // ==================================================

                var badges =
                    await _context.EmployerBadges
                        .Where(x =>
                            x.VerificationDocumentId ==
                            documentId)
                        .ToListAsync();


                if (badges.Any())
                {
                    foreach (var badge in badges)
                    {
                        badge.BadgeStatus =
                            BadgeStatus.Pending;
                    }
                }
                else
                {
                    _context.EmployerBadges.Add(
                        new EmployerBadge
                        {
                            BadgeId =
                                Guid.NewGuid(),

                            EmployerId =
                                employerId,

                            VerificationDocumentId =
                                doc.DocumentId,

                            BadgeStatus =
                                BadgeStatus.Pending,

                            IssuedAt =
                                DateTime.UtcNow
                        });
                }


                // ==================================================
                // SAVE
                // ==================================================

                await _context.SaveChangesAsync();


                // ==================================================
                // RESPONSE
                // ==================================================

                return Map(doc);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "UpdateAsync error. EmployerId:{EmployerId} DocumentId:{DocumentId}",
                    employerId,
                    documentId);

                return null;
            }
        }

        public async Task<bool> DeleteAsync(Guid employerId, Guid documentId)
        {
            try
            {
                var doc = await _context.EmployerVerificationDocuments
                    .FirstOrDefaultAsync(x =>
                        x.EmployerId == employerId &&
                        x.DocumentId == documentId &&
                        !x.IsDeleted);

                if (doc == null)
                    return false;

                doc.IsDeleted = true;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "DeleteAsync error. EmployerId:{EmployerId} DocumentId:{DocumentId}",
                    employerId, documentId);
                return false;
            }
        }

        private static CompanyDocumentResponseDto Map(EmployerVerificationDocument doc)
        {
            return new CompanyDocumentResponseDto
            {
                DocumentId = doc.DocumentId,
                DocumentTypeId = doc.DocumentTypeId,

                DocumentName = doc.DocumentType?.DocumentName ?? string.Empty,
                Category = doc.DocumentType?.Category ?? string.Empty,

                DocumentNumber = doc.DocumentNumber,
                IssuingAuthority = doc.IssuingAuthority,
                IssueDate = doc.IssueDate,
                ExpiryDate = doc.ExpiryDate,

                FileName = doc.FileName,
                FileUrl = doc.FileUrl,

                DetectedDocumentType = doc.DetectedDocumentType,
                AiConfidenceScore = doc.AiConfidenceScore,

                Status = doc.Status.ToString(),
                UploadedAt = doc.UploadedAt,
                VerifiedAt = doc.VerifiedAt,
                Remarks = doc.Remarks
            };
        }

    }
}
