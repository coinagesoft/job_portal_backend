using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    namespace JobPortal.Services.Implement.Recruiter
    {
        public class VerificationService : IVerificationService
        {
        private readonly AppDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<VerificationService> _logger;
        public VerificationService(
     AppDbContext context,
     ICloudinaryService cloudinaryService,
     ILogger<VerificationService> logger)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        public async Task<VerificationDashboardResponseDto?> GetVerificationDashboardAsync(
                Guid employerId)
            {
                var profile = await _context.EmployerProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployerId == employerId);

                if (profile == null)
                    return null;

                var response = new VerificationDashboardResponseDto();

                response.Badges.Add(new VerificationBadgeDto
                {
                    BadgeName = "GST Verified",
                    Status = profile.GstRegistered ? "Approved" : "Pending",
                    Description = "GST registration verification."
                });

                response.Badges.Add(new VerificationBadgeDto
                {
                    BadgeName = "PAN Verified",
                    Status = !string.IsNullOrWhiteSpace(profile.Pan)
                        ? "Approved"
                        : "Pending",
                    Description = "PAN verification status."
                });

                response.Badges.Add(new VerificationBadgeDto
                {
                    BadgeName = "POE Licensed",
                    Status = !string.IsNullOrWhiteSpace(profile.PoeLicenceUrl)
                        ? "Approved"
                        : "Pending",
                    Description = "POE licence verification."
                });

                response.Badges.Add(new VerificationBadgeDto
                {
                    BadgeName = "RPSL Licensed",
                    Status = !string.IsNullOrWhiteSpace(profile.RpslLicenceUrl)
                        ? "Approved"
                        : "Pending",
                    Description = "RPSL licence verification."
                });

                response.Documents.Add(new VerificationDocumentDto
                {
                    DocumentType = "GST",
                    FileUrl = profile.GstRegistered ? profile.BusinessRegDocUrl : null,
                    Status = profile.GstRegistered ? "Uploaded" : "Not Uploaded",
                    UploadedAt = profile.UpdatedAt
                });

                response.Documents.Add(new VerificationDocumentDto
                {
                    DocumentType = "PAN",
                    FileUrl = null,
                    Status = !string.IsNullOrWhiteSpace(profile.Pan)
                        ? "Available"
                        : "Not Available",
                    UploadedAt = profile.UpdatedAt
                });

                response.Documents.Add(new VerificationDocumentDto
                {
                    DocumentType = "POE",
                    FileUrl = profile.PoeLicenceUrl,
                    Status = !string.IsNullOrWhiteSpace(profile.PoeLicenceUrl)
                        ? "Uploaded"
                        : "Not Uploaded",
                    UploadedAt = profile.UpdatedAt
                });

                response.Documents.Add(new VerificationDocumentDto
                {
                    DocumentType = "RPSL",
                    FileUrl = profile.RpslLicenceUrl,
                    Status = !string.IsNullOrWhiteSpace(profile.RpslLicenceUrl)
                        ? "Uploaded"
                        : "Not Uploaded",
                    UploadedAt = profile.UpdatedAt
                });

                response.Documents.Add(new VerificationDocumentDto
                {
                    DocumentType = "BUSINESS_REGISTRATION",
                    FileUrl = profile.BusinessRegDocUrl,
                    Status = !string.IsNullOrWhiteSpace(profile.BusinessRegDocUrl)
                        ? "Uploaded"
                        : "Not Uploaded",
                    UploadedAt = profile.UpdatedAt
                });

                return response;
            }


        public async Task<bool> UploadDocumentAsync(Guid employerId,UploadVerificationDocumentRequestDto request)
        {
            try
            {
                var profile = await _context.EmployerProfiles
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

    if (profile == null)
                    return false;

                if (request.File == null || request.File.Length == 0)
                    throw new Exception("File is required.");

                switch (request.DocumentType)
                {
                    case DocumentType.POE:

                        // Remove old file
                        if (!string.IsNullOrWhiteSpace(profile.PoeLicencePublicId))
                        {
                            await _cloudinaryService.DeleteAsync(
                                profile.PoeLicencePublicId);
                        }

                        var poeUpload =
                            await _cloudinaryService.UploadDocumentAsync(
                                request.File,
                                "verification-documents");

                        profile.PoeLicenceUrl = poeUpload.Url;
                        profile.PoeLicencePublicId = poeUpload.PublicId;

                        break;

                    case DocumentType.RPSL:

                        if (!string.IsNullOrWhiteSpace(profile.RpslLicencePublicId))
                        {
                            await _cloudinaryService.DeleteAsync(
                                profile.RpslLicencePublicId);
                        }

                        var rpslUpload =
                            await _cloudinaryService.UploadDocumentAsync(
                                request.File,
                                "verification-documents");

                        profile.RpslLicenceUrl = rpslUpload.Url;
                        profile.RpslLicencePublicId = rpslUpload.PublicId;

                        break;

                    case DocumentType.BUSINESS_REGISTRATION:

                        var businessUpload =
                            await _cloudinaryService.UploadDocumentAsync(
                                request.File,
                                "verification-documents");

                        profile.BusinessRegDocUrl = businessUpload.Url;

                        break;

                    default:

                        throw new Exception(
                            $"Invalid document type '{request.DocumentType}'.");
                }

                profile.UpdatedAt = DateTime.UtcNow;

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




        public async Task<DocumentViewResponseDto?> GetDocumentAsync(
        Guid employerId,
        DocumentType documentType)
        {
            var profile = await _context.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployerId == employerId);

            if (profile == null)
                return null;

            string? fileUrl = documentType switch
            {
                DocumentType.POE => profile.PoeLicenceUrl,

                DocumentType.RPSL => profile.RpslLicenceUrl,

                DocumentType.BUSINESS_REGISTRATION => profile.BusinessRegDocUrl,

                _ => null
            };

            return new DocumentViewResponseDto
            {
                DocumentType = documentType,
                FileUrl = fileUrl
            };
        }
    }
    }
