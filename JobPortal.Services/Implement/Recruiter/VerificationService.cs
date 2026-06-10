using JobPortal.Application.DTOs.Recruiter.CompanyProfile;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
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

            public VerificationService(AppDbContext context)
            {
                _context = context;
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
                    Status = !string.IsNullOrWhiteSpace(profile.PoeLicenceS3Url)
                        ? "Approved"
                        : "Pending",
                    Description = "POE licence verification."
                });

                response.Badges.Add(new VerificationBadgeDto
                {
                    BadgeName = "RPSL Licensed",
                    Status = !string.IsNullOrWhiteSpace(profile.RpslLicenceS3Url)
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
                    FileUrl = profile.PoeLicenceS3Url,
                    Status = !string.IsNullOrWhiteSpace(profile.PoeLicenceS3Url)
                        ? "Uploaded"
                        : "Not Uploaded",
                    UploadedAt = profile.UpdatedAt
                });

                response.Documents.Add(new VerificationDocumentDto
                {
                    DocumentType = "RPSL",
                    FileUrl = profile.RpslLicenceS3Url,
                    Status = !string.IsNullOrWhiteSpace(profile.RpslLicenceS3Url)
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

            public async Task<bool> UploadDocumentAsync(
                Guid employerId,
                UploadVerificationDocumentRequestDto request)
            {
                var profile = await _context.EmployerProfiles
                    .FirstOrDefaultAsync(x => x.EmployerId == employerId);

                if (profile == null)
                    return false;

                // TODO:
                // Upload request.File to S3 / Azure Blob / Local Storage
                // Replace with actual uploaded file URL
                var fileUrl = $"uploads/{request.File.FileName}";

                switch (request.DocumentType.ToUpper())
                {
                    case "POE":
                        profile.PoeLicenceS3Url = fileUrl;
                        break;

                    case "RPSL":
                        profile.RpslLicenceS3Url = fileUrl;
                        break;

                    case "BUSINESS_REGISTRATION":
                        profile.BusinessRegDocUrl = fileUrl;
                        break;

                default:
                    throw new Exception(
                        $"Invalid document type received: '{request.DocumentType}'. " +
                        $"Allowed values are: POE, RPSL, BUSINESS_REGISTRATION");
            }

                profile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return true;
            }

            public async Task<DocumentViewResponseDto?> GetDocumentAsync(
                Guid employerId,
                string documentType)
            {
                var profile = await _context.EmployerProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EmployerId == employerId);

                if (profile == null)
                    return null;

                string? fileUrl = documentType.ToUpper() switch
                {
                    "POE" => profile.PoeLicenceS3Url,
                    "RPSL" => profile.RpslLicenceS3Url,
                    "BUSINESS_REGISTRATION" => profile.BusinessRegDocUrl,
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
