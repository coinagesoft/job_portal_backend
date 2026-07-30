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

        public async Task<CompanyDocumentResponseDto?> UploadAsync(Guid employerId, UploadCompanyDocumentRequestDto request)
        {
            try
            {
                if (request.File == null || request.File.Length == 0)
                    throw new Exception("File is required.");

                // Upload file first
                var upload = await _fileStorageService.UploadDocumentAsync(
                    request.File,
                    StorageFolder);

                // Parse using Gemini
                var parsed = await _geminiCompanyDocumentParserService
                    .ParseDocumentAsync(request.File);

                _logger.LogInformation(
    "Gemini Result => Success:{Success}, Type:{Type}, Confidence:{Confidence}, Json:{Json}",
    parsed.Success,
    parsed.DocumentType,
    parsed.AiConfidenceScore,
    parsed.ParsedData?.GetRawText());

                if (!parsed.Success)
                    throw new Exception(parsed.Message);

                VerificationDocumentMaster? master = null;

                // Existing master document selected
                if (request.DocumentTypeId.HasValue)
                {
                    master = await _context.VerificationDocumentMasters
                        .FirstOrDefaultAsync(x =>
                            x.DocumentTypeId == request.DocumentTypeId.Value &&
                            x.IsActive);

                    if (master == null)
                        throw new Exception("Document type not found.");
                }
                else
                {
                    // "Other" document uploaded
                    master = await _context.VerificationDocumentMasters
                        .FirstOrDefaultAsync(x =>
                            x.DocumentName.ToLower() == parsed.DocumentType.ToLower() &&
                            x.IsActive);

                    if (master == null)
                    {
                        master = new VerificationDocumentMaster
                        {
                            DocumentTypeId = Guid.NewGuid(),
                            Code = Guid.NewGuid().ToString("N")[..8].ToUpper(),
                            DocumentName = parsed.DocumentType,
                            Category = string.IsNullOrWhiteSpace(request.Category)
                                ? "Other"
                                : request.Category,
                            Description = request.DocumentName,
                            IsMandatory = false,
                            IsActive = true,
                            RequiresVerification = true,
                            IsSystemDocument = false,
                            AllowMultipleUploads = false,
                            AllowCustomDocument = true,
                            DisplayOrder = 999
                        };

                        _context.VerificationDocumentMasters.Add(master);
                        await _context.SaveChangesAsync();
                    }
                }

                // Only one active document allowed
                if (!master.AllowMultipleUploads)
                {
                    var existingDocuments = await _context.EmployerVerificationDocuments
                        .Where(x =>
                            x.EmployerId == employerId &&
                            x.DocumentTypeId == master.DocumentTypeId &&
                            !x.IsDeleted)
                        .ToListAsync();

                    foreach (var existing in existingDocuments)
                    {
                        if (!string.IsNullOrWhiteSpace(existing.PublicId))
                            await _fileStorageService.DeleteAsync(existing.PublicId);

                        existing.IsDeleted = true;

                        var badges = await _context.EmployerBadges
                            .Where(x => x.VerificationDocumentId == existing.DocumentId)
                            .ToListAsync();

                        foreach (var badge in badges)
                            badge.BadgeStatus = BadgeStatus.Pending;
                    }
                }

                var entity = new EmployerVerificationDocument
                {
                    DocumentId = Guid.NewGuid(),
                    EmployerId = employerId,
                    DocumentTypeId = master.DocumentTypeId,

                    // Populate these once Gemini exposes them
                   
                    DocumentNumber = parsed.DocumentNumber,
                    IssuingAuthority = parsed.IssuingAuthority,
                    IssueDate = parsed.IssueDate,
                    ExpiryDate = parsed.ExpiryDate,

                    ParsedDataJson = parsed.ParsedData?.GetRawText(),
                    AiConfidenceScore = parsed.AiConfidenceScore,
                    DetectedDocumentType = parsed.DocumentType,

                    FileName = request.File.FileName,
                    FileUrl = upload.Url,
                    PublicId = upload.PublicId,

                    Status = VerificationDocumentStatus.Pending,
                    UploadedAt = DateTime.UtcNow,
                    IsDeleted = false,

                    DocumentType = master
                };

                _context.EmployerVerificationDocuments.Add(entity);

                _context.EmployerBadges.Add(new EmployerBadge
                {
                    BadgeId = Guid.NewGuid(),
                    EmployerId = employerId,
                    VerificationDocumentId = entity.DocumentId,
                    BadgeStatus = BadgeStatus.Pending,
                    IssuedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

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
         .Where(x => x.EmployerId == employerId && !x.IsDeleted)
         .OrderByDescending(x => x.UploadedAt)
         .ToListAsync();

            return docs.Select(Map).ToList();
        }

        public async Task<CompanyDocumentResponseDto?> GetByIdAsync(Guid employerId, Guid documentId)
        {
            var doc = await _context.EmployerVerificationDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId &&
                    x.DocumentId == documentId &&
                    !x.IsDeleted);

            return doc == null ? null : Map(doc);
        }

        public async Task<CompanyDocumentResponseDto?> UpdateAsync(
           Guid employerId,
           Guid documentId,
           UpdateCompanyDocumentRequestDto request)
        {
            try
            {
                var doc = await _context.EmployerVerificationDocuments
                    .Include(x => x.DocumentType)
                    .FirstOrDefaultAsync(x =>
                        x.EmployerId == employerId &&
                        x.DocumentId == documentId &&
                        !x.IsDeleted);

                if (doc == null)
                    return null;

                // Replace uploaded file
                if (request.File != null && request.File.Length > 0)
                {
                    // Delete old file
                    if (!string.IsNullOrWhiteSpace(doc.PublicId))
                    {
                        await _fileStorageService.DeleteAsync(doc.PublicId);
                    }

                    // Upload new file
                    var upload = await _fileStorageService.UploadDocumentAsync(
                        request.File,
                        StorageFolder);

                    // Parse using Gemini
                    var parsed = await _geminiCompanyDocumentParserService
                        .ParseDocumentAsync(request.File);

                    if (!parsed.Success)
                        throw new Exception(parsed.Message);

                    doc.FileName = request.File.FileName;
                    doc.FileUrl = upload.Url;
                    doc.PublicId = upload.PublicId;

                    doc.ParsedDataJson = parsed.ParsedData?.GetRawText();
                    doc.AiConfidenceScore = parsed.AiConfidenceScore;
                    doc.DetectedDocumentType = parsed.DocumentType;

                    //When parser exposes these fields:
                    doc.DocumentNumber = parsed.DocumentNumber;
                    doc.IssuingAuthority = parsed.IssuingAuthority;
                    doc.IssueDate = parsed.IssueDate;
                    doc.ExpiryDate = parsed.ExpiryDate;
                }

                // Reset verification
                doc.Status = VerificationDocumentStatus.Pending;
                doc.VerifiedAt = null;
                doc.VerifiedBy = null;
                doc.Remarks = null;
                doc.UploadedAt = DateTime.UtcNow;

                var badges = await _context.EmployerBadges
                    .Where(x => x.VerificationDocumentId == documentId)
                    .ToListAsync();

                if (badges.Any())
                {
                    foreach (var badge in badges)
                    {
                        badge.BadgeStatus = BadgeStatus.Pending;
                    }
                }
                else
                {
                    _context.EmployerBadges.Add(new EmployerBadge
                    {
                        BadgeId = Guid.NewGuid(),
                        EmployerId = employerId,
                        VerificationDocumentId = doc.DocumentId,
                        BadgeStatus = BadgeStatus.Pending,
                        IssuedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

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

                Status = doc.Status,
                UploadedAt = doc.UploadedAt,
                VerifiedAt = doc.VerifiedAt,
                Remarks = doc.Remarks
            };
        }

    }
}
