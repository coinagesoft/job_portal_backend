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
using System.IO;
using System.Linq;


namespace JobPortal.Services.Implement.Admin
{
    public class AdminRecruiterService : IAdminRecruiterService
    {
        private readonly AppDbContext _db;

        public AdminRecruiterService(AppDbContext db)
        {
            _db = db;
        }

       

        // ------------------------------------------------------------
        // TRANSACTION HISTORY (dedicated endpoint)
        // ------------------------------------------------------------
        // Backs the "Transaction History" table on the recruiter detail
        // page. Same query as the Transactions block inside
        // GetRecruiterDetailAsync above, exposed on its own so the
        // frontend doesn't have to fetch the entire recruiter profile
        // just to show this table.
        public async Task<List<RecruiterTransactionDto>?> GetRecruiterTransactionsAsync(
            Guid employerId)
        {
            var employerExists = await _db.EmployerProfiles
                .AsNoTracking()
                .AnyAsync(e => e.EmployerId == employerId);

            if (!employerExists)
            {
                return null;
            }

            var transactionRows = await _db.PaymentTransactions
                .AsNoTracking()
                .Where(t => t.EmployerId == employerId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.TransactionId,
                    t.CreatedAt,
                    t.PackType,
                    t.TransactionType,
                    t.TotalAmountPaise,
                    t.PaymentMethod,
                    t.RazorpayPaymentId,
                    t.StripePaymentIntentId,
                    t.RazorpayOrderId,
                    t.PaymentStatus,
                    InvoiceNumber = _db.Invoices
                        .Where(i => i.TransactionId == t.TransactionId)
                        .Select(i => i.InvoiceNumber)
                        .FirstOrDefault(),
                    InvoiceDate = _db.Invoices
                        .Where(i => i.TransactionId == t.TransactionId)
                        .Select(i => (DateOnly?)i.InvoiceDate)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return transactionRows
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

                    InvoiceNumber = t.InvoiceNumber,

                    InvoiceDate = t.InvoiceDate,

                    InvoiceUrl = t.InvoiceNumber != null
                        ? $"/api/admin/recruiters/{employerId}/transactions/{t.TransactionId}/invoice/download"
                        : null
                })
                .ToList();
        }

        // ------------------------------------------------------------
        // INVOICE PDF (generated on demand — mirrors
        // RecruiterInvoiceService.DownloadInvoicePdfAsync on the
        // employer/recruiter side, scoped here by admin instead of by
        // the recruiter's own JWT).
        // ------------------------------------------------------------
        public async Task<(byte[] Bytes, string FileName)?> DownloadRecruiterInvoicePdfAsync(
            Guid employerId,
            Guid transactionId)
        {
            var data = await (
                from invoice in _db.Invoices
                join transaction in _db.PaymentTransactions
                on invoice.TransactionId equals transaction.TransactionId
                where invoice.TransactionId == transactionId
                      && transaction.EmployerId == employerId
                select new { Invoice = invoice, Transaction = transaction }
            ).AsNoTracking().FirstOrDefaultAsync();

            if (data == null)
            {
                return null;
            }

            var employer = await _db.EmployerProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployerId == employerId);

            var contactEmail = !string.IsNullOrWhiteSpace(employer?.ContactEmailPublic)
                ? employer!.ContactEmailPublic
                : await _db.Users
                    .Where(u => u.UserId == data.Transaction.UserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();

            var bytes = BuildInvoicePdf(data.Invoice, data.Transaction, employer, contactEmail);
            var fileName = $"{data.Invoice.InvoiceNumber}.pdf";

            return (bytes, fileName);
        }

        private static byte[] BuildInvoicePdf(
            Invoice invoice,
            PaymentTransaction transaction,
            EmployerProfile? employer,
            string? contactEmail)
        {
            using var stream = new MemoryStream();
            using var writer = new iText.Kernel.Pdf.PdfWriter(stream);
            using var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
            var document = new iText.Layout.Document(pdf, iText.Kernel.Geom.PageSize.A4);
            document.SetMargins(36, 36, 36, 36);

            var titleFont = iText.Kernel.Font.PdfFontFactory.CreateFont(
                iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            var regularFont = iText.Kernel.Font.PdfFontFactory.CreateFont(
                iText.IO.Font.Constants.StandardFonts.HELVETICA);
            var brandColor = new iText.Kernel.Colors.DeviceRgb(230, 126, 24);

            // ── Header ──────────────────────────────────────────
            document.Add(
                new iText.Layout.Element.Paragraph("JobBox")
                    .SetFont(titleFont)
                    .SetFontSize(22)
                    .SetFontColor(brandColor)
                    .SetMarginBottom(0));

            document.Add(
                new iText.Layout.Element.Paragraph("TAX INVOICE")
                    .SetFont(titleFont)
                    .SetFontSize(13)
                    .SetMarginTop(2)
                    .SetMarginBottom(16));

            // ── Invoice meta ────────────────────────────────────
            var metaTable = new iText.Layout.Element.Table(
                    iText.Layout.Properties.UnitValue.CreatePercentArray(new float[] { 1, 1 }))
                .UseAllAvailableWidth()
                .SetMarginBottom(14);

            metaTable.AddCell(PlainCell($"Invoice No: {invoice.InvoiceNumber}", regularFont));
            metaTable.AddCell(PlainCell($"Invoice Date: {invoice.InvoiceDate:dd MMM yyyy}", regularFont));
            metaTable.AddCell(PlainCell(
                $"Payment Ref: {transaction.RazorpayPaymentId ?? transaction.RazorpayOrderId ?? "-"}",
                regularFont));
            metaTable.AddCell(PlainCell($"Status: {transaction.PaymentStatus}", regularFont));
            document.Add(metaTable);

            // ── Billed to ───────────────────────────────────────
            document.Add(new iText.Layout.Element.Paragraph("Billed To")
                .SetFont(titleFont).SetFontSize(11).SetMarginBottom(2));
            document.Add(new iText.Layout.Element.Paragraph(employer?.CompanyDisplayName ?? "-")
                .SetFont(regularFont).SetFontSize(10));

            if (employer != null)
            {
                var addressLine = string.Join(", ", new[]
                {
                    employer.AddressLine1,
                    employer.AddressLine2,
                    employer.City,
                    employer.State,
                    employer.Pincode,
                    employer.Country
                }.Where(part => !string.IsNullOrWhiteSpace(part)));

                if (!string.IsNullOrWhiteSpace(addressLine))
                {
                    document.Add(new iText.Layout.Element.Paragraph(addressLine)
                        .SetFont(regularFont).SetFontSize(9));
                }

                if (employer.GstRegistered && !string.IsNullOrWhiteSpace(employer.Gstin))
                {
                    document.Add(new iText.Layout.Element.Paragraph($"GSTIN: {employer.Gstin}")
                        .SetFont(regularFont).SetFontSize(9));
                }

                if (!string.IsNullOrWhiteSpace(employer.Pan))
                {
                    document.Add(new iText.Layout.Element.Paragraph($"PAN: {employer.Pan}")
                        .SetFont(regularFont).SetFontSize(9));
                }

                if (!string.IsNullOrWhiteSpace(employer.ContactPhone))
                {
                    document.Add(new iText.Layout.Element.Paragraph($"Phone: {employer.ContactPhone}")
                        .SetFont(regularFont).SetFontSize(9));
                }
            }

            if (!string.IsNullOrWhiteSpace(contactEmail))
            {
                document.Add(new iText.Layout.Element.Paragraph($"Email: {contactEmail}")
                    .SetFont(regularFont).SetFontSize(9));
            }

            // ── Line item ───────────────────────────────────────
            var itemsTable = new iText.Layout.Element.Table(
                    iText.Layout.Properties.UnitValue.CreatePercentArray(new float[] { 5, 2, 2 }))
                .UseAllAvailableWidth()
                .SetMarginTop(20);

            itemsTable.AddHeaderCell(HeaderCell("Description", titleFont));
            itemsTable.AddHeaderCell(HeaderCell("Validity", titleFont));
            itemsTable.AddHeaderCell(HeaderCell("Amount (Rs.)", titleFont));

            itemsTable.AddCell(BodyCell(
                $"{transaction.PackType ?? transaction.TransactionType ?? "Plan"} ({transaction.CreditQuantity ?? 0} credits)",
                regularFont));
            itemsTable.AddCell(BodyCell($"{transaction.ValidityMonths ?? 0} month(s)", regularFont));
            itemsTable.AddCell(BodyCell(invoice.InvoiceAmount.ToString("N2"), regularFont));

            document.Add(itemsTable);

            // ── Totals ──────────────────────────────────────────
            var totalsTable = new iText.Layout.Element.Table(
                    iText.Layout.Properties.UnitValue.CreatePercentArray(new float[] { 3, 1 }))
                .UseAllAvailableWidth()
                .SetMarginTop(10);

            totalsTable.AddCell(TotalsLabelCell("Subtotal", regularFont));
            totalsTable.AddCell(TotalsValueCell($"Rs. {invoice.InvoiceAmount:N2}", regularFont));

            totalsTable.AddCell(TotalsLabelCell("GST (18%)", regularFont));
            totalsTable.AddCell(TotalsValueCell($"Rs. {invoice.InvoiceGst:N2}", regularFont));

            totalsTable.AddCell(TotalsLabelCell("Total", titleFont));
            totalsTable.AddCell(TotalsValueCell($"Rs. {invoice.InvoiceTotal:N2}", titleFont));

            document.Add(totalsTable);

            document.Add(
                new iText.Layout.Element.Paragraph(
                        "\nThis is a system-generated invoice and does not require a signature.")
                    .SetFont(regularFont)
                    .SetFontSize(8)
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.GRAY)
                    .SetMarginTop(24));

            document.Close();
            return stream.ToArray();
        }

        private static iText.Layout.Element.Cell PlainCell(string text, iText.Kernel.Font.PdfFont font) =>
            new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(text).SetFont(font).SetFontSize(9))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER);

        private static iText.Layout.Element.Cell HeaderCell(string text, iText.Kernel.Font.PdfFont font) =>
            new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(text).SetFont(font).SetFontSize(9))
                .SetBackgroundColor(new iText.Kernel.Colors.DeviceRgb(245, 245, 245));

        private static iText.Layout.Element.Cell BodyCell(string text, iText.Kernel.Font.PdfFont font) =>
            new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(text).SetFont(font).SetFontSize(9));

        private static iText.Layout.Element.Cell TotalsLabelCell(string text, iText.Kernel.Font.PdfFont font) =>
            new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(text).SetFont(font).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);

        private static iText.Layout.Element.Cell TotalsValueCell(string text, iText.Kernel.Font.PdfFont font) =>
            new iText.Layout.Element.Cell()
                .Add(new iText.Layout.Element.Paragraph(text).SetFont(font).SetFontSize(10))
                .SetBorder(iText.Layout.Borders.Border.NO_BORDER)
                .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);

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

            // --------------------------------------------------
            // DOCUMENTS
            // --------------------------------------------------


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


                    // --------------------------------------------------
                    // AI EXTRACTION PERCENTAGE
                    // --------------------------------------------------
                    //
                    // Supports both:
                    //
                    // 0.98 -> 98
                    // 0.85 -> 85
                    //
                    // and:
                    //
                    // 98 -> 98
                    // 85 -> 85
                    //
                    // --------------------------------------------------

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


                    return new RecruiterDocumentDto
                    {
                        DocumentId =
                            d.DocumentId,

                        Title =
                            title,

                        SubTitle =
                            category,

                        Status =
                            d.Status.ToString(),

                        FileName =
                            d.FileName,

                        FileUrl =
                            d.FileUrl,

                        DocumentNumber =
                            d.DocumentNumber,

                        IssuingAuthority =
                            d.IssuingAuthority,

                        IssueDate =
                            d.IssueDate,

                        ExpiryDate =
                            d.ExpiryDate,

                        Expired =
                            expired,

                       

                        // New percentage
                        AiExtractionPercentage =
                            aiExtractionPercentage,

                        DetectedDocumentType =
                            d.DetectedDocumentType,

                        UploadedAt =
                            d.UploadedAt,

                        VerifiedAt =
                            d.VerifiedAt,

                        Remarks =
                            d.Remarks
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
            // Keep loading all master documents because they are
            // required for uploaded-document metadata.
            //
            // Verification summary uses:
            // - Mandatory documents
            // - Requested documents
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
            // IMPORTANT:
            // This list is still ONLY actual uploaded documents.
            //
            // It is NOT changed by the verification-progress logic.
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
            // LOAD COMPANY-SPECIFIC ADMIN REQUESTS
            // ===========================================================
            //
            // Includes:
            //
            // 1. Requested existing master documents
            // 2. Requested custom documents
            //
            // Cancelled requests are excluded.
            //
            // ===========================================================

            var documentRequests =
                await _db.EmployerDocumentRequests
                    .AsNoTracking()
                    .Where(r =>
                        r.EmployerId == employerId &&
                        r.Status != "Cancelled")
                    .OrderByDescending(r => r.RequestedAt)
                    .ToListAsync();


            // ===========================================================
            // REQUESTED MASTER DOCUMENT TYPE IDS
            // ===========================================================

            var requestedMasterDocumentTypeIds =
                documentRequests
                    .Where(r =>
                        r.DocumentTypeId.HasValue)
                    .Select(r =>
                        r.DocumentTypeId!.Value)
                    .ToHashSet();


            // ===========================================================
            // REQUESTED CUSTOM DOCUMENTS
            // ===========================================================
            //
            // DocumentTypeId == null means custom / "Other".
            //
            // ===========================================================

            var requestedCustomDocuments =
                documentRequests
                    .Where(r =>
                        !r.DocumentTypeId.HasValue &&
                        !string.IsNullOrWhiteSpace(
                            r.CustomDocumentName))
                    .ToList();


            // ===========================================================
            // TOTAL REQUIRED DOCUMENTS
            // ===========================================================
            //
            // Count:
            //
            // 1. ALL mandatory master documents
            // 2. Requested master documents
            // 3. Requested custom documents
            //
            // IMPORTANT:
            //
            // If a mandatory document is also requested,
            // it is counted ONLY ONCE.
            //
            // ===========================================================

            var additionalRequestedMasterCount =
                requestedMasterDocumentTypeIds
                    .Count(id =>
                        !mandatoryDocumentTypeIds.Contains(id));


            var requestedCustomCount =
                requestedCustomDocuments.Count;


            var verificationTotal =
                mandatoryDocumentTypes.Count +
                additionalRequestedMasterCount +
                requestedCustomCount;


            // ===========================================================
            // CURRENT DOCUMENT FOR EACH MANDATORY TYPE
            // ===========================================================
            //
            // Existing logic preserved:
            // latest uploaded document for each mandatory type.
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
            // VERIFIED MANDATORY DOCUMENTS
            // ===========================================================

            var verificationVerifiedMandatory =
                latestMandatoryDocuments.Count(doc =>
                    doc != null &&
                    doc.Status ==
                        VerificationDocumentStatus.Approved);


            // ===========================================================
            // VERIFIED REQUESTED MASTER DOCUMENTS
            // ===========================================================
            //
            // Exclude mandatory document types because those are already
            // counted in verificationVerifiedMandatory.
            //
            // Match requested documents using RequestId.
            //
            // ===========================================================

            var verificationVerifiedRequestedMaster =
                documentRequests
                    .Where(r =>
                        r.DocumentTypeId.HasValue &&
                        !mandatoryDocumentTypeIds.Contains(
                            r.DocumentTypeId.Value))
                    .Count(request =>
                        documents.Any(doc =>
                            doc.RequestId.HasValue &&
                            doc.RequestId.Value ==
                                request.RequestId &&
                            doc.Status ==
                                VerificationDocumentStatus.Approved));


            // ===========================================================
            // VERIFIED REQUESTED CUSTOM DOCUMENTS
            // ===========================================================

            var verificationVerifiedRequestedCustom =
                requestedCustomDocuments
                    .Count(request =>
                        documents.Any(doc =>
                            doc.RequestId.HasValue &&
                            doc.RequestId.Value ==
                                request.RequestId &&
                            doc.Status ==
                                VerificationDocumentStatus.Approved));


            // ===========================================================
            // TOTAL VERIFIED
            // ===========================================================

            var verificationVerified =
                verificationVerifiedMandatory +
                verificationVerifiedRequestedMaster +
                verificationVerifiedRequestedCustom;


            // ===========================================================
            // UPLOADED MANDATORY COUNT
            // ===========================================================

            var uploadedMandatoryCount =
                latestMandatoryDocuments.Count(doc =>
                    doc != null);


            // ===========================================================
            // UPLOADED REQUESTED MASTER COUNT
            // ===========================================================
            //
            // Mandatory types are excluded because they are already
            // included in uploadedMandatoryCount.
            //
            // ===========================================================

            var uploadedRequestedMasterCount =
                documentRequests
                    .Where(r =>
                        r.DocumentTypeId.HasValue &&
                        !mandatoryDocumentTypeIds.Contains(
                            r.DocumentTypeId.Value))
                    .Count(request =>
                        documents.Any(doc =>
                            doc.RequestId.HasValue &&
                            doc.RequestId.Value ==
                                request.RequestId));


            // ===========================================================
            // UPLOADED REQUESTED CUSTOM COUNT
            // ===========================================================

            var uploadedRequestedCustomCount =
                requestedCustomDocuments
                    .Count(request =>
                        documents.Any(doc =>
                            doc.RequestId.HasValue &&
                            doc.RequestId.Value ==
                                request.RequestId));


            // ===========================================================
            // TOTAL UPLOADED REQUIRED DOCUMENTS
            // ===========================================================

            var uploadedRequiredDocuments =
                uploadedMandatoryCount +
                uploadedRequestedMasterCount +
                uploadedRequestedCustomCount;


            // ===========================================================
            // NOT UPLOADED
            // ===========================================================
            //
            // Required documents that don't have an upload.
            //
            // ===========================================================

            var verificationNotUploaded =
                verificationTotal -
                uploadedRequiredDocuments;


            // ===========================================================
            // REJECTED MANDATORY DOCUMENTS
            // ===========================================================

            var verificationRejectedMandatory =
                latestMandatoryDocuments.Count(doc =>
                    doc != null &&
                    doc.Status ==
                        VerificationDocumentStatus.Rejected);


            // ===========================================================
            // REJECTED REQUESTED MASTER DOCUMENTS
            // ===========================================================

            var verificationRejectedRequestedMaster =
                documentRequests
                    .Where(r =>
                        r.DocumentTypeId.HasValue &&
                        !mandatoryDocumentTypeIds.Contains(
                            r.DocumentTypeId.Value))
                    .Count(request =>
                        documents.Any(doc =>
                            doc.RequestId.HasValue &&
                            doc.RequestId.Value ==
                                request.RequestId &&
                            doc.Status ==
                                VerificationDocumentStatus.Rejected));


            // ===========================================================
            // REJECTED REQUESTED CUSTOM DOCUMENTS
            // ===========================================================

            var verificationRejectedRequestedCustom =
                requestedCustomDocuments
                    .Count(request =>
                        documents.Any(doc =>
                            doc.RequestId.HasValue &&
                            doc.RequestId.Value ==
                                request.RequestId &&
                            doc.Status ==
                                VerificationDocumentStatus.Rejected));


            // ===========================================================
            // TOTAL REJECTED
            // ===========================================================

            var verificationRejected =
                verificationRejectedMandatory +
                verificationRejectedRequestedMaster +
                verificationRejectedRequestedCustom;


            // ===========================================================
            // PENDING
            // ===========================================================
            //
            // Uploaded required documents which are neither Approved
            // nor Rejected.
            //
            // NotUploaded is kept separate.
            //
            // ===========================================================

            var verificationPending =
                verificationTotal -
                verificationVerified -
                verificationRejected -
                verificationNotUploaded;


            // ===========================================================
            // OVERALL VERIFICATION PROGRESS
            // ===========================================================
            //
            // Example:
            //
            // Total    = 6
            // Verified = 4
            //
            // Progress = 4 / 6 * 100
            //          = 66.67%
            //
            // ===========================================================

            var verificationProgress =
                verificationTotal == 0
                    ? 0m
                    : Math.Round(
                        (decimal)verificationVerified /
                        verificationTotal *
                        100m,
                        2);


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
            // IMPORTANT:
            //
            // KEEP OLD LOGIC.
            //
            // ONLY UPLOADED DOCUMENTS ARE RETURNED HERE.
            //
            // Required but not uploaded documents are NOT added
            // to this list.
            //
            // They are represented only in Verification summary.
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
                        // ==================================================
                        // TOTAL REQUIRED
                        // ==================================================
                        //
                        // Mandatory + Requested
                        //
                        // Includes documents not uploaded yet.
                        //
                        Total =
                            verificationTotal,


                        // ==================================================
                        // VERIFIED
                        // ==================================================
                        //
                        // Approved mandatory + approved requested.
                        //
                        Verified =
                            verificationVerified,


                        // ==================================================
                        // PENDING
                        // ==================================================

                        Pending =
                            verificationPending,


                        // ==================================================
                        // NOT UPLOADED
                        // ==================================================

                        NotUploaded =
                            verificationNotUploaded,


                        // ==================================================
                        // REJECTED
                        // ==================================================

                        Rejected =
                            verificationRejected,


                        // ==================================================
                        // OVERALL VERIFICATION PROGRESS
                        // ==================================================
                        //
                        // Example:
                        // 4 verified / 6 required = 66.67
                        //
                        VerificationProgress =
                            verificationProgress,


                        // ==================================================
                        // STATUS
                        // ==================================================

                        Status =
                            verificationStatus
                    },

                // ==========================================================
                // ONLY ACTUAL UPLOADED DOCUMENTS
                // ==========================================================

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

        public async Task<bool> UpdateRecruiterDocumentStatusAsync(Guid documentId, UpdateRecruiterDocumentStatusRequestDto request,
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
                // IMPORTANT:
                // ONLY RETURN UPLOADED DOCUMENTS
                // --------------------------------------------------

                if (selectedDocument == null)
                {
                    continue;
                }


                // --------------------------------------------------
                // DOCUMENT CATEGORY
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

                var status =
                    selectedDocument.Status.ToString();


                // --------------------------------------------------
                // REQUIRES VERIFICATION
                // --------------------------------------------------

                var requiresVerification =
                    matchingRequest != null
                        ? true
                        : master.RequiresVerification;


                // --------------------------------------------------
                // MESSAGE
                // --------------------------------------------------

                var message =
                    matchingRequest != null
                        ? matchingRequest.Message
                        : null;


                // --------------------------------------------------
                // ADD UPLOADED DOCUMENT
                // --------------------------------------------------

                checklist.Add(
                    new AdminRecruiterDocumentChecklistDto
                    {
                        DocumentTypeId =
                            master.DocumentTypeId,

                        DocumentName =
                            master.DocumentName,

                        Category =
                            master.Category,

                        DocumentCategory =
                            documentCategory,

                        IsMandatory =
                            master.IsMandatory,

                        RequiresVerification =
                            requiresVerification,

                        Status =
                            status,

                        Message =
                            message,

                        DocumentId =
                            selectedDocument.DocumentId,

                        RequestId =
                            selectedDocument.RequestId,

                        UploadedAt =
                            selectedDocument.UploadedAt,

                        VerifiedAt =
                            selectedDocument.VerifiedAt
                    });
            }


            // ==================================================
            // 2. REQUESTED CUSTOM DOCUMENTS
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

                var selectedDocument =
                    employerDocuments
                        .Where(d =>
                            d.RequestId.HasValue &&
                            d.RequestId.Value ==
                                request.RequestId)
                        .OrderByDescending(d => d.UploadedAt)
                        .FirstOrDefault();


                // --------------------------------------------------
                // ONLY RETURN UPLOADED DOCUMENT
                // --------------------------------------------------

                if (selectedDocument == null)
                {
                    continue;
                }


                // --------------------------------------------------
                // STATUS
                // --------------------------------------------------

                var status =
                    selectedDocument.Status.ToString();


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

                        Message =
                            request.Message,

                        DocumentId =
                            selectedDocument.DocumentId,

                        RequestId =
                            selectedDocument.RequestId,

                        UploadedAt =
                            selectedDocument.UploadedAt,

                        VerifiedAt =
                            selectedDocument.VerifiedAt
                    });
            }


            // ==================================================
            // 3. NORMAL ADDITIONAL DOCUMENTS
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


                // --------------------------------------------------
                // STATUS
                // --------------------------------------------------

                var status =
                    document.Status.ToString();


                // --------------------------------------------------
                // ADDITIONAL DOCUMENT
                // --------------------------------------------------

                checklist.Add(
                    new AdminRecruiterDocumentChecklistDto
                    {
                        DocumentTypeId =
                            document.DocumentTypeId,

                        DocumentName =
                            documentName,

                        Category =
                            document.Category,

                        DocumentCategory =
                            "Additional",

                        IsMandatory =
                            false,

                        RequiresVerification =
                            false,

                        Status =
                            status,

                        Message =
                            null,

                        DocumentId =
                            document.DocumentId,

                        RequestId =
                            document.RequestId,

                        UploadedAt =
                            document.UploadedAt,

                        VerifiedAt =
                            document.VerifiedAt
                    });
            }


            // ==================================================
            // 4. VERIFICATION CALCULATION
            // ==================================================

            var verificationChecklist =
                checklist
                    .Where(d =>
                        d.RequiresVerification)
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
            // Since checklist now contains uploaded documents only,
            // this will normally be 0.
            //
            // Kept here so the existing response structure remains
            // compatible.
            // ==================================================

            var notUploaded = 0;


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
            // OVERALL VERIFICATION PROGRESS
            // ==================================================
            //
            // Example:
            //
            // Total requiring verification = 5
            // Approved = 3
            //
            // Progress = 3 / 5 * 100 = 60%
            //
            // Always between 0 and 100.
            // ==================================================

            var verificationProgress =
                verificationTotal == 0
                    ? 0
                    : Math.Clamp(
                        (int)Math.Round(
                            (double)verified /
                            verificationTotal *
                            100),
                        0,
                        100);


            // ==================================================
            // RESPONSE
            // ==================================================

            return new AdminRecruiterDocumentChecklistResponseDto
            {
                EmployerId =
                    employerId,

                // Uploaded documents only
                Total =
                    checklist.Count,

                // Only documents requiring verification
                VerificationTotal =
                    verificationTotal,

                // Approved documents
                Verified =
                    verified,

                // No-upload records are excluded
                NotUploaded =
                    notUploaded,

                Rejected =
                    rejected,

                Pending =
                    pending,

                VerificationStatus =
                    verificationStatus,

                // OVERALL PROGRESS
                VerificationProgress =
                    verificationProgress,

                Documents =
                    checklist
            };
        }

        //public async Task<AdminRecruiterDocumentChecklistResponseDto?> GetRecruiterDocumentChecklistAsync(Guid employerId)
        //{
        //    // ==================================================
        //    // CHECK RECRUITER
        //    // ==================================================

        //    var employerExists = await _db.EmployerProfiles
        //        .AsNoTracking()
        //        .AnyAsync(e => e.EmployerId == employerId);

        //    if (!employerExists)
        //    {
        //        return null;
        //    }


        //    // ==================================================
        //    // GET ACTIVE DOCUMENT MASTER TYPES
        //    // ==================================================

        //    var documentMasters = await _db.VerificationDocumentMasters
        //        .AsNoTracking()
        //        .Where(d => d.IsActive)
        //        .OrderBy(d => d.DisplayOrder)
        //        .Select(d => new
        //        {
        //            d.DocumentTypeId,
        //            d.Code,
        //            d.DocumentName,

        //            // Business category
        //            // Example: Tax / License
        //            d.Category,

        //            d.IsMandatory,
        //            d.RequiresVerification
        //        })
        //        .ToListAsync();


        //    // ==================================================
        //    // GET ALL RECRUITER UPLOADED DOCUMENTS
        //    // ==================================================

        //    var employerDocuments = await _db.EmployerVerificationDocuments
        //        .AsNoTracking()
        //        .Where(d =>
        //            d.EmployerId == employerId &&
        //            !d.IsDeleted)
        //        .OrderByDescending(d => d.UploadedAt)
        //        .ToListAsync();


        //    // ==================================================
        //    // GET ADMIN DOCUMENT REQUESTS
        //    // ==================================================
        //    //
        //    // These requests can exist even before upload.
        //    //
        //    // Requested document is identified using RequestId.
        //    //
        //    // ==================================================

        //    var documentRequests = await _db.EmployerDocumentRequests
        //        .AsNoTracking()
        //        .Where(r =>
        //            r.EmployerId == employerId &&
        //            r.Status != "Cancelled")
        //        .OrderByDescending(r => r.RequestedAt)
        //        .ToListAsync();


        //    // ==================================================
        //    // CHECKLIST
        //    // ==================================================

        //    var checklist =
        //        new List<AdminRecruiterDocumentChecklistDto>();


        //    // ==================================================
        //    // 1. MASTER DOCUMENTS
        //    // ==================================================
        //    //
        //    // Mandatory:
        //    //     Always displayed
        //    //
        //    // Optional:
        //    //     Displayed only when uploaded
        //    //
        //    // If an admin specifically requested an optional
        //    // master document:
        //    //
        //    //     DocumentCategory = RequestedAdditional
        //    //
        //    // ==================================================

        //    foreach (var master in documentMasters)
        //    {
        //        // --------------------------------------------------
        //        // FIND LATEST ADMIN REQUEST FOR THIS MASTER
        //        // --------------------------------------------------

        //        var matchingRequest = documentRequests
        //            .Where(r =>
        //                r.DocumentTypeId.HasValue &&
        //                r.DocumentTypeId.Value ==
        //                    master.DocumentTypeId)
        //            .OrderByDescending(r => r.RequestedAt)
        //            .FirstOrDefault();


        //        // --------------------------------------------------
        //        // FIND UPLOADED DOCUMENTS FOR THIS TYPE
        //        // --------------------------------------------------

        //        var uploadedDocuments = employerDocuments
        //            .Where(d =>
        //                d.DocumentTypeId.HasValue &&
        //                d.DocumentTypeId.Value ==
        //                    master.DocumentTypeId)
        //            .OrderByDescending(d => d.UploadedAt)
        //            .ToList();


        //        EmployerVerificationDocument? selectedDocument = null;


        //        // --------------------------------------------------
        //        // IF REQUESTED, FIRST FIND UPLOAD FOR THAT REQUEST
        //        // --------------------------------------------------

        //        if (matchingRequest != null)
        //        {
        //            selectedDocument = uploadedDocuments
        //                .FirstOrDefault(d =>
        //                    d.RequestId.HasValue &&
        //                    d.RequestId.Value ==
        //                        matchingRequest.RequestId);
        //        }


        //        // --------------------------------------------------
        //        // OTHERWISE PREFER APPROVED DOCUMENT
        //        // --------------------------------------------------

        //        selectedDocument ??=
        //            uploadedDocuments
        //                .FirstOrDefault(d =>
        //                    d.Status ==
        //                    VerificationDocumentStatus.Approved);


        //        // --------------------------------------------------
        //        // OTHERWISE USE LATEST UPLOAD
        //        // --------------------------------------------------

        //        selectedDocument ??=
        //            uploadedDocuments.FirstOrDefault();


        //        // --------------------------------------------------
        //        // DOCUMENT CATEGORY
        //        // --------------------------------------------------
        //        //
        //        // This tells frontend HOW the document is being used.
        //        //
        //        // Mandatory
        //        // Optional
        //        // RequestedAdditional
        //        //
        //        // --------------------------------------------------

        //        string documentCategory;

        //        if (matchingRequest != null)
        //        {
        //            documentCategory =
        //                "RequestedAdditional";
        //        }
        //        else if (master.IsMandatory)
        //        {
        //            documentCategory =
        //                "Mandatory";
        //        }
        //        else
        //        {
        //            documentCategory =
        //                "Optional";
        //        }


        //        // --------------------------------------------------
        //        // STATUS
        //        // --------------------------------------------------
        //        //
        //        // No upload:
        //        //     NotUploaded
        //        //
        //        // Upload exists:
        //        //     Pending
        //        //     Approved
        //        //     Rejected
        //        //     Expired
        //        //     Resubmission
        //        //
        //        // --------------------------------------------------

        //        var status =
        //            selectedDocument == null
        //                ? "NotUploaded"
        //                : selectedDocument.Status.ToString();


        //        // --------------------------------------------------
        //        // REQUIRES VERIFICATION
        //        // --------------------------------------------------
        //        //
        //        // Requested documents always require verification.
        //        //
        //        // Otherwise master configuration decides.
        //        //
        //        // --------------------------------------------------

        //        var requiresVerification =
        //            matchingRequest != null
        //                ? true
        //                : master.RequiresVerification;


        //        // --------------------------------------------------
        //        // MESSAGE
        //        // --------------------------------------------------
        //        //
        //        // Only requested documents receive a message.
        //        //
        //        // Mandatory = null
        //        // Optional = null
        //        // --------------------------------------------------

        //        var message =
        //            matchingRequest != null
        //                ? matchingRequest.Message
        //                : null;


        //        // --------------------------------------------------
        //        // ADD TO CHECKLIST
        //        // --------------------------------------------------

        //        checklist.Add(
        //            new AdminRecruiterDocumentChecklistDto
        //            {
        //                DocumentTypeId =
        //                    master.DocumentTypeId,

        //                DocumentName =
        //                    master.DocumentName,

        //                // Business category
        //                // Example: Tax / License
        //                Category =
        //                    master.Category,

        //                // Mandatory / Optional /
        //                // RequestedAdditional
        //                DocumentCategory =
        //                    documentCategory,

        //                IsMandatory =
        //                    master.IsMandatory,

        //                RequiresVerification =
        //                    requiresVerification,

        //                Status =
        //                    status,

        //                // Only requested document
        //                Message =
        //                    message,

        //                DocumentId =
        //                    selectedDocument?.DocumentId
        //                    ?? Guid.Empty,

        //                UploadedAt =
        //                    selectedDocument?.UploadedAt
        //                    ?? default,

        //                VerifiedAt =
        //                    selectedDocument?.VerifiedAt
        //            });
        //    }


        //    // ==================================================
        //    // 2. REQUESTED CUSTOM DOCUMENTS
        //    // ==================================================
        //    //
        //    // Admin selected "Other".
        //    //
        //    // DocumentTypeId = null
        //    // CustomDocumentName = requested name
        //    //
        //    // These documents exist in EmployerDocumentRequests.
        //    //
        //    // ==================================================

        //    var customRequests = documentRequests
        //        .Where(r =>
        //            !r.DocumentTypeId.HasValue &&
        //            !string.IsNullOrWhiteSpace(
        //                r.CustomDocumentName))
        //        .ToList();


        //    foreach (var request in customRequests)
        //    {
        //        var requestedName =
        //            request.CustomDocumentName!.Trim();


        //        // --------------------------------------------------
        //        // FIND UPLOAD FOR EXACT REQUEST
        //        // --------------------------------------------------
        //        //
        //        // IMPORTANT:
        //        // Match by RequestId.
        //        //
        //        // Do NOT match by document name.
        //        //
        //        // --------------------------------------------------

        //        var selectedDocument =
        //            employerDocuments
        //                .Where(d =>
        //                    d.RequestId.HasValue &&
        //                    d.RequestId.Value ==
        //                        request.RequestId)
        //                .OrderByDescending(d => d.UploadedAt)
        //                .FirstOrDefault();


        //        // --------------------------------------------------
        //        // STATUS
        //        // --------------------------------------------------

        //        var status =
        //            selectedDocument == null
        //                ? "NotUploaded"
        //                : selectedDocument.Status.ToString();


        //        // --------------------------------------------------
        //        // ADD REQUESTED CUSTOM DOCUMENT
        //        // --------------------------------------------------

        //        checklist.Add(
        //            new AdminRecruiterDocumentChecklistDto
        //            {
        //                DocumentTypeId =
        //                    null,

        //                DocumentName =
        //                    requestedName,

        //                // No VerificationDocumentMaster exists
        //                // for custom requested documents.
        //                Category =
        //                    "Other",

        //                DocumentCategory =
        //                    "RequestedAdditional",

        //                IsMandatory =
        //                    false,

        //                RequiresVerification =
        //                    true,

        //                Status =
        //                    status,

        //                // ONLY requested document gets Message
        //                Message =
        //                    request.Message,

        //                DocumentId =
        //                    selectedDocument?.DocumentId
        //                    ?? Guid.Empty,

        //                UploadedAt =
        //                    selectedDocument?.UploadedAt
        //                    ?? default,

        //                VerifiedAt =
        //                    selectedDocument?.VerifiedAt
        //            });
        //    }


        //    // ==================================================
        //    // 3. NORMAL ADDITIONAL DOCUMENTS
        //    // ==================================================
        //    //
        //    // These are directly uploaded by recruiter.
        //    //
        //    // They do NOT come from EmployerDocumentRequests.
        //    //
        //    // Identification:
        //    //
        //    // RequestId = null
        //    // Category  = Additional
        //    //
        //    // ==================================================

        //    var additionalDocuments = employerDocuments
        //        .Where(d =>
        //            !d.RequestId.HasValue &&
        //            string.Equals(
        //                d.Category,
        //                "Additional",
        //                StringComparison.OrdinalIgnoreCase))
        //        .OrderByDescending(d => d.UploadedAt)
        //        .ToList();


        //    foreach (var document in additionalDocuments)
        //    {
        //        var documentName =
        //            document.CustomDocumentName
        //            ?? document.DetectedDocumentType
        //            ?? document.FileName
        //            ?? "Additional Document";


        //        checklist.Add(
        //            new AdminRecruiterDocumentChecklistDto
        //            {
        //                DocumentTypeId =
        //                    document.DocumentTypeId,

        //                DocumentName =
        //                    documentName,

        //                // Additional document does not have
        //                // VerificationDocumentMaster category.
        //                Category =
        //                    document.Category,

        //                DocumentCategory =
        //                    "Additional",

        //                IsMandatory =
        //                    false,

        //                // Normal additional documents do not
        //                // participate in verification.
        //                RequiresVerification =
        //                    false,

        //                Status =
        //                    document.Status.ToString(),

        //                // Never show request message
        //                Message =
        //                    null,

        //                DocumentId =
        //                    document.DocumentId,

        //                UploadedAt =
        //                    document.UploadedAt,

        //                VerifiedAt =
        //                    document.VerifiedAt
        //            });
        //    }


        //    // ==================================================
        //    // 4. VERIFICATION CALCULATION
        //    // ==================================================
        //    //
        //    // Verification is based on:
        //    //
        //    //     Mandatory
        //    //     Optional where RequiresVerification = true
        //    //     RequestedAdditional
        //    //
        //    // Normal Additional is excluded.
        //    //
        //    // ==================================================

        //    var verificationChecklist =
        //        checklist
        //            .Where(d => d.RequiresVerification)
        //            .ToList();


        //    // ==================================================
        //    // TOTAL REQUIRING VERIFICATION
        //    // ==================================================

        //    var verificationTotal =
        //        verificationChecklist.Count;


        //    // ==================================================
        //    // VERIFIED
        //    // ==================================================

        //    var verified =
        //        verificationChecklist.Count(d =>
        //            d.Status.Equals(
        //                VerificationDocumentStatus.Approved.ToString(),
        //                StringComparison.OrdinalIgnoreCase));


        //    // ==================================================
        //    // REJECTED
        //    // ==================================================

        //    var rejected =
        //        verificationChecklist.Count(d =>
        //            d.Status.Equals(
        //                VerificationDocumentStatus.Rejected.ToString(),
        //                StringComparison.OrdinalIgnoreCase));


        //    // ==================================================
        //    // NOT UPLOADED
        //    // ==================================================
        //    //
        //    // IMPORTANT:
        //    //
        //    // Only verification-required documents.
        //    //
        //    // Pending is NOT NotUploaded.
        //    //
        //    // Example:
        //    //
        //    // No file -> NotUploaded
        //    // File uploaded -> Pending
        //    // Approved -> Approved
        //    // Rejected -> Rejected
        //    //
        //    // ==================================================

        //    var notUploaded =
        //        verificationChecklist.Count(d =>
        //            d.Status.Equals(
        //                "NotUploaded",
        //                StringComparison.OrdinalIgnoreCase));


        //    // ==================================================
        //    // PENDING
        //    // ==================================================

        //    var pending =
        //        verificationChecklist.Count(d =>
        //            d.Status.Equals(
        //                VerificationDocumentStatus.Pending.ToString(),
        //                StringComparison.OrdinalIgnoreCase));


        //    // ==================================================
        //    // VERIFICATION STATUS
        //    // ==================================================

        //    string verificationStatus;

        //    if (verificationTotal == 0)
        //    {
        //        verificationStatus = "Pending";
        //    }
        //    else if (rejected > 0)
        //    {
        //        verificationStatus = "Rejected";
        //    }
        //    else if (verified == verificationTotal)
        //    {
        //        verificationStatus = "Verified";
        //    }
        //    else
        //    {
        //        verificationStatus = "Pending";
        //    }


        //    // ==================================================
        //    // RESPONSE
        //    // ==================================================

        //    return new AdminRecruiterDocumentChecklistResponseDto
        //    {
        //        EmployerId =
        //            employerId,

        //        // Mandatory
        //        // Optional
        //        // Additional
        //        // RequestedAdditional
        //        Total =
        //            checklist.Count,

        //        // Only RequiresVerification = true
        //        VerificationTotal =
        //            verificationTotal,

        //        Verified =
        //            verified,

        //        NotUploaded =
        //            notUploaded,

        //        Rejected =
        //            rejected,

        //        Pending =
        //            pending,

        //        VerificationStatus =
        //            verificationStatus,

        //        Documents =
        //            checklist
        //    };
        //}

        public async Task<List<AdminRecruiterDocumentVerificationListDto>>
      GetCompanyRequiredDocumentVerificationAsync(Guid employerId)
        {
            // ==================================================
            // CHECK RECRUITER
            // ==================================================

            var employerExists = await _db.EmployerProfiles
                .AsNoTracking()
                .AnyAsync(e => e.EmployerId == employerId);

            if (!employerExists)
            {
                return new List<AdminRecruiterDocumentVerificationListDto>();
            }


            // ==================================================
            // GET ACTIVE DOCUMENT MASTER TYPES
            // ==================================================

            var documentMasters = await _db.VerificationDocumentMasters
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();


            // ==================================================
            // GET RECRUITER UPLOADED DOCUMENTS
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

            var documentRequests = await _db.EmployerDocumentRequests
                .AsNoTracking()
                .Where(r =>
                    r.EmployerId == employerId &&
                    r.Status != "Cancelled")
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();


            // ==================================================
            // RESULT
            // ==================================================

            var result =
                new List<AdminRecruiterDocumentVerificationListDto>();


            // ==================================================
            // 1. MANDATORY + REQUESTED MASTER DOCUMENTS
            // ==================================================
            //
            // Include:
            //
            // 1. ALL Mandatory documents
            // 2. Optional documents ONLY if requested by admin
            //
            // IMPORTANT:
            // These are returned even when NOT uploaded.
            //
            // ==================================================

            foreach (var master in documentMasters)
            {
                // --------------------------------------------------
                // FIND LATEST ADMIN REQUEST FOR THIS DOCUMENT TYPE
                // --------------------------------------------------

                var matchingRequest = documentRequests
                    .Where(r =>
                        r.DocumentTypeId.HasValue &&
                        r.DocumentTypeId.Value ==
                            master.DocumentTypeId)
                    .OrderByDescending(r => r.RequestedAt)
                    .FirstOrDefault();


                // --------------------------------------------------
                // ONLY MANDATORY OR REQUESTED
                // --------------------------------------------------

                if (!master.IsMandatory && matchingRequest == null)
                {
                    continue;
                }


                // --------------------------------------------------
                // FIND UPLOADED DOCUMENT
                // --------------------------------------------------

                EmployerVerificationDocument? selectedDocument = null;


                // --------------------------------------------------
                // IF REQUESTED, MATCH USING REQUEST ID
                // --------------------------------------------------

                if (matchingRequest != null)
                {
                    selectedDocument =
                        employerDocuments
                            .Where(d =>
                                d.RequestId.HasValue &&
                                d.RequestId.Value ==
                                    matchingRequest.RequestId)
                            .OrderByDescending(d => d.UploadedAt)
                            .FirstOrDefault();
                }


                // --------------------------------------------------
                // FOR MANDATORY DOCUMENTS
                // IF NO REQUEST MATCH, FIND BY DOCUMENT TYPE
                // --------------------------------------------------

                selectedDocument ??=
                    employerDocuments
                        .Where(d =>
                            d.DocumentTypeId.HasValue &&
                            d.DocumentTypeId.Value ==
                                master.DocumentTypeId &&
                            !d.RequestId.HasValue)
                        .OrderByDescending(d => d.UploadedAt)
                        .FirstOrDefault();


                // --------------------------------------------------
                // DOCUMENT CATEGORY
                // --------------------------------------------------

                string documentCategory;

                if (matchingRequest != null)
                {
                    documentCategory =
                        "RequestedAdditional";
                }
                else
                {
                    documentCategory =
                        "Mandatory";
                }


                // --------------------------------------------------
                // VERIFICATION STATUS
                // --------------------------------------------------

                var verificationStatus =
                    selectedDocument == null
                        ? "NotUploaded"
                        : selectedDocument.Status.ToString();


                // --------------------------------------------------
                // ADD DOCUMENT
                // --------------------------------------------------

                result.Add(
                    new AdminRecruiterDocumentVerificationListDto
                    {
                        // Actual uploaded document ID.
                        // NULL when not uploaded.
                        DocumentId =
                            selectedDocument?.DocumentId,

                        // Master DocumentTypeId is available
                        // even when document is not uploaded.
                        DocumentTypeId =
                            selectedDocument?.DocumentTypeId
                            ?? master.DocumentTypeId,

                        // Request ID:
                        // requested document -> request ID
                        // mandatory without request -> null
                        RequestId =
                            matchingRequest?.RequestId
                            ?? selectedDocument?.RequestId,

                        DocumentName =
                            master.DocumentName,

                        DocumentType =
                            master.Code,

                        DocumentCategory =
                            documentCategory,

                        DocumentTypeCategory =
                            master.Category,

                        DocumentVerificationStatus =
                            verificationStatus
                    });
            }


            // ==================================================
            // 2. REQUESTED CUSTOM DOCUMENTS
            // ==================================================
            //
            // Admin selected "Other".
            //
            // These must ALSO be returned even when they
            // have not been uploaded yet.
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
                // --------------------------------------------------
                // FIND UPLOADED DOCUMENT FOR EXACT REQUEST
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
                // VERIFICATION STATUS
                // --------------------------------------------------

                var verificationStatus =
                    selectedDocument == null
                        ? "NotUploaded"
                        : selectedDocument.Status.ToString();


                // --------------------------------------------------
                // ADD REQUESTED CUSTOM DOCUMENT
                // --------------------------------------------------

                result.Add(
                    new AdminRecruiterDocumentVerificationListDto
                    {
                        // NULL until recruiter uploads
                        DocumentId =
                            selectedDocument?.DocumentId,

                        // Custom request has no master type
                        DocumentTypeId =
                            selectedDocument?.DocumentTypeId,

                        // ALWAYS available from request
                        RequestId =
                            request.RequestId,

                        DocumentName =
                            request.CustomDocumentName!.Trim(),

                        DocumentType =
                            "Other",

                        DocumentCategory =
                            "RequestedAdditional",

                        DocumentTypeCategory =
                            "Other",

                        DocumentVerificationStatus =
                            verificationStatus
                    });
            }


            // ==================================================
            // RETURN
            // ==================================================

            return result;
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