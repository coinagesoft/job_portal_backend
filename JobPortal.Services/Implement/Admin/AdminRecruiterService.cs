using JobPortal.Application.DTOs;
using JobPortal.Application.DTOs.Admin;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Admin
{
    public class AdminRecruiterService : IAdminRecruiterService
    {
        private readonly AppDbContext _db; // rename to your actual DbContext class

        public AdminRecruiterService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<AdminRecruiterListItemDto>> GetRecruitersAsync()
        {
            // VerificationDocumentMaster contains only
            // Admin-created/common documents.
            //
            // Only active document types are considered.
            var commonDocumentTypes = await _db.VerificationDocumentMasters
                .AsNoTracking()
                .Where(d => d.IsActive)
                .Select(d => new
                {
                    d.DocumentTypeId,
                    d.DocumentName
                })
                .ToListAsync();

            var employers = await _db.EmployerProfiles
                .AsNoTracking()
                .Include(e => e.User)
                .Include(e => e.VerificationDocuments)
                    .ThenInclude(d => d.DocumentType)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var docsTotal = commonDocumentTypes.Count;

            return employers.Select(e =>
            {
                // Only recruiter documents that belong to an
                // Admin-created document type are considered.
                //
                // Additional documents have no matching DocumentType
                // and therefore are ignored.
                var commonDocuments = e.VerificationDocuments
                    .Where(d =>
                        !d.IsDeleted &&
                        d.DocumentTypeId.HasValue &&
                        commonDocumentTypes.Any(
                            master => master.DocumentTypeId == d.DocumentTypeId.Value
                        )
                    )
                    .ToList();

                // Count Admin-created document TYPES that have
                // at least one approved document.
                //
                // Multiple uploads of the same document type
                // are counted as ONE document.
                var docsVerified = commonDocumentTypes.Count(master =>
                    commonDocuments.Any(d =>
                        d.DocumentTypeId == master.DocumentTypeId &&
                        d.Status == JobPortal.Domain.Enums.RecruiterEnums.VerificationDocumentStatus.Approved
                    )
                );

                // If any Admin-created/common document is rejected,
                // overall verification is Rejected.
                var hasRejectedDocument = commonDocumentTypes.Any(master =>
                    commonDocuments.Any(d =>
                        d.DocumentTypeId == master.DocumentTypeId &&
                        d.Status == JobPortal.Domain.Enums.RecruiterEnums.VerificationDocumentStatus.Rejected
                    )
                );

                string overallVerificationStatus;

                if (hasRejectedDocument)
                {
                    overallVerificationStatus = "Rejected";
                }
                else if (docsTotal > 0 && docsVerified == docsTotal)
                {
                    overallVerificationStatus = "Verified";
                }
                else
                {
                    overallVerificationStatus = "Pending";
                }

                return new AdminRecruiterListItemDto
                {
                    Id = e.EmployerId.ToString(),

                    Logo = e.CompanyLogoUrl,

                    Company = e.CompanyDisplayName,

                    Sector = e.IndustryType,

                    Person = e.ContactPersonName,

                    Email = string.IsNullOrWhiteSpace(e.ContactEmailPublic)
                            ? e.User.Email
                            : e.ContactEmailPublic,

                    Plan = "",

                    // KEEP "Gst" because frontend already expects "gst".
                    // This now represents overall common-document verification.
                    Gst = overallVerificationStatus,

                    // Only Admin-created/common documents
                    DocsVerified = docsVerified,

                    // Total Admin-created/common documents
                    DocsTotal = docsTotal,

                    Status = e.AccountStatus.ToString(),

                    Registered = e.CreatedAt.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    )
                };
            }).ToList();
        }

        public async Task<bool> UpdateRecruiterStatusAsync(
            Guid employerId,
            string status,
            string? reason,
            Guid performedByAdminId,
            string ipAddress,
            string? userAgent)
        {
            var employer = await _db.EmployerProfiles
                .Include(e => e.User)
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            if (employer == null)
            {
                return false;
            }

            if (!Enum.TryParse<AccountStatus>(
                status,
                true,
                out var accountStatus))
            {
                throw new ArgumentException(
                    $"Invalid recruiter status: {status}"
                );
            }

            // Get admin who performed the action
            var admin = await _db.AdminUsers
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.AdminId == performedByAdminId);

            if (admin == null)
            {
                throw new ArgumentException(
                    "Admin user not found."
                );
            }

            // Keep old values for audit
            var oldStatus = employer.AccountStatus.ToString();
            var oldSuspensionReason = employer.User.SuspensionReason;

            // Validate suspension reason
            if (accountStatus == AccountStatus.Suspended &&
                string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(
                    "Suspension reason is required."
                );
            }

            // New suspension reason
            var newSuspensionReason =
                accountStatus == AccountStatus.Suspended
                    ? reason!.Trim()
                    : null;

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                // -----------------------------------------
                // UPDATE EMPLOYER PROFILE
                // -----------------------------------------
                employer.AccountStatus = accountStatus;

                // -----------------------------------------
                // UPDATE USER
                // -----------------------------------------
                employer.User.AccountStatus = accountStatus;

                employer.User.SuspensionReason =
                    newSuspensionReason;

                employer.UpdatedAt = DateTime.UtcNow;
                employer.User.UpdatedAt = DateTime.UtcNow;

                // -----------------------------------------
                // CREATE AUDIT LOG
                // -----------------------------------------
                var auditLog = new AuditLog
                {
                    LogId = Guid.NewGuid(),

                    PerformedByAdminId = admin.AdminId,

                    PerformedByName = admin.FullName,

                    // Use actual assigned admin role
                    PerformedByRole = admin.Role?.RoleName
                                      ?? admin.AdminType,

                    Module = "Recruiters",

                    Action = "Update Status",

                    TargetEntityType = "EmployerProfile",

                    TargetEntityId = employer.EmployerId,

                    TargetEntityName = employer.CompanyDisplayName,

                    Severity = accountStatus == AccountStatus.Suspended
                        ? AuditSeverity.Warning
                        : AuditSeverity.Info,

                    OldValues = System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            Status = oldStatus,
                            SuspensionReason = oldSuspensionReason
                        }
                    ),

                    NewValues = System.Text.Json.JsonSerializer.Serialize(
                        new
                        {
                            Status = accountStatus.ToString(),
                            SuspensionReason = newSuspensionReason
                        }
                    ),

                    Description = accountStatus == AccountStatus.Suspended
                        ? $"Recruiter account suspended. Reason: {newSuspensionReason}"
                        : "Recruiter account activated.",

                    IpAddress = ipAddress,

                    UserAgent = userAgent,

                    Success = true,

                    CreatedAt = DateTime.UtcNow
                };

                _db.AuditLogs.Add(auditLog);

                // -----------------------------------------
                // SAVE EVERYTHING
                // -----------------------------------------
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AdminRecruiterDetailDto?> GetRecruiterDetailAsync(
         Guid employerId)
        {
            var employer = await _db.EmployerProfiles
                .AsNoTracking()
                .Include(e => e.User)
                .Include(e => e.CreditWallet)
                .Include(e => e.VerificationDocuments)
                    .ThenInclude(d => d.DocumentType)
                .Include(e => e.Badges)
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            if (employer == null)
            {
                return null;
            }

            // --------------------------------------------------
            // MEMBERSHIP / PLAN
            // --------------------------------------------------

            var membership = await _db.EmployerPlanPurchase
                .AsNoTracking()
                .Where(p => p.EmployerId == employerId)
                .OrderByDescending(p => p.AssignedAt)
                .FirstOrDefaultAsync();

            // --------------------------------------------------
            // JOBS
            // --------------------------------------------------

            var jobs = await _db.JobPostings
                .AsNoTracking()
                .Where(j => j.EmployerId == employerId)
                .Select(j => new
                {
                    j.JobId,
                    j.JobStatus
                })
                .ToListAsync();

            var totalJobPosts = jobs.Count;

            var totalOpenJobs = jobs.Count(j =>
                j.JobStatus.ToString().Equals(
                    "Active",
                    StringComparison.OrdinalIgnoreCase));

            // --------------------------------------------------
            // DOCUMENTS
            // --------------------------------------------------

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var documents = employer.VerificationDocuments
                .Where(d => !d.IsDeleted)
                .OrderBy(d => d.DocumentType != null
                    ? d.DocumentType.DisplayOrder
                    : int.MaxValue)
                .ThenBy(d => d.UploadedAt)
                .Select(d =>
                {
                    var expired =
                        d.ExpiryDate.HasValue &&
                        d.ExpiryDate.Value < today;

                    var title =
                        d.DocumentType?.DocumentName
                        ?? d.CustomDocumentName
                        ?? d.DetectedDocumentType
                        ?? "Document";

                    var category =
                        d.DocumentType?.Category
                        ?? d.Category;

                    return new RecruiterDocumentDto
                    {
                        DocumentId = d.DocumentId,

                        Title = title,

                        SubTitle = category,

                        Status = d.Status.ToString(),

                        FileName = d.FileName,

                        FileUrl = d.FileUrl,

                        DocumentNumber = d.DocumentNumber,

                        IssuingAuthority = d.IssuingAuthority,

                        IssueDate = d.IssueDate,

                        ExpiryDate = d.ExpiryDate,

                        Expired = expired,

                        AiConfidenceScore = d.AiConfidenceScore,

                        DetectedDocumentType = d.DetectedDocumentType,

                        UploadedAt = d.UploadedAt,

                        VerifiedAt = d.VerifiedAt,

                        Remarks = d.Remarks
                    };
                })
                .ToList();

            // --------------------------------------------------
            // BADGES
            // --------------------------------------------------

            var badges = employer.Badges
                .Select(b => new RecruiterBadgeDto
                {
                    BadgeId = b.BadgeId,

                    // Dynamic badge name/type from database.
                    // No predefined badge names.
                    BadgeType = b.BadgeType?.ToString(),

                    BadgeStatus = b.BadgeStatus.ToString(),

                    RevocationReason = b.RevocationReason,

                    VerificationDocumentId = b.VerificationDocumentId,

                    IssuedAt = b.IssuedAt,

                    RevokedAt = b.RevokedAt,

                    // Use the dynamic BadgeType as the display label.
                    Label = b.BadgeType?.ToString() ?? "Verification Badge",

                    Active = b.BadgeStatus.ToString()
                        .Equals(
                            "Active",
                            StringComparison.OrdinalIgnoreCase)
                })
                .ToList();

            // --------------------------------------------------
            // ACCOUNT HEALTH
            // --------------------------------------------------

            var accountHealthIssues = documents
                .Where(d => d.Expired)
                .Select(d =>
                    $"{d.Title} documentation needs re-upload as the previous file has reached its expiry date.")
                .ToList();

            // --------------------------------------------------
            // TRANSACTIONS + INVOICES
            // --------------------------------------------------

            var transactions = await _db.PaymentTransactions
                .AsNoTracking()
                .Where(t => t.EmployerId == employerId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new RecruiterTransactionDto
                {
                    TransactionId = t.TransactionId,

                    Date = t.CreatedAt,

                    Description =
                        !string.IsNullOrWhiteSpace(t.PackType)
                            ? t.PackType
                            : t.TransactionType,

                    Type = t.TransactionType,

                    Amount = t.TotalAmountPaise / 100m,

                    Payment = t.PaymentMethod,

                    TransactionNumber =
                        t.RazorpayPaymentId
                        ?? t.StripePaymentIntentId
                        ?? t.RazorpayOrderId,

                    PaymentStatus = t.PaymentStatus,

                    InvoiceNumber = _db.Invoices
                        .Where(i => i.TransactionId == t.TransactionId)
                        .Select(i => i.InvoiceNumber)
                        .FirstOrDefault(),

                    InvoiceDate = _db.Invoices
                        .Where(i => i.TransactionId == t.TransactionId)
                        .Select(i => (DateOnly?)i.InvoiceDate)
                        .FirstOrDefault(),

                    InvoiceUrl = _db.Invoices
                        .Where(i => i.TransactionId == t.TransactionId)
                        .Select(i => i.InvoiceS3Url)
                        .FirstOrDefault()
                })
                .ToListAsync();

            // --------------------------------------------------
            // RETURN
            // --------------------------------------------------

            return new AdminRecruiterDetailDto
            {
                Id = employer.EmployerId.ToString(),

                Logo = employer.CompanyLogoUrl,

                Company = employer.CompanyDisplayName,

                AccountStatus = employer.AccountStatus.ToString(),

                Recruiter = new RecruiterInformationDto
                {
                    Name = employer.ContactPersonName,

                    Role = employer.Designation,

                    Email = string.IsNullOrWhiteSpace(
                        employer.ContactEmailPublic)
                            ? employer.User.Email
                            : employer.ContactEmailPublic
                },

                CompanyInformation = new RecruiterCompanyDto
                {
                    LegalName = employer.LegalName,

                    IndustryType = employer.IndustryType,

                    DisplayName = employer.CompanyDisplayName,

                    TotalEmployees = employer.TotalEmployees,

                    FoundedYear = employer.YearEstablished,

                    Address = BuildAddress(employer),

                    BusinessType = employer.BusinessType,

                    CompanySize = employer.CompanySize?.ToString(),

                    // EmployerProfile does not currently have
                    // a separate CompanyType property.
                    CompanyType = null,

                    Website = employer.WebsiteUrl
                },

                Membership = membership == null
                    ? null
                    : new RecruiterMembershipDto
                    {
                        PlanName = membership.PlanName,

                        Credits = membership.Credits,

                        Price = membership.Price,

                        AssignedAt = membership.AssignedAt,

                        ExpiresAt = membership.ExpiresAt,

                        IsActive = membership.IsActive
                    },

                Documents = documents,

                Badges = badges,

                QuickInsights = new RecruiterQuickInsightsDto
                {
                    RegisteredOn = employer.CreatedAt,

                    TotalOpenJobs = totalOpenJobs,

                    TotalJobPosts = totalJobPosts,

                    CurrentCredits =
                        employer.CreditWallet?.CreditBalance ?? 0
                },

                AccountHealth = new RecruiterAccountHealthDto
                {
                    ProfileCompletion =
                        employer.ProfileCompletionScore,

                    Issues = accountHealthIssues
                },

                PrimaryContact = new RecruiterPrimaryContactDto
                {
                    Name = employer.ContactPersonName,

                    Role = employer.Designation,

                    Email = string.IsNullOrWhiteSpace(
                        employer.ContactEmailPublic)
                            ? employer.User.Email
                            : employer.ContactEmailPublic
                },

                Transactions = transactions
            };
        }

        public async Task<AdminRecruiterDocumentsResponseDto?>
    GetRecruiterDocumentsAsync(Guid employerId)
        {
            // --------------------------------------------------
            // EMPLOYER
            // --------------------------------------------------

            var employer = await _db.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            if (employer == null)
            {
                return null;
            }

            // --------------------------------------------------
            // COMMON / ADMIN-CREATED DOCUMENT TYPES
            // --------------------------------------------------
            //
            // VerificationDocumentMaster contains the
            // admin-created/common document definitions.
            //
            // Only ACTIVE master documents participate in
            // verification calculation.
            //

            var commonDocumentTypes = await _db.VerificationDocumentMasters
                .AsNoTracking()
                .Where(d => d.IsActive)
                .Select(d => new
                {
                    d.DocumentTypeId,
                    d.Code,
                    d.DocumentName,
                    d.Category,
                    d.IsMandatory,
                    d.RequiresVerification,
                    d.Description
                })
                .ToListAsync();

            var commonDocumentTypeIds =
                commonDocumentTypes
                    .Select(d => d.DocumentTypeId)
                    .ToHashSet();

            // --------------------------------------------------
            // ALL RECRUITER DOCUMENTS
            // --------------------------------------------------
            //
            // IMPORTANT:
            // Do NOT filter by DocumentTypeId here.
            //
            // This means:
            // Common documents     -> included
            // Additional documents -> included
            //

            var documents = await _db.EmployerVerificationDocuments
                .AsNoTracking()
                .Where(d =>
                    d.EmployerId == employerId &&
                    !d.IsDeleted)
                .Include(d => d.DocumentType)
                .OrderBy(d => d.DocumentType != null
                    ? d.DocumentType.DisplayOrder
                    : int.MaxValue)
                .ThenBy(d => d.UploadedAt)
                .ToListAsync();

            // --------------------------------------------------
            // VERIFICATION CALCULATION
            // --------------------------------------------------
            //
            // ONLY documents linked to VerificationDocumentMaster
            // participate in verification.
            //
            // Additional documents are ignored here.
            //

            var commonDocuments = documents
                .Where(d =>
                    d.DocumentTypeId.HasValue &&
                    commonDocumentTypeIds.Contains(d.DocumentTypeId.Value))
                .ToList();

            var verificationTotal = commonDocumentTypeIds.Count;

            var verificationVerified = commonDocumentTypeIds.Count(
                documentTypeId =>
                    commonDocuments.Any(d =>
                        d.DocumentTypeId == documentTypeId &&
                        d.Status ==
                            JobPortal.Domain.Enums.RecruiterEnums
                                .VerificationDocumentStatus.Approved)
            );

            var verificationRejected = commonDocumentTypeIds.Count(
                documentTypeId =>
                    commonDocuments.Any(d =>
                        d.DocumentTypeId == documentTypeId &&
                        d.Status ==
                            JobPortal.Domain.Enums.RecruiterEnums
                                .VerificationDocumentStatus.Rejected)
            );

            var verificationPending =
                Math.Max(
                    verificationTotal - verificationVerified,
                    0);

            var verificationPercentage =
                verificationTotal == 0
                    ? 0
                    : (int)Math.Round(
                        verificationVerified * 100.0 /
                        verificationTotal);

            string verificationStatus;

            if (verificationTotal == 0)
            {
                verificationStatus = "Pending";
            }
            else if (verificationVerified == verificationTotal)
            {
                // IMPORTANT:
                // Even if an additional document is rejected,
                // this remains Verified.
                verificationStatus = "Verified";
            }
            else if (verificationRejected > 0)
            {
                verificationStatus = "Rejected";
            }
            else
            {
                verificationStatus = "Pending";
            }

            // --------------------------------------------------
            // DOCUMENT DTO
            // --------------------------------------------------

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var documentDtos = documents
                .Select(d =>
                {
                    var isCommonDocument =
                        d.DocumentTypeId.HasValue &&
                        commonDocumentTypeIds.Contains(
                            d.DocumentTypeId.Value);

                    var documentName =
                        d.DocumentType?.DocumentName
                        ?? d.CustomDocumentName
                        ?? d.DetectedDocumentType
                        ?? "Additional Document";

                    var category =
                        d.DocumentType?.Category
                        ?? d.Category;

                    return new AdminRecruiterDocumentDto
                    {
                        DocumentId = d.DocumentId,

                        DocumentTypeId = d.DocumentTypeId,

                        IsCommonDocument = isCommonDocument,

                        DocumentName = documentName,

                        Category = category,

                        DocumentNumber = d.DocumentNumber,

                        IssuingAuthority = d.IssuingAuthority,

                        IssueDate = d.IssueDate,

                        ExpiryDate = d.ExpiryDate,

                        IsExpired =
                            d.ExpiryDate.HasValue &&
                            d.ExpiryDate.Value < today,

                        FileName = d.FileName,

                        FileUrl = d.FileUrl,

                        PublicId = d.PublicId,

                        Status = d.Status.ToString(),

                        VerifiedBy = d.VerifiedBy,

                        UploadedAt = d.UploadedAt,

                        VerifiedAt = d.VerifiedAt,

                        Remarks = d.Remarks,

                        DetectedDocumentType =
                            d.DetectedDocumentType,

                        AiConfidenceScore =
                            d.AiConfidenceScore,

                        MasterDescription =
                            d.DocumentType?.Description,

                        RequiresVerification =
                            d.DocumentType?.RequiresVerification ?? false,

                        IsMandatory =
                            d.DocumentType?.IsMandatory ?? false
                    };
                })
                .ToList();

            // --------------------------------------------------
            // RESPONSE
            // --------------------------------------------------

            return new AdminRecruiterDocumentsResponseDto
            {
                EmployerId = employer.EmployerId,

                CompanyName = employer.CompanyDisplayName,

                CompanyLogoUrl = employer.CompanyLogoUrl,

                Gstin = employer.Gstin,

                RegisteredAt = employer.CreatedAt,

                City = employer.City,

                Country = employer.Country,

                Verification =
                    new RecruiterDocumentVerificationSummaryDto
                    {
                        Total = verificationTotal,

                        Verified = verificationVerified,

                        Pending = verificationPending,

                        Rejected = verificationRejected,

                        VerificationPercentage =
                            verificationPercentage,

                        Status = verificationStatus
                    },

                Documents = documentDtos
            };
        }

        private static string BuildAddress(EmployerProfile employer)
        {
            var parts = new[]
            {
        employer.AddressLine1,
        employer.AddressLine2,
        employer.City,
        employer.State,
        employer.Pincode,
        employer.Country
    };

            return string.Join(
                ", ",
                parts.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        public async Task<bool> UpdateRecruiterDocumentStatusAsync(
    Guid documentId,
    UpdateRecruiterDocumentStatusRequestDto request,
    AdminAuditContext audit)
        {
            var document = await _db.EmployerVerificationDocuments
                .Include(d => d.Employer)
                .FirstOrDefaultAsync(d =>
                    d.DocumentId == documentId &&
                    !d.IsDeleted);

            if (document == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(request.Status))
            {
                throw new ArgumentException(
                    "Document status is required.");
            }

            if (!Enum.TryParse<VerificationDocumentStatus>(
                request.Status,
                true,
                out var newStatus))
            {
                throw new ArgumentException(
                    $"Invalid document status: {request.Status}");
            }

            // Rejection and resubmission require remarks
            if ((newStatus == VerificationDocumentStatus.Rejected ||
                 newStatus == VerificationDocumentStatus.Resubmission) &&
                string.IsNullOrWhiteSpace(request.Remarks))
            {
                throw new ArgumentException(
                    "Remarks are required when rejecting or requesting resubmission.");
            }

            var oldStatus = document.Status.ToString();
            var oldRemarks = document.Remarks;

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                document.Status = newStatus;

                document.Remarks =
                    string.IsNullOrWhiteSpace(request.Remarks)
                        ? null
                        : request.Remarks.Trim();

                // -----------------------------------------
                // APPROVED
                // -----------------------------------------

                if (newStatus == VerificationDocumentStatus.Approved)
                {
                    document.VerifiedBy = audit.AdminId;
                    document.VerifiedAt = DateTime.UtcNow;
                }

                // -----------------------------------------
                // REJECTED / RESUBMISSION
                // -----------------------------------------

                else if (
                    newStatus == VerificationDocumentStatus.Rejected ||
                    newStatus == VerificationDocumentStatus.Resubmission)
                {
                    document.VerifiedBy = audit.AdminId;
                    document.VerifiedAt = DateTime.UtcNow;
                }

                // -----------------------------------------
                // AUDIT LOG
                // -----------------------------------------

                var action = newStatus switch
                {
                    VerificationDocumentStatus.Approved
                        => "Verify Document",

                    VerificationDocumentStatus.Rejected
                        => "Reject Document",

                    VerificationDocumentStatus.Resubmission
                        => "Request Document Resubmission",

                    _ => "Update Document Status"
                };

                var description = newStatus switch
                {
                    VerificationDocumentStatus.Approved
                        => "Recruiter verification document approved.",

                    VerificationDocumentStatus.Rejected
                        => $"Recruiter verification document rejected. Reason: {document.Remarks}",

                    VerificationDocumentStatus.Resubmission
                        => $"Recruiter verification document resubmission requested. Message: {document.Remarks}",

                    _ => "Recruiter verification document status updated."
                };

                var auditLog = new AuditLog
                {
                    LogId = Guid.NewGuid(),

                    PerformedByAdminId = audit.AdminId,

                    PerformedByName = audit.AdminName,

                    PerformedByRole = audit.AdminRole,

                    Module = "Recruiters",

                    Action = action,

                    TargetEntityType =
                        "EmployerVerificationDocument",

                    TargetEntityId = document.DocumentId,

                    TargetEntityName =
                        document.DocumentType?.DocumentName
                        ?? document.CustomDocumentName
                        ?? document.DetectedDocumentType
                        ?? document.FileName,

                    Severity =
                        newStatus == VerificationDocumentStatus.Rejected
                            ? AuditSeverity.Warning
                            : AuditSeverity.Info,

                    OldValues =
                        System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                Status = oldStatus,
                                Remarks = oldRemarks
                            }),

                    NewValues =
                        System.Text.Json.JsonSerializer.Serialize(
                            new
                            {
                                Status = newStatus.ToString(),
                                Remarks = document.Remarks
                            }),

                    Description = description,

                    IpAddress = audit.IpAddress,

                    UserAgent = audit.UserAgent,

                    Success = true,

                    CreatedAt = DateTime.UtcNow
                };

                _db.AuditLogs.Add(auditLog);

                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AdminRecruiterDocumentChecklistResponseDto?>
    GetRecruiterDocumentChecklistAsync(Guid employerId)
        {
            // --------------------------------------------------
            // CHECK RECRUITER
            // --------------------------------------------------

            var employerExists = await _db.EmployerProfiles
                .AsNoTracking()
                .AnyAsync(e => e.EmployerId == employerId);

            if (!employerExists)
            {
                return null;
            }

            // --------------------------------------------------
            // ADMIN-CREATED / COMMON DOCUMENT TYPES
            // --------------------------------------------------

            var documentMasters = await _db.VerificationDocumentMasters
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new
                {
                    d.DocumentTypeId,
                    d.Code,
                    d.DocumentName,
                    d.Category,
                    d.IsMandatory,
                    d.RequiresVerification
                })
                .ToListAsync();

            var commonDocumentTypeIds = documentMasters
                .Select(d => d.DocumentTypeId)
                .ToHashSet();

            // --------------------------------------------------
            // ALL RECRUITER DOCUMENTS
            // --------------------------------------------------
            //
            // IMPORTANT:
            // We intentionally DO NOT filter DocumentTypeId here.
            //
            // Therefore:
            // Common documents     -> included
            // Additional documents -> included
            //

            var employerDocuments = await _db.EmployerVerificationDocuments
                .AsNoTracking()
                .Where(d =>
                    d.EmployerId == employerId &&
                    !d.IsDeleted)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            // --------------------------------------------------
            // CHECKLIST
            // --------------------------------------------------

            var checklist =
                new List<AdminRecruiterDocumentChecklistDto>();

            // --------------------------------------------------
            // COMMON DOCUMENTS
            // --------------------------------------------------
            //
            // One checklist item per master document type.
            //
            // If multiple files exist for the same common
            // document type, use the most relevant upload.
            //

            foreach (var master in documentMasters)
            {
                var uploadedDocuments = employerDocuments
                    .Where(d =>
                        d.DocumentTypeId == master.DocumentTypeId)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToList();

                EmployerVerificationDocument? selectedDocument = null;

                if (uploadedDocuments.Any())
                {
                    // Prefer approved document if available.
                    selectedDocument = uploadedDocuments
                        .FirstOrDefault(d =>
                            d.Status ==
                            VerificationDocumentStatus.Approved);

                    // Otherwise latest upload.
                    selectedDocument ??=
                        uploadedDocuments.First();
                }

                checklist.Add(
                    new AdminRecruiterDocumentChecklistDto
                    {
                        DocumentTypeId =
                            master.DocumentTypeId,

                        Code =
                            master.Code,

                        DocumentName =
                            master.DocumentName,

                        Category =
                            master.Category,

                        IsCommonDocument = true,

                        IsMandatory =
                            master.IsMandatory,

                        RequiresVerification =
                            master.RequiresVerification,

                        Status =
                            selectedDocument == null
                                ? "Pending"
                                : selectedDocument.Status.ToString(),

                        DocumentId =
                            selectedDocument?.DocumentId
                            ?? Guid.Empty,

                        Remarks =
                            selectedDocument?.Remarks,

                        UploadedAt =
                            selectedDocument?.UploadedAt
                            ?? default,

                        VerifiedAt =
                            selectedDocument?.VerifiedAt
                    });
            }

            // --------------------------------------------------
            // ADDITIONAL DOCUMENTS
            // --------------------------------------------------
            //
            // Additional documents have no matching
            // VerificationDocumentMaster record.
            //
            // They ARE displayed in checklist.
            // They DO NOT participate in verification.
            //

            var additionalDocuments = employerDocuments
                .Where(d =>
                    !d.DocumentTypeId.HasValue ||
                    !commonDocumentTypeIds.Contains(
                        d.DocumentTypeId.Value))
                .ToList();

            foreach (var document in additionalDocuments)
            {
                var documentName =
                    document.CustomDocumentName
                    ?? document.DetectedDocumentType
                    ?? "Additional Document";

                checklist.Add(
                    new AdminRecruiterDocumentChecklistDto
                    {
                        DocumentTypeId =
                            document.DocumentTypeId,

                        Code = null,

                        DocumentName =
                            documentName,

                        Category =
                            document.Category,

                        IsCommonDocument = false,

                        IsMandatory = false,

                        RequiresVerification = false,

                        Status =
                            document.Status.ToString(),

                        DocumentId =
                            document.DocumentId,

                        Remarks =
                            document.Remarks,

                        UploadedAt =
                            document.UploadedAt,

                        VerifiedAt =
                            document.VerifiedAt
                    });
            }

            // --------------------------------------------------
            // VERIFICATION CALCULATION
            // --------------------------------------------------
            //
            // ONLY COMMON DOCUMENTS.
            //
            // Additional documents are completely ignored here.
            //

            var commonChecklist = checklist
                .Where(d => d.IsCommonDocument)
                .ToList();

            var verificationTotal =
                commonChecklist.Count;

            var verified =
                commonChecklist.Count(d =>
                    d.Status.Equals(
                        VerificationDocumentStatus.Approved.ToString(),
                        StringComparison.OrdinalIgnoreCase));

            var rejected =
                commonChecklist.Count(d =>
                    d.Status.Equals(
                        VerificationDocumentStatus.Rejected.ToString(),
                        StringComparison.OrdinalIgnoreCase));

            var pending =
                verificationTotal - verified - rejected;

            if (pending < 0)
            {
                pending = 0;
            }

            var verificationPercentage =
                verificationTotal == 0
                    ? 0
                    : (int)Math.Round(
                        verified * 100.0 /
                        verificationTotal);

            string verificationStatus;

            if (verificationTotal == 0)
            {
                verificationStatus = "Pending";
            }
            else if (verified == verificationTotal)
            {
                verificationStatus = "Verified";
            }
            else if (rejected > 0)
            {
                verificationStatus = "Rejected";
            }
            else
            {
                verificationStatus = "Pending";
            }

            // --------------------------------------------------
            // RESPONSE
            // --------------------------------------------------

            return new AdminRecruiterDocumentChecklistResponseDto
            {
                EmployerId = employerId,

                // All checklist items:
                // common + additional
                Total = checklist.Count,

                // Only common documents
                VerificationTotal = verificationTotal,

                Verified = verified,

                Pending = pending,

                Rejected = rejected,

                VerificationPercentage =
                    verificationPercentage,

                VerificationStatus =
                    verificationStatus,

                Documents = checklist
            };
        }

    }
}
