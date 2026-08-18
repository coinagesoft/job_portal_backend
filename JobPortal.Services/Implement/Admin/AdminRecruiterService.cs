using JobPortal.Application.DTOs;
using JobPortal.Application.DTOs.Admin;
using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using System.Globalization;


namespace JobPortal.Services.Implement.Admin
{
    public class AdminRecruiterService : IAdminRecruiterService
    {
        private readonly AppDbContext _db; 

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

                

                    // KEEP "Gst" because frontend already expects "gst".
                    // This now represents overall common-document verification.
                    verificationStatus = overallVerificationStatus,

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
            // ===========================================================
            // CHECK EMPLOYER
            // ===========================================================

            var employer = await _db.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.EmployerId == employerId);

            if (employer == null)
            {
                return null;
            }


            // ===========================================================
            // LOAD ALL ACTIVE MASTER DOCUMENT TYPES
            // ===========================================================
            //
            // Mandatory + Optional
            //
            // We need all master documents for metadata,
            // but ONLY mandatory documents are used
            // for verification summary.
            //
            // ===========================================================

            var masterDocumentTypes = await _db
                .VerificationDocumentMasters
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new
                {
                    d.DocumentTypeId,
                    d.DocumentName,
                    d.Category,
                    d.IsMandatory,
                    d.RequiresVerification,
                    d.Description,
                    d.DisplayOrder
                })
                .ToListAsync();


            // ===========================================================
            // MANDATORY MASTER DOCUMENT TYPES
            // ===========================================================

            var mandatoryDocumentTypes =
                masterDocumentTypes
                    .Where(d => d.IsMandatory)
                    .ToList();

            var mandatoryDocumentTypeIds =
                mandatoryDocumentTypes
                    .Select(d => d.DocumentTypeId)
                    .ToHashSet();


            // ===========================================================
            // LOAD ALL UPLOADED DOCUMENTS
            // ===========================================================
            //
            // Mandatory
            // Optional
            // Additional
            // RequestedAdditional
            //
            // Only actual uploaded documents are returned.
            //
            // Missing mandatory documents are counted separately
            // as NotUploaded.
            //
            // ===========================================================

            var documents = await _db
                .EmployerVerificationDocuments
                .AsNoTracking()
                .Include(d => d.DocumentType)
                .Where(d =>
                    d.EmployerId == employerId &&
                    !d.IsDeleted)
                .OrderBy(d =>
                    d.DocumentType != null
                        ? d.DocumentType.DisplayOrder
                        : int.MaxValue)
                .ThenByDescending(d => d.UploadedAt)
                .ToListAsync();


            // ===========================================================
            // MANDATORY UPLOADED DOCUMENTS
            // ===========================================================

            var mandatoryUploadedDocuments =
                documents
                    .Where(d =>
                        d.DocumentTypeId.HasValue &&
                        mandatoryDocumentTypeIds.Contains(
                            d.DocumentTypeId.Value))
                    .ToList();


            // ===========================================================
            // TOTAL MANDATORY DOCUMENTS
            // ===========================================================

            var verificationTotal =
                mandatoryDocumentTypes.Count;


            // ===========================================================
            // CURRENT DOCUMENT FOR EACH MANDATORY TYPE
            // ===========================================================
            //
            // There should only be one active document per document type
            // because re-upload replaces the old document.
            //
            // We still use the latest UploadedAt as a safety check.
            //
            // ===========================================================

            var latestMandatoryDocuments =
                mandatoryDocumentTypes
                    .Select(master =>
                        mandatoryUploadedDocuments
                            .Where(doc =>
                                doc.DocumentTypeId ==
                                master.DocumentTypeId)
                            .OrderByDescending(doc =>
                                doc.UploadedAt)
                            .FirstOrDefault())
                    .ToList();


            // ===========================================================
            // VERIFIED
            // ===========================================================

            var verificationVerified =
                latestMandatoryDocuments.Count(doc =>
                    doc != null &&
                    doc.Status ==
                        VerificationDocumentStatus.Approved);


            // ===========================================================
            // REJECTED
            // ===========================================================

            var verificationRejected =
                latestMandatoryDocuments.Count(doc =>
                    doc != null &&
                    doc.Status ==
                        VerificationDocumentStatus.Rejected);


            // ===========================================================
            // UPLOADED MANDATORY DOCUMENT COUNT
            // ===========================================================

            var uploadedMandatoryCount =
                latestMandatoryDocuments.Count(doc =>
                    doc != null);


            // ===========================================================
            // NOT UPLOADED
            // ===========================================================
            //
            // No uploaded document = NotUploaded.
            //
            // Pending is NOT NotUploaded.
            //
            // ===========================================================

            var verificationNotUploaded =
                latestMandatoryDocuments.Count(doc =>
                    doc == null);


            // ===========================================================
            // PENDING
            // ===========================================================
            //
            // Uploaded mandatory documents which are neither
            // Approved nor Rejected.
            //
            // Includes:
            //
            // Pending
            // Resubmission
            // Expired
            //
            // ===========================================================

            var verificationPending =
                latestMandatoryDocuments.Count(doc =>
                    doc != null &&
                    doc.Status !=
                        VerificationDocumentStatus.Approved &&
                    doc.Status !=
                        VerificationDocumentStatus.Rejected);


            // ===========================================================
            // OVERALL VERIFICATION STATUS
            // ===========================================================

            string verificationStatus;

            if (verificationTotal == 0)
            {
                verificationStatus = "Pending";
            }
            else if (verificationRejected > 0)
            {
                verificationStatus = "Rejected";
            }
            else if (verificationVerified ==
                     verificationTotal)
            {
                verificationStatus = "Verified";
            }
            else
            {
                verificationStatus = "Pending";
            }


            // ===========================================================
            // TODAY
            // ===========================================================

            var today =
                DateOnly.FromDateTime(
                    DateTime.UtcNow);


            // ===========================================================
            // DOCUMENT DTOs
            // ===========================================================
            //
            // ONLY UPLOADED DOCUMENTS ARE RETURNED.
            //
            // Each document gets its OWN AI extraction percentage.
            //
            // Example:
            //
            // GST       -> 98%
            // PAN       -> 91%
            // Factory   -> 76%
            //
            // ===========================================================

            var documentDtos =
                documents
                    .Select(d =>
                    {
                        // -------------------------------------------------
                        // DOCUMENT NAME
                        // -------------------------------------------------

                        var documentName =
                            d.DocumentType?.DocumentName
                            ?? d.CustomDocumentName
                            ?? d.DetectedDocumentType
                            ?? "Additional Document";


                        // -------------------------------------------------
                        // DOCUMENT CATEGORY
                        // -------------------------------------------------
                        //
                        // RequestedAdditional
                        // Mandatory
                        // Optional
                        // Additional
                        //
                        // -------------------------------------------------

                        string documentCategory;

                        if (d.RequestId.HasValue)
                        {
                            documentCategory =
                                "RequestedAdditional";
                        }
                        else if (d.DocumentType != null)
                        {
                            documentCategory =
                                d.DocumentType.IsMandatory
                                    ? "Mandatory"
                                    : "Optional";
                        }
                        else
                        {
                            documentCategory =
                                "Additional";
                        }


                        // -------------------------------------------------
                        // BUSINESS CATEGORY
                        // -------------------------------------------------
                        //
                        // Master document:
                        //     VerificationDocumentMaster.Category
                        //
                        // Additional/custom:
                        //     EmployerVerificationDocument.Category
                        //
                        // Examples:
                        //
                        // Tax
                        // Licence
                        // Registration
                        // Other
                        //
                        // -------------------------------------------------

                        var category =
                            d.DocumentType?.Category
                            ?? d.Category;


                        // -------------------------------------------------
                        // EXPIRED
                        // -------------------------------------------------

                        var isExpired =
                            d.ExpiryDate.HasValue &&
                            d.ExpiryDate.Value < today;


                        // -------------------------------------------------
                        // AI EXTRACTION PERCENTAGE
                        // -------------------------------------------------
                        //
                        // Gemini score may be:
                        //
                        // 0.98 -> 98
                        // 0.85 -> 85
                        //
                        // Or already:
                        //
                        // 98 -> 98
                        // 85 -> 85
                        //
                        // Each document is calculated independently.
                        //
                        // -------------------------------------------------

                        decimal? aiExtractionPercentage = null;

                        if (d.AiConfidenceScore.HasValue)
                        {
                            var score =
                                d.AiConfidenceScore.Value;

                            if (score >= 0m &&
                                score <= 1m)
                            {
                                score *= 100m;
                            }

                            aiExtractionPercentage =
                                Math.Round(
                                    Math.Clamp(
                                        score,
                                        0m,
                                        100m),
                                    2);
                        }


                        // -------------------------------------------------
                        // DTO
                        // -------------------------------------------------

                        return new AdminRecruiterDocumentDto
                        {
                            DocumentId =
                                d.DocumentId,

                            DocumentTypeId =
                                d.DocumentTypeId,

                            RequestId =
                                d.RequestId,

                            DocumentName =
                                documentName,

                            // Business category
                            // Tax / Licence / Registration / Other
                            Category =
                                category,

                            // Document classification
                            // Mandatory / Optional /
                            // Additional / RequestedAdditional
                            DocumentCategory =
                                documentCategory,

                            DocumentNumber =
                                d.DocumentNumber,

                            IssuingAuthority =
                                d.IssuingAuthority,

                            IssueDate =
                                d.IssueDate,

                            ExpiryDate =
                                d.ExpiryDate,

                            IsExpired =
                                isExpired,

                            FileName =
                                d.FileName,

                            FileUrl =
                                d.FileUrl,

                            PublicId =
                                d.PublicId,

                            Status =
                                d.Status.ToString(),

                            VerifiedBy =
                                d.VerifiedBy,

                            UploadedAt =
                                d.UploadedAt,

                            VerifiedAt =
                                d.VerifiedAt,

                            Remarks =
                                d.Remarks,

                            DetectedDocumentType =
                                d.DetectedDocumentType,

                            // DOCUMENT-WISE AI PERCENTAGE
                            AiExtractionPercentage =
                                aiExtractionPercentage,

                            RequiresVerification =
                                d.DocumentType?.RequiresVerification
                                ?? d.RequestId.HasValue,

                            IsMandatory =
                                d.DocumentType?.IsMandatory
                                ?? false
                        };
                    })
                    .ToList();


            // ===========================================================
            // RESPONSE
            // ===========================================================
            //
            // IMPORTANT:
            //
            // There is NO overall AI extraction percentage anymore.
            //
            // AI percentage exists only inside each document.
            //
            // ===========================================================

            return new AdminRecruiterDocumentsResponseDto
            {
                EmployerId =
                    employer.EmployerId,

                CompanyName =
                    employer.CompanyDisplayName,

                CompanyLogoUrl =
                    employer.CompanyLogoUrl,

                Gstin =
                    employer.Gstin,

                RegisteredAt =
                    employer.CreatedAt,

                City =
                    employer.City,

                Country =
                    employer.Country,

                Verification =
                    new RecruiterDocumentVerificationSummaryDto
                    {
                        // Mandatory document count
                        Total =
                            verificationTotal,

                        // Mandatory documents approved
                        Verified =
                            verificationVerified,

                        // Uploaded mandatory documents waiting
                        // for verification
                        Pending =
                            verificationPending,

                        // Mandatory documents with no upload
                        NotUploaded =
                            verificationNotUploaded,

                        // Mandatory documents rejected
                        Rejected =
                            verificationRejected,

                        // Overall verification status only.
                        // NO AI percentage here.
                        Status =
                            verificationStatus
                    },

                Documents =
                    documentDtos
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

        public async Task<bool> UpdateRecruiterDocumentStatusAsync(Guid documentId,UpdateRecruiterDocumentStatusRequestDto request,
        AdminAuditContext audit)
        {
            // --------------------------------------------------
            // VALIDATE REQUEST
            // --------------------------------------------------

            if (request == null)
            {
                throw new ArgumentException(
                    "Document status request is required.");
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

            // Rejected and Resubmission require remarks
            if ((newStatus == VerificationDocumentStatus.Rejected ||
                 newStatus == VerificationDocumentStatus.Resubmission) &&
                string.IsNullOrWhiteSpace(request.Remarks))
            {
                throw new ArgumentException(
                    "Remarks are required when rejecting or requesting resubmission.");
            }

            // --------------------------------------------------
            // GET DOCUMENT
            // --------------------------------------------------

            var document = await _db.EmployerVerificationDocuments
                .Include(d => d.Employer)
                .Include(d => d.DocumentType)
                .FirstOrDefaultAsync(d =>
                    d.DocumentId == documentId &&
                    !d.IsDeleted);

            if (document == null)
            {
                return false;
            }

            // --------------------------------------------------
            // GET ADMIN
            // --------------------------------------------------
            //
            // IMPORTANT:
            // audit.AdminId MUST be AdminUser.AdminId
            // because AuditLogs.PerformedByAdminId has FK
            // to AdminUsers.AdminId.
            //

            var admin = await _db.AdminUsers
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a =>
                    a.AdminId == audit.AdminId);

            if (admin == null)
            {
                throw new ArgumentException(
                    "Admin user not found.");
            }

            // --------------------------------------------------
            // OLD VALUES
            // --------------------------------------------------

            var oldStatus = document.Status.ToString();
            var oldRemarks = document.Remarks;

            var newRemarks =
                string.IsNullOrWhiteSpace(request.Remarks)
                    ? null
                    : request.Remarks.Trim();

            // --------------------------------------------------
            // DOCUMENT NAME
            // --------------------------------------------------

            var documentName =
                document.DocumentType?.DocumentName
                ?? document.CustomDocumentName
                ?? document.DetectedDocumentType
                ?? document.FileName;

            // --------------------------------------------------
            // TRANSACTION
            // --------------------------------------------------

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            try
            {
                // --------------------------------------------------
                // UPDATE DOCUMENT STATUS
                // --------------------------------------------------

                document.Status = newStatus;

                document.Remarks = newRemarks;

                // Approved / Rejected / Resubmission
                // are all admin-reviewed states.
                if (newStatus == VerificationDocumentStatus.Approved ||
                    newStatus == VerificationDocumentStatus.Rejected ||
                    newStatus == VerificationDocumentStatus.Resubmission)
                {
                    document.VerifiedBy = admin.AdminId;
                    document.VerifiedAt = DateTime.UtcNow;
                }

                // --------------------------------------------------
                // GET EXISTING BADGE
                // --------------------------------------------------

                var badge = await _db.EmployerBadges
                    .FirstOrDefaultAsync(b =>
                        b.VerificationDocumentId == documentId);

                // ==================================================
                // APPROVED
                // ==================================================

                if (newStatus ==
                    VerificationDocumentStatus.Approved)
                {
                    if (badge == null)
                    {
                        badge = new EmployerBadge
                        {
                            BadgeId = Guid.NewGuid(),

                            EmployerId = document.EmployerId,

                            // Dynamic badge
                            BadgeType = null,

                            VerificationDocumentId =
                                documentId,

                            BadgeStatus =
                                BadgeStatus.Approved,

                            IssuedBy =
                                admin.AdminId,

                            IssuedAt =
                                DateTime.UtcNow,

                            RevocationReason = null,

                            RevokedAt = null
                        };

                        _db.EmployerBadges.Add(badge);
                    }
                    else
                    {
                        badge.BadgeStatus =
                            BadgeStatus.Approved;

                        badge.RevocationReason = null;

                        badge.RevokedAt = null;

                        badge.IssuedBy =
                            admin.AdminId;

                        badge.IssuedAt =
                            DateTime.UtcNow;

                        // Keep badge dynamic
                        badge.BadgeType = null;
                    }
                }

                // ==================================================
                // REJECTED
                // ==================================================

                else if (newStatus ==
                         VerificationDocumentStatus.Rejected)
                {
                    if (badge == null)
                    {
                        // Create a badge record so the rejected
                        // document has a badge/status history.
                        badge = new EmployerBadge
                        {
                            BadgeId = Guid.NewGuid(),

                            EmployerId =
                                document.EmployerId,

                            BadgeType = null,

                            VerificationDocumentId =
                                documentId,

                            BadgeStatus =
                                BadgeStatus.Revoked,

                            IssuedBy =
                                admin.AdminId,

                            IssuedAt =
                                DateTime.UtcNow,

                            RevokedAt =
                                DateTime.UtcNow,

                            RevocationReason =
                                newRemarks
                        };

                        _db.EmployerBadges.Add(badge);
                    }
                    else
                    {
                        badge.BadgeStatus =
                            BadgeStatus.Revoked;

                        badge.RevokedAt =
                            DateTime.UtcNow;

                        badge.RevocationReason =
                            newRemarks;
                    }
                }

                // ==================================================
                // RESUBMISSION
                // ==================================================
                //
                // IMPORTANT:
                // Resubmission gets its own badge status.
                // It is NOT changed to Revoked.
                //

                else if (newStatus ==
                         VerificationDocumentStatus.Resubmission)
                {
                    if (badge == null)
                    {
                        badge = new EmployerBadge
                        {
                            BadgeId = Guid.NewGuid(),

                            EmployerId =
                                document.EmployerId,

                            // Dynamic badge
                            BadgeType = null,

                            VerificationDocumentId =
                                documentId,

                            BadgeStatus =
                                BadgeStatus.Resubmission,

                            IssuedBy =
                                admin.AdminId,

                            IssuedAt =
                                DateTime.UtcNow,

                            RevocationReason = null,

                            RevokedAt = null
                        };

                        _db.EmployerBadges.Add(badge);
                    }
                    else
                    {
                        badge.BadgeStatus =
                            BadgeStatus.Resubmission;

                        badge.RevocationReason = null;

                        badge.RevokedAt = null;

                        badge.IssuedBy =
                            admin.AdminId;

                        badge.IssuedAt =
                            DateTime.UtcNow;

                        // Keep badge dynamic
                        badge.BadgeType = null;
                    }
                }

                // ==================================================
                // AUDIT ACTION
                // ==================================================

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

                // ==================================================
                // AUDIT SEVERITY
                // ==================================================

                var severity =
                    newStatus == VerificationDocumentStatus.Rejected
                        ? AuditSeverity.Warning
                        : newStatus ==
                          VerificationDocumentStatus.Resubmission
                            ? AuditSeverity.Warning
                            : AuditSeverity.Info;

                // ==================================================
                // AUDIT DESCRIPTION
                // ==================================================

                var description = newStatus switch
                {
                    VerificationDocumentStatus.Approved =>
                        $"Recruiter verification document approved: " +
                        $"{documentName}.",

                    VerificationDocumentStatus.Rejected =>
                        $"Recruiter verification document rejected: " +
                        $"{documentName}. " +
                        $"Reason: {newRemarks}",

                    VerificationDocumentStatus.Resubmission =>
                        $"Recruiter verification document resubmission " +
                        $"requested: {documentName}. " +
                        $"Message: {newRemarks}",

                    _ =>
                        $"Recruiter verification document status updated: " +
                        $"{documentName}."
                };

                // ==================================================
                // AUDIT LOG
                // ==================================================

                var auditLog = new AuditLog
                {
                    LogId = Guid.NewGuid(),

                    // IMPORTANT:
                    // This must be AdminUsers.AdminId
                    PerformedByAdminId =
                        admin.AdminId,

                    PerformedByName =
                        admin.FullName,

                    PerformedByRole =
                        admin.Role?.RoleName
                        ?? admin.AdminType,

                    Module =
                        "Recruiters",

                    Action =
                        action,

                    TargetEntityType =
                        "EmployerVerificationDocument",

                    TargetEntityId =
                        document.DocumentId,

                    TargetEntityName =
                        documentName,

                    Severity =
                        severity,

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
                                Status =
                                    newStatus.ToString(),

                                Remarks =
                                    newRemarks,

                                BadgeStatus =
                                    newStatus ==
                                    VerificationDocumentStatus.Approved
                                        ? BadgeStatus.Approved.ToString()
                                        : newStatus ==
                                          VerificationDocumentStatus.Rejected
                                            ? BadgeStatus.Revoked.ToString()
                                            : newStatus ==
                                              VerificationDocumentStatus.Resubmission
                                                ? BadgeStatus.Resubmission.ToString()
                                                : null
                            }),

                    Description =
                        description,

                    IpAddress =
                        audit.IpAddress,

                    UserAgent =
                        audit.UserAgent,

                    Success =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

                _db.AuditLogs.Add(auditLog);

                // ==================================================
                // SAVE
                // ==================================================

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

        public async Task<AdminRecruiterDocumentChecklistResponseDto?> GetRecruiterDocumentChecklistAsync(Guid employerId)
        {
            // ==================================================
            // CHECK RECRUITER
            // ==================================================

            var employerExists = await _db.EmployerProfiles
                .AsNoTracking()
                .AnyAsync(e => e.EmployerId == employerId);

            if (!employerExists)
            {
                return null;
            }


            // ==================================================
            // GET ACTIVE DOCUMENT MASTER TYPES
            // ==================================================

            var documentMasters = await _db.VerificationDocumentMasters
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .Select(d => new
                {
                    d.DocumentTypeId,
                    d.Code,
                    d.DocumentName,

                    // Business category
                    // Example: Tax / License
                    d.Category,

                    d.IsMandatory,
                    d.RequiresVerification
                })
                .ToListAsync();


            // ==================================================
            // GET ALL RECRUITER UPLOADED DOCUMENTS
            // ==================================================

            var employerDocuments = await _db.EmployerVerificationDocuments
                .AsNoTracking()
                .Where(d =>
                    d.EmployerId == employerId &&
                    !d.IsDeleted)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();


            // ==================================================
            // GET ADMIN DOCUMENT REQUESTS
            // ==================================================
            //
            // These requests can exist even before upload.
            //
            // Requested document is identified using RequestId.
            //
            // ==================================================

            var documentRequests = await _db.EmployerDocumentRequests
                .AsNoTracking()
                .Where(r =>
                    r.EmployerId == employerId &&
                    r.Status != "Cancelled")
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();


            // ==================================================
            // CHECKLIST
            // ==================================================

            var checklist =
                new List<AdminRecruiterDocumentChecklistDto>();


            // ==================================================
            // 1. MASTER DOCUMENTS
            // ==================================================
            //
            // Mandatory:
            //     Always displayed
            //
            // Optional:
            //     Displayed only when uploaded
            //
            // If an admin specifically requested an optional
            // master document:
            //
            //     DocumentCategory = RequestedAdditional
            //
            // ==================================================

            foreach (var master in documentMasters)
            {
                // --------------------------------------------------
                // FIND LATEST ADMIN REQUEST FOR THIS MASTER
                // --------------------------------------------------

                var matchingRequest = documentRequests
                    .Where(r =>
                        r.DocumentTypeId.HasValue &&
                        r.DocumentTypeId.Value ==
                            master.DocumentTypeId)
                    .OrderByDescending(r => r.RequestedAt)
                    .FirstOrDefault();


                // --------------------------------------------------
                // FIND UPLOADED DOCUMENTS FOR THIS TYPE
                // --------------------------------------------------

                var uploadedDocuments = employerDocuments
                    .Where(d =>
                        d.DocumentTypeId.HasValue &&
                        d.DocumentTypeId.Value ==
                            master.DocumentTypeId)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToList();


                EmployerVerificationDocument? selectedDocument = null;


                // --------------------------------------------------
                // IF REQUESTED, FIRST FIND UPLOAD FOR THAT REQUEST
                // --------------------------------------------------

                if (matchingRequest != null)
                {
                    selectedDocument = uploadedDocuments
                        .FirstOrDefault(d =>
                            d.RequestId.HasValue &&
                            d.RequestId.Value ==
                                matchingRequest.RequestId);
                }


                // --------------------------------------------------
                // OTHERWISE PREFER APPROVED DOCUMENT
                // --------------------------------------------------

                selectedDocument ??=
                    uploadedDocuments
                        .FirstOrDefault(d =>
                            d.Status ==
                            VerificationDocumentStatus.Approved);


                // --------------------------------------------------
                // OTHERWISE USE LATEST UPLOAD
                // --------------------------------------------------

                selectedDocument ??=
                    uploadedDocuments.FirstOrDefault();


                // --------------------------------------------------
                // DOCUMENT CATEGORY
                // --------------------------------------------------
                //
                // This tells frontend HOW the document is being used.
                //
                // Mandatory
                // Optional
                // RequestedAdditional
                //
                // --------------------------------------------------

                string documentCategory;

                if (matchingRequest != null)
                {
                    documentCategory =
                        "RequestedAdditional";
                }
                else if (master.IsMandatory)
                {
                    documentCategory =
                        "Mandatory";
                }
                else
                {
                    documentCategory =
                        "Optional";
                }


                // --------------------------------------------------
                // STATUS
                // --------------------------------------------------
                //
                // No upload:
                //     NotUploaded
                //
                // Upload exists:
                //     Pending
                //     Approved
                //     Rejected
                //     Expired
                //     Resubmission
                //
                // --------------------------------------------------

                var status =
                    selectedDocument == null
                        ? "NotUploaded"
                        : selectedDocument.Status.ToString();


                // --------------------------------------------------
                // REQUIRES VERIFICATION
                // --------------------------------------------------
                //
                // Requested documents always require verification.
                //
                // Otherwise master configuration decides.
                //
                // --------------------------------------------------

                var requiresVerification =
                    matchingRequest != null
                        ? true
                        : master.RequiresVerification;


                // --------------------------------------------------
                // MESSAGE
                // --------------------------------------------------
                //
                // Only requested documents receive a message.
                //
                // Mandatory = null
                // Optional = null
                // --------------------------------------------------

                var message =
                    matchingRequest != null
                        ? matchingRequest.Message
                        : null;


                // --------------------------------------------------
                // ADD TO CHECKLIST
                // --------------------------------------------------

                checklist.Add(
                    new AdminRecruiterDocumentChecklistDto
                    {
                        DocumentTypeId =
                            master.DocumentTypeId,

                        DocumentName =
                            master.DocumentName,

                        // Business category
                        // Example: Tax / License
                        Category =
                            master.Category,

                        // Mandatory / Optional /
                        // RequestedAdditional
                        DocumentCategory =
                            documentCategory,

                        IsMandatory =
                            master.IsMandatory,

                        RequiresVerification =
                            requiresVerification,

                        Status =
                            status,

                        // Only requested document
                        Message =
                            message,

                        DocumentId =
                            selectedDocument?.DocumentId
                            ?? Guid.Empty,

                        UploadedAt =
                            selectedDocument?.UploadedAt
                            ?? default,

                        VerifiedAt =
                            selectedDocument?.VerifiedAt
                    });
            }


            // ==================================================
            // 2. REQUESTED CUSTOM DOCUMENTS
            // ==================================================
            //
            // Admin selected "Other".
            //
            // DocumentTypeId = null
            // CustomDocumentName = requested name
            //
            // These documents exist in EmployerDocumentRequests.
            //
            // ==================================================

            var customRequests = documentRequests
                .Where(r =>
                    !r.DocumentTypeId.HasValue &&
                    !string.IsNullOrWhiteSpace(
                        r.CustomDocumentName))
                .ToList();


            foreach (var request in customRequests)
            {
                var requestedName =
                    request.CustomDocumentName!.Trim();


                // --------------------------------------------------
                // FIND UPLOAD FOR EXACT REQUEST
                // --------------------------------------------------
                //
                // IMPORTANT:
                // Match by RequestId.
                //
                // Do NOT match by document name.
                //
                // --------------------------------------------------

                var selectedDocument =
                    employerDocuments
                        .Where(d =>
                            d.RequestId.HasValue &&
                            d.RequestId.Value ==
                                request.RequestId)
                        .OrderByDescending(d => d.UploadedAt)
                        .FirstOrDefault();


                // --------------------------------------------------
                // STATUS
                // --------------------------------------------------

                var status =
                    selectedDocument == null
                        ? "NotUploaded"
                        : selectedDocument.Status.ToString();


                // --------------------------------------------------
                // ADD REQUESTED CUSTOM DOCUMENT
                // --------------------------------------------------

                checklist.Add(
                    new AdminRecruiterDocumentChecklistDto
                    {
                        DocumentTypeId =
                            null,

                        DocumentName =
                            requestedName,

                        // No VerificationDocumentMaster exists
                        // for custom requested documents.
                        Category =
                            "Other",

                        DocumentCategory =
                            "RequestedAdditional",

                        IsMandatory =
                            false,

                        RequiresVerification =
                            true,

                        Status =
                            status,

                        // ONLY requested document gets Message
                        Message =
                            request.Message,

                        DocumentId =
                            selectedDocument?.DocumentId
                            ?? Guid.Empty,

                        UploadedAt =
                            selectedDocument?.UploadedAt
                            ?? default,

                        VerifiedAt =
                            selectedDocument?.VerifiedAt
                    });
            }


            // ==================================================
            // 3. NORMAL ADDITIONAL DOCUMENTS
            // ==================================================
            //
            // These are directly uploaded by recruiter.
            //
            // They do NOT come from EmployerDocumentRequests.
            //
            // Identification:
            //
            // RequestId = null
            // Category  = Additional
            //
            // ==================================================

            var additionalDocuments = employerDocuments
                .Where(d =>
                    !d.RequestId.HasValue &&
                    string.Equals(
                        d.Category,
                        "Additional",
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.UploadedAt)
                .ToList();


            foreach (var document in additionalDocuments)
            {
                var documentName =
                    document.CustomDocumentName
                    ?? document.DetectedDocumentType
                    ?? document.FileName
                    ?? "Additional Document";


                checklist.Add(
                    new AdminRecruiterDocumentChecklistDto
                    {
                        DocumentTypeId =
                            document.DocumentTypeId,

                        DocumentName =
                            documentName,

                        // Additional document does not have
                        // VerificationDocumentMaster category.
                        Category =
                            document.Category,

                        DocumentCategory =
                            "Additional",

                        IsMandatory =
                            false,

                        // Normal additional documents do not
                        // participate in verification.
                        RequiresVerification =
                            false,

                        Status =
                            document.Status.ToString(),

                        // Never show request message
                        Message =
                            null,

                        DocumentId =
                            document.DocumentId,

                        UploadedAt =
                            document.UploadedAt,

                        VerifiedAt =
                            document.VerifiedAt
                    });
            }


            // ==================================================
            // 4. VERIFICATION CALCULATION
            // ==================================================
            //
            // Verification is based on:
            //
            //     Mandatory
            //     Optional where RequiresVerification = true
            //     RequestedAdditional
            //
            // Normal Additional is excluded.
            //
            // ==================================================

            var verificationChecklist =
                checklist
                    .Where(d => d.RequiresVerification)
                    .ToList();


            // ==================================================
            // TOTAL REQUIRING VERIFICATION
            // ==================================================

            var verificationTotal =
                verificationChecklist.Count;


            // ==================================================
            // VERIFIED
            // ==================================================

            var verified =
                verificationChecklist.Count(d =>
                    d.Status.Equals(
                        VerificationDocumentStatus.Approved.ToString(),
                        StringComparison.OrdinalIgnoreCase));


            // ==================================================
            // REJECTED
            // ==================================================

            var rejected =
                verificationChecklist.Count(d =>
                    d.Status.Equals(
                        VerificationDocumentStatus.Rejected.ToString(),
                        StringComparison.OrdinalIgnoreCase));


            // ==================================================
            // NOT UPLOADED
            // ==================================================
            //
            // IMPORTANT:
            //
            // Only verification-required documents.
            //
            // Pending is NOT NotUploaded.
            //
            // Example:
            //
            // No file -> NotUploaded
            // File uploaded -> Pending
            // Approved -> Approved
            // Rejected -> Rejected
            //
            // ==================================================

            var notUploaded =
                verificationChecklist.Count(d =>
                    d.Status.Equals(
                        "NotUploaded",
                        StringComparison.OrdinalIgnoreCase));


            // ==================================================
            // PENDING
            // ==================================================

            var pending =
                verificationChecklist.Count(d =>
                    d.Status.Equals(
                        VerificationDocumentStatus.Pending.ToString(),
                        StringComparison.OrdinalIgnoreCase));


            // ==================================================
            // VERIFICATION STATUS
            // ==================================================

            string verificationStatus;

            if (verificationTotal == 0)
            {
                verificationStatus = "Pending";
            }
            else if (rejected > 0)
            {
                verificationStatus = "Rejected";
            }
            else if (verified == verificationTotal)
            {
                verificationStatus = "Verified";
            }
            else
            {
                verificationStatus = "Pending";
            }


            // ==================================================
            // RESPONSE
            // ==================================================

            return new AdminRecruiterDocumentChecklistResponseDto
            {
                EmployerId =
                    employerId,

                // Mandatory
                // Optional
                // Additional
                // RequestedAdditional
                Total =
                    checklist.Count,

                // Only RequiresVerification = true
                VerificationTotal =
                    verificationTotal,

                Verified =
                    verified,

                NotUploaded =
                    notUploaded,

                Rejected =
                    rejected,

                Pending =
                    pending,

                VerificationStatus =
                    verificationStatus,

                Documents =
                    checklist
            };
        }

        public async Task<DocumentTypeAdminDto?> CreateOptionalDocumentTypeAsync(CreateOptionalDocumentTypeRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentException(
                    "Document type request is required.");
            }

            if (string.IsNullOrWhiteSpace(request.DocumentName))
            {
                throw new ArgumentException(
                    "Document name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Category))
            {
                throw new ArgumentException(
                    "Category is required.");
            }

            var documentName = request.DocumentName.Trim();

            // -----------------------------------------
            // CHECK DUPLICATE
            // -----------------------------------------

            var exists = await _db.VerificationDocumentMasters
                .AnyAsync(x =>
                    x.DocumentName.ToLower() ==
                    documentName.ToLower());

            if (exists)
            {
                throw new ArgumentException(
                    "Document type already exists.");
            }

            // -----------------------------------------
            // DISPLAY ORDER
            // -----------------------------------------

            var maxDisplayOrder =
                await _db.VerificationDocumentMasters
                    .MaxAsync(x => (int?)x.DisplayOrder)
                ?? 0;

            // -----------------------------------------
            // CREATE OPTIONAL DOCUMENT TYPE
            // -----------------------------------------

            var entity = new VerificationDocumentMaster
            {
                DocumentTypeId = Guid.NewGuid(),

                Code = Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpper(),

                DocumentName = documentName,

                Category = request.Category.Trim(),

                // IMPORTANT:
                // This API ALWAYS creates NON-MANDATORY document
                IsMandatory = false,

                // Admin can decide whether verification
                // is required for this optional document.
                RequiresVerification = false,

                IsActive = true,

                AllowMultipleUploads = false,

                AllowCustomDocument = true,

                IsSystemDocument = true,

                DisplayOrder = maxDisplayOrder + 1,

                CreatedAt = DateTime.UtcNow
            };

            _db.VerificationDocumentMasters.Add(entity);

            await _db.SaveChangesAsync();

            return Map(entity);
        }

        public async Task<DocumentTypeAdminDto?> UpdateDocumentRequirementAsync(Guid documentTypeId, UpdateDocumentRequirementRequestDto request)
        {
            if (request == null)
            {
                throw new ArgumentException(
                    "Document requirement request is required.");
            }

            var documentType = await _db.VerificationDocumentMasters
                .FirstOrDefaultAsync(x =>
                    x.DocumentTypeId == documentTypeId);

            if (documentType == null)
            {
                return null;
            }

            documentType.IsMandatory = request.IsMandatory;
            documentType.RequiresVerification = request.IsMandatory;
            documentType.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Map(documentType);
        }

        public async Task<List<AdminDocumentRequirementDto>> GetDocumentRequirementsAsync()
        {
            var documents = await _db.VerificationDocumentMasters
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.IsSystemDocument)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new AdminDocumentRequirementDto
                {
                    Id = x.DocumentTypeId,

                    DocumentName = x.DocumentName,

                    Category = x.Category,

                    IsMandatory = x.IsMandatory,

                    RequiresVerification = x.RequiresVerification,

                    IsActive = x.IsActive,

                    DisplayOrder = x.DisplayOrder
                })
                .ToListAsync();

            return documents;
        }

        public async Task<List<OptionalDocumentTypeDto>> GetOptionalDocumentNamesAsync()
        {
            return await _db.VerificationDocumentMasters
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.IsMandatory == false)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new OptionalDocumentTypeDto
                {
                    DocumentTypeId = x.DocumentTypeId,
                    DocumentName = x.DocumentName
                })
                .ToListAsync();
        }

        public async Task<EmployerDocumentRequestDto> RequestRecruiterDocumentAsync(Guid employerId, RequestRecruiterDocumentDto request, Guid adminId)
        {
            if (request == null)
            {
                throw new ArgumentException(
                    "Document request is required.");
            }

            // --------------------------------------------------
            // CHECK RECRUITER
            // --------------------------------------------------

            var employerExists = await _db.EmployerProfiles
                .AnyAsync(x => x.EmployerId == employerId);

            if (!employerExists)
            {
                throw new ArgumentException(
                    "Recruiter not found.");
            }

            // --------------------------------------------------
            // CHECK ADMIN
            // --------------------------------------------------

            var adminExists = await _db.AdminUsers
                .AnyAsync(x =>
                    x.AdminId == adminId &&
                    x.IsActive);

            if (!adminExists)
            {
                throw new ArgumentException(
                    "Admin user not found or inactive.");
            }

            // ==================================================
            // EXISTING OPTIONAL DOCUMENT
            // ==================================================

            if (request.DocumentTypeId.HasValue)
            {
                var documentType =
                    await _db.VerificationDocumentMasters
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.DocumentTypeId ==
                                request.DocumentTypeId.Value &&
                            x.IsActive);

                if (documentType == null)
                {
                    throw new ArgumentException(
                        "Document type not found or inactive.");
                }

                // Only optional documents can be requested.
                if (documentType.IsMandatory)
                {
                    throw new ArgumentException(
                        "Mandatory documents cannot be requested.");
                }

                // Check whether the recruiter already has
                // a pending request for this document.
                var alreadyRequested =
                    await _db.EmployerDocumentRequests
                        .AnyAsync(x =>
                            x.EmployerId == employerId &&
                            x.DocumentTypeId ==
                                request.DocumentTypeId.Value &&
                            x.Status == "Pending");

                if (alreadyRequested)
                {
                    throw new ArgumentException(
                        "This document has already been requested.");
                }

                var entity = new EmployerDocumentRequest
                {
                    RequestId = Guid.NewGuid(),

                    EmployerId = employerId,

                    DocumentTypeId =
                        documentType.DocumentTypeId,

                    CustomDocumentName = null,

                    Message =
                        string.IsNullOrWhiteSpace(request.Message)
                            ? null
                            : request.Message.Trim(),

                    Status = "Pending",

                    RequestedBy = adminId,

                    RequestedAt = DateTime.UtcNow
                };

                _db.EmployerDocumentRequests.Add(entity);

                await _db.SaveChangesAsync();

                return new EmployerDocumentRequestDto
                {
                    RequestId = entity.RequestId,

                    EmployerId = entity.EmployerId,

                    DocumentTypeId = entity.DocumentTypeId,

                    CustomDocumentName = null,

                    DocumentName = documentType.DocumentName,

                    Message = entity.Message,

                    Status = entity.Status,

                    RequestedAt = entity.RequestedAt
                };
            }

            // ==================================================
            // CUSTOM DOCUMENT / OTHER
            // ==================================================

            if (string.IsNullOrWhiteSpace(
                request.CustomDocumentName))
            {
                throw new ArgumentException(
                    "Custom document name is required.");
            }

            var customDocumentName =
                request.CustomDocumentName.Trim();

            // Check duplicate pending custom request
            // for this recruiter.
            var customAlreadyRequested =
                await _db.EmployerDocumentRequests
                    .AnyAsync(x =>
                        x.EmployerId == employerId &&
                        x.DocumentTypeId == null &&
                        x.CustomDocumentName != null &&
                        x.CustomDocumentName.ToLower() ==
                            customDocumentName.ToLower() &&
                        x.Status == "Pending");

            if (customAlreadyRequested)
            {
                throw new ArgumentException(
                    "This custom document has already been requested.");
            }

            var customEntity = new EmployerDocumentRequest
            {
                RequestId = Guid.NewGuid(),

                EmployerId = employerId,

                DocumentTypeId = null,

                CustomDocumentName = customDocumentName,

                Message =
                    string.IsNullOrWhiteSpace(request.Message)
                        ? null
                        : request.Message.Trim(),

                Status = "Pending",

                RequestedBy = adminId,

                RequestedAt = DateTime.UtcNow
            };

            _db.EmployerDocumentRequests.Add(customEntity);

            await _db.SaveChangesAsync();

            return new EmployerDocumentRequestDto
            {
                RequestId = customEntity.RequestId,

                EmployerId = customEntity.EmployerId,

                DocumentTypeId = null,

                CustomDocumentName =
                    customEntity.CustomDocumentName,

                DocumentName =
                    customEntity.CustomDocumentName,

                Message = customEntity.Message,

                Status = customEntity.Status,

                RequestedAt =
                    customEntity.RequestedAt
            };
        }

        private DocumentTypeAdminDto Map(VerificationDocumentMaster entity)
        {
            return new DocumentTypeAdminDto
            {
                Id = entity.DocumentTypeId,

                DocumentName = entity.DocumentName,

                Category = entity.Category,

                IsMandatory = entity.IsMandatory,

                IsActive = entity.IsActive,

                RequiresVerification = entity.RequiresVerification,

                AllowMultipleUploads = entity.AllowMultipleUploads,

                DisplayOrder = entity.DisplayOrder,

                Description = entity.Description
            };
        }
    }
}
