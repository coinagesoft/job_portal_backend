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

        //public async Task<VerificationDashboardResponseDto?> GetVerificationDashboardAsync(
        //        Guid employerId)
        //{
        //    var profile = await _context.EmployerProfiles
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(x => x.EmployerId == employerId);

        //    if (profile == null)
        //        return null;

        //    var response = new VerificationDashboardResponseDto();

        //    response.Badges.Add(new VerificationBadgeDto
        //    {
        //        BadgeName = "GST",
        //        Status = !string.IsNullOrWhiteSpace(profile.GstCertificateUrl)
        //            ? "Uploaded"
        //            : "Not Uploaded",
        //        Description = "GST registration verification."
        //    });

        //    response.Badges.Add(new VerificationBadgeDto
        //    {
        //        BadgeName = "PAN",
        //        Status = !string.IsNullOrWhiteSpace(profile.PanCardUrl)
        //            ? "Uploaded"
        //            : "Not Uploaded",
        //        Description = "PAN verification status."
        //    });

        //    response.Badges.Add(new VerificationBadgeDto
        //    {
        //        BadgeName = "POE Licensed",
        //        Status = !string.IsNullOrWhiteSpace(profile.PoeLicenceUrl)
        //            ? "Uploaded"
        //            : "Not Uploaded",
        //        Description = "POE licence verification."
        //    });

        //    response.Badges.Add(new VerificationBadgeDto
        //    {
        //        BadgeName = "RPSL Licensed",
        //        Status = !string.IsNullOrWhiteSpace(profile.RpslLicenceUrl)
        //            ? "Uploaded"
        //            : "Not Uploaded",
        //        Description = "RPSL licence verification."
        //    });

        //    response.Documents.Add(new VerificationDocumentDto
        //    {
        //        DocumentType = "GST",
        //        FileUrl = profile.GstCertificateUrl,
        //        Status = !string.IsNullOrWhiteSpace(profile.GstCertificateUrl)
        //            ? "Uploaded"
        //            : "Not Uploaded",
        //        UploadedAt = !string.IsNullOrWhiteSpace(profile.GstCertificateUrl) ? profile.UpdatedAt : null
        //    });

        //    response.Documents.Add(new VerificationDocumentDto
        //    {
        //        DocumentType = "PAN",
        //        FileUrl = profile.PanCardUrl,
        //        Status = !string.IsNullOrWhiteSpace(profile.PanCardUrl)
        //            ? "Uploaded"
        //            : "Not Uploaded",
        //        UploadedAt = !string.IsNullOrWhiteSpace(profile.PanCardUrl) ? profile.UpdatedAt : null
        //    });

        //    response.Documents.Add(new VerificationDocumentDto
        //    {
        //        DocumentType = "POE",
        //        FileUrl = profile.PoeLicenceUrl,
        //        Status = !string.IsNullOrWhiteSpace(profile.PoeLicenceUrl)
        //            ? "Uploaded"
        //            : "Not Uploaded",
        //        UploadedAt = !string.IsNullOrWhiteSpace(profile.PoeLicenceUrl) ? profile.UpdatedAt : null
        //    });

        //    response.Documents.Add(new VerificationDocumentDto
        //    {
        //        DocumentType = "RPSL",
        //        FileUrl = profile.RpslLicenceUrl,
        //        Status = !string.IsNullOrWhiteSpace(profile.RpslLicenceUrl)
        //            ? "Uploaded"
        //            : "Not Uploaded",
        //        UploadedAt = !string.IsNullOrWhiteSpace(profile.RpslLicenceUrl) ? profile.UpdatedAt : null
        //    });

        //    response.Documents.Add(new VerificationDocumentDto
        //    {
        //        DocumentType = "BUSINESS_REGISTRATION",
        //        FileUrl = profile.BusinessRegDocUrl,
        //        Status = !string.IsNullOrWhiteSpace(profile.BusinessRegDocUrl)
        //            ? "Uploaded"
        //            : "Not Uploaded",
        //        UploadedAt = !string.IsNullOrWhiteSpace(profile.BusinessRegDocUrl) ? profile.UpdatedAt : null
        //    });

        //    return response;
        //}

        public async Task<VerificationDashboardResponseDto?> GetVerificationDashboardAsync(
       Guid employerId)
        {
            var employerExists = await _context.EmployerProfiles
                .AsNoTracking()
                .AnyAsync(x => x.EmployerId == employerId);

            if (!employerExists)
                return null;

            var response = new VerificationDashboardResponseDto();

            // ===========================================================
            // Load Master Document Types (Admin Created)
            // ===========================================================
            var masters = await _context.VerificationDocumentMasters
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            // ===========================================================
            // Load Employer Uploaded Documents
            // ===========================================================
            var uploadedDocuments = await _context.EmployerVerificationDocuments
                .AsNoTracking()
                .Where(x => x.EmployerId == employerId && !x.IsDeleted)
                .ToListAsync();

            // ===========================================================
            // Standard Documents (GST, PAN, RPSL etc.)
            // ===========================================================
            foreach (var master in masters)
            {
                var uploaded = uploadedDocuments
                    .Where(x =>
                        x.DocumentTypeId.HasValue &&
                        x.DocumentTypeId.Value == master.DocumentTypeId)
                    .OrderByDescending(x => x.UploadedAt)
                    .FirstOrDefault();

                var status = uploaded == null
                    ? "Not Uploaded"
                    : uploaded.Status.ToString();

                response.Badges.Add(new VerificationBadgeDto
                {
                    BadgeName = master.DocumentName,
                    Status = status,
                    Description = master.Description
                        ?? $"{master.DocumentName} verification."
                });

                response.Documents.Add(new VerificationDocumentDto
                {
                    DocumentType = master.DocumentName,
                    FileUrl = uploaded?.FileUrl,
                    Status = status,
                    UploadedAt = uploaded?.UploadedAt
                });
            }

            // ===========================================================
            // Additional Documents (Employer Specific)
            // ===========================================================
            var additionalDocuments = uploadedDocuments
                .Where(x => x.DocumentTypeId == null)
                .OrderByDescending(x => x.UploadedAt)
                .ToList();

            foreach (var doc in additionalDocuments)
            {
                var documentName =
                    doc.CustomDocumentName
                    ?? doc.DetectedDocumentType
                    ?? doc.FileName;

                // Badge
                response.Badges.Add(new VerificationBadgeDto
                {
                    BadgeName = documentName,
                    Status = doc.Status.ToString(),
                    Description = "Additional verification document."
                });

                // Document
                response.Documents.Add(new VerificationDocumentDto
                {
                    DocumentType = documentName,
                    FileUrl = doc.FileUrl,
                    Status = doc.Status.ToString(),
                    UploadedAt = doc.UploadedAt
                });
            }

            return response;
        }

        public async Task<bool> UploadDocumentAsync(
     Guid employerId,
     UploadVerificationDocumentRequestDto request)
        {
            try
            {
                var employer = await _context.EmployerProfiles
                    .FirstOrDefaultAsync(x => x.EmployerId == employerId);

                if (employer == null)
                    return false;

                if (request.File == null || request.File.Length == 0)
                    throw new Exception("File is required.");

                // Validate document type (only for standard documents)
                VerificationDocumentMaster? master = null;

                if (request.DocumentTypeId.HasValue)
                {
                    master = await _context.VerificationDocumentMasters
                        .FirstOrDefaultAsync(x =>
                            x.DocumentTypeId == request.DocumentTypeId &&
                            x.IsActive);

                    if (master == null)
                        throw new Exception("Invalid document type.");
                }

                // Upload file
                var uploadResult = await _fileStorageService.UploadDocumentAsync(
                    request.File,
                    "verification-documents");

                if (string.IsNullOrWhiteSpace(uploadResult.Url))
                    throw new Exception("Failed to upload document.");

                // Parse document using Gemini (optional)
                GeminiCompanyDocumentParseResponse? parsed = null;

                try
                {
                    parsed = await _geminiCompanyDocumentParserService
                        .ParseDocumentAsync(request.File);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Gemini parsing failed for {FileName}",
                        request.File.FileName);
                }

                EmployerVerificationDocument? existing = null;

                // ===========================================================
                // STANDARD DOCUMENTS (GST, PAN, RPSL, etc.)
                // ===========================================================
                if (request.DocumentTypeId.HasValue)
                {
                    existing = await _context.EmployerVerificationDocuments
                        .FirstOrDefaultAsync(x =>
                            x.EmployerId == employerId &&
                            x.DocumentTypeId == request.DocumentTypeId &&
                            !x.IsDeleted);

                    if (existing != null)
                    {
                        if (!string.IsNullOrWhiteSpace(existing.PublicId))
                        {
                            await _fileStorageService.DeleteAsync(existing.PublicId);
                        }

                        existing.FileName = request.File.FileName;
                        existing.FileUrl = uploadResult.Url;
                        existing.PublicId = uploadResult.PublicId;
                        existing.UploadedAt = DateTime.UtcNow;

                        existing.DocumentNumber = parsed?.DocumentNumber;
                        existing.IssuingAuthority = parsed?.IssuingAuthority;
                        existing.IssueDate = parsed?.IssueDate;
                        existing.ExpiryDate = parsed?.ExpiryDate;

                        existing.DetectedDocumentType = parsed?.DocumentType;
                        existing.ParsedDataJson = parsed?.ParsedData?.GetRawText();
                        existing.AiConfidenceScore = parsed?.AiConfidenceScore;

                        existing.Status = VerificationDocumentStatus.Pending;
                        existing.VerifiedAt = null;
                        existing.VerifiedBy = null;
                        existing.Remarks = null;
                    }
                    else
                    {
                        _context.EmployerVerificationDocuments.Add(
                            new EmployerVerificationDocument
                            {
                                DocumentId = Guid.NewGuid(),

                                EmployerId = employerId,
                                DocumentTypeId = request.DocumentTypeId,

                                CustomDocumentName = null,
                                Category = master?.Category,

                                FileName = request.File.FileName,
                                FileUrl = uploadResult.Url,
                                PublicId = uploadResult.PublicId,

                                UploadedAt = DateTime.UtcNow,

                                DetectedDocumentType = parsed?.DocumentType,
                                DocumentNumber = parsed?.DocumentNumber,
                                IssuingAuthority = parsed?.IssuingAuthority,
                                IssueDate = parsed?.IssueDate,
                                ExpiryDate = parsed?.ExpiryDate,
                                ParsedDataJson = parsed?.ParsedData?.GetRawText(),
                                AiConfidenceScore = parsed?.AiConfidenceScore,

                                Status = VerificationDocumentStatus.Pending
                            });
                    }
                }

                // ===========================================================
                // OTHER DOCUMENTS
                // ===========================================================
                else
                {
                    var detectedName = parsed?.DocumentType?.Trim();

                    existing = await _context.EmployerVerificationDocuments
                        .FirstOrDefaultAsync(x =>
                            x.EmployerId == employerId &&
                            x.DocumentTypeId == null &&
                            x.DetectedDocumentType == detectedName &&
                            !x.IsDeleted);

                    if (existing != null)
                    {
                        if (!string.IsNullOrWhiteSpace(existing.PublicId))
                        {
                            await _fileStorageService.DeleteAsync(existing.PublicId);
                        }

                        existing.FileName = request.File.FileName;
                        existing.FileUrl = uploadResult.Url;
                        existing.PublicId = uploadResult.PublicId;
                        existing.UploadedAt = DateTime.UtcNow;

                        existing.DocumentNumber = parsed?.DocumentNumber;
                        existing.IssuingAuthority = parsed?.IssuingAuthority;
                        existing.IssueDate = parsed?.IssueDate;
                        existing.ExpiryDate = parsed?.ExpiryDate;

                        existing.DetectedDocumentType = parsed?.DocumentType;
                        existing.ParsedDataJson = parsed?.ParsedData?.GetRawText();
                        existing.AiConfidenceScore = parsed?.AiConfidenceScore;

                        existing.Status = VerificationDocumentStatus.Pending;
                        existing.VerifiedAt = null;
                        existing.VerifiedBy = null;
                        existing.Remarks = null;
                    }
                    else
                    {
                        _context.EmployerVerificationDocuments.Add(
                            new EmployerVerificationDocument
                            {
                                DocumentId = Guid.NewGuid(),

                                EmployerId = employerId,

                                // Additional document
                                DocumentTypeId = null,
                                CustomDocumentName = null,
                                Category = "Additional",

                                FileName = request.File.FileName,
                                FileUrl = uploadResult.Url,
                                PublicId = uploadResult.PublicId,

                                UploadedAt = DateTime.UtcNow,

                                DetectedDocumentType = parsed?.DocumentType,
                                DocumentNumber = parsed?.DocumentNumber,
                                IssuingAuthority = parsed?.IssuingAuthority,
                                IssueDate = parsed?.IssueDate,
                                ExpiryDate = parsed?.ExpiryDate,
                                ParsedDataJson = parsed?.ParsedData?.GetRawText(),
                                AiConfidenceScore = parsed?.AiConfidenceScore,

                                Status = VerificationDocumentStatus.Pending
                            });
                    }
                }

                employer.UpdatedAt = DateTime.UtcNow;

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
                    Category = x.Category,
                    IsMandatory = x.IsMandatory
                })
                .ToListAsync();
        }


    }
}