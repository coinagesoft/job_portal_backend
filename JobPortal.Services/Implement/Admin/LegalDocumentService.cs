using JobPortal.Application.DTOs.Admin.LegalPages;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Admin
{
    public class LegalDocumentService : ILegalDocumentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LegalDocumentService> _logger;

        public LegalDocumentService(
            AppDbContext context,
            ILogger<LegalDocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============================================================
        // GET ALL LEGAL DOCUMENTS
        // ============================================================

        public async Task<List<LegalDocumentAdminDto>> GetAllAsync()
        {
            var docs = await _context.LegalDocuments
                .AsNoTracking()
                .OrderBy(x => x.Type)
                .ToListAsync();

            return docs.Select(Map).ToList();
        }

        // ============================================================
        // GET LEGAL DOCUMENT BY TYPE
        // ============================================================

        public async Task<LegalDocumentAdminDto?> GetByTypeAsync(string type)
        {
            var doc = await FindAsync(type, tracking: false);

            return doc == null ? null : Map(doc);
        }

        // ============================================================
        // ENSURE UTC DATE
        // ============================================================

        /// <summary>
        /// Dates coming from JSON such as "2026-09-01" can deserialize
        /// with DateTime.Kind = Unspecified.
        ///
        /// PostgreSQL timestamp with time zone requires UTC.
        /// </summary>
        private static DateTime? EnsureUtc(DateTime? value)
        {
            if (value == null)
                return null;

            return value.Value.Kind switch
            {
                DateTimeKind.Utc =>
                    value.Value,

                DateTimeKind.Local =>
                    value.Value.ToUniversalTime(),

                _ =>
                    DateTime.SpecifyKind(
                        value.Value,
                        DateTimeKind.Utc)
            };
        }

        // ============================================================
        // NORMALIZE TYPE
        // ============================================================

        private static string NormalizeType(string type)
        {
            return (type ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
        }

        // ============================================================
        // GET DEFAULT TITLE
        // ============================================================


        private static string GetDefaultTitle(string type)
        {
            return type switch
            {
                "privacy" =>
                    "Privacy Policy",

                "terms" =>
                    "Terms & Conditions",

                "cancellation-refund" =>
                    "Cancellation & Refund Policy",

                "shipping-delivery" =>
                    "Shipping & Delivery Policy",

                _ =>
                    type
            };
        }


        // ============================================================
        // CREATE NEW LEGAL DOCUMENT
        // ============================================================

        private static LegalDocument CreateDocument(
            string type,
            string content,
            DateTime? effectiveDate,
            Guid? adminId)
        {
            var now = DateTime.UtcNow;

            return new LegalDocument
            {
                DocumentId = Guid.NewGuid(),

                Type = type,

                Title = GetDefaultTitle(type),

                DraftContent = content ?? string.Empty,

                DraftEffectiveDate = effectiveDate,

                PublishedContent = null,

                PublishedEffectiveDate = null,

                PublishedAt = null,

                Status = "Draft",

                UpdatedBy = adminId,

                UpdatedAt = now
            };
        }

        // ============================================================
        // SAVE DRAFT
        // ============================================================

        public async Task<LegalDocumentAdminDto?> SaveDraftAsync(
            string type,
            SaveLegalDocumentRequestDto request,
            Guid? adminId)
        {
            var normalizedType = NormalizeType(type);

            var effectiveDate =
                EnsureUtc(request.EffectiveDate);

            var content =
                request.Content ?? string.Empty;

            // --------------------------------------------------------
            // Find existing document
            // --------------------------------------------------------

            var doc = await FindAsync(
                normalizedType,
                tracking: true);

            // --------------------------------------------------------
            // If document doesn't exist, CREATE it
            // --------------------------------------------------------

            if (doc == null)
            {
                doc = CreateDocument(
                    normalizedType,
                    content,
                    effectiveDate,
                    adminId);

                _context.LegalDocuments.Add(doc);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "New legal document draft created. Type:{Type} AdminId:{AdminId}",
                    normalizedType,
                    adminId);

                return Map(doc);
            }

            // --------------------------------------------------------
            // Existing document -> update draft
            // --------------------------------------------------------

            doc.DraftContent = content;

            doc.DraftEffectiveDate =
                effectiveDate;

            doc.Status = "Draft";

            doc.UpdatedBy = adminId;

            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Legal document draft saved. Type:{Type} AdminId:{AdminId}",
                normalizedType,
                adminId);

            return Map(doc);
        }

        // ============================================================
        // PUBLISH
        // ============================================================

        public async Task<LegalDocumentAdminDto?> PublishAsync(
            string type,
            SaveLegalDocumentRequestDto request,
            Guid? adminId)
        {
            var normalizedType = NormalizeType(type);

            var effectiveDate =
                EnsureUtc(request.EffectiveDate)
                ?? DateTime.UtcNow.Date;

            var content =
                request.Content ?? string.Empty;

            // --------------------------------------------------------
            // Find existing document
            // --------------------------------------------------------

            var doc = await FindAsync(
                normalizedType,
                tracking: true);

            // --------------------------------------------------------
            // If document doesn't exist, CREATE it directly as
            // published.
            // --------------------------------------------------------

            if (doc == null)
            {
                var now = DateTime.UtcNow;

                doc = new LegalDocument
                {
                    DocumentId = Guid.NewGuid(),

                    Type = normalizedType,

                    Title = GetDefaultTitle(normalizedType),

                    DraftContent = content,

                    DraftEffectiveDate = effectiveDate,

                    PublishedContent = content,

                    PublishedEffectiveDate = effectiveDate,

                    PublishedAt = now,

                    Status = "Published",

                    UpdatedBy = adminId,

                    UpdatedAt = now
                };

                _context.LegalDocuments.Add(doc);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "New legal document created and published. Type:{Type} AdminId:{AdminId}",
                    normalizedType,
                    adminId);

                return Map(doc);
            }

            // --------------------------------------------------------
            // Existing document -> update draft + published version
            // --------------------------------------------------------

            doc.DraftContent = content;

            doc.DraftEffectiveDate = effectiveDate;

            doc.PublishedContent = content;

            doc.PublishedEffectiveDate = effectiveDate;

            doc.PublishedAt = DateTime.UtcNow;

            doc.Status = "Published";

            doc.UpdatedBy = adminId;

            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Legal document published. Type:{Type} AdminId:{AdminId}",
                normalizedType,
                adminId);

            return Map(doc);
        }

        // ============================================================
        // DISCARD SERVER DRAFT
        // ============================================================

        public async Task<LegalDocumentAdminDto?> DiscardDraftAsync(
            string type)
        {
            var normalizedType = NormalizeType(type);

            var doc = await FindAsync(
                normalizedType,
                tracking: true);

            if (doc == null)
                return null;

            // --------------------------------------------------------
            // If something has already been published,
            // restore the draft to the published version.
            // --------------------------------------------------------

            if (doc.PublishedContent != null)
            {
                doc.DraftContent =
                    doc.PublishedContent;

                doc.DraftEffectiveDate =
                    doc.PublishedEffectiveDate;

                doc.Status = "Published";
            }
            else
            {
                // ----------------------------------------------------
                // No published version yet.
                // Clear the draft.
                // ----------------------------------------------------

                doc.DraftContent = string.Empty;

                doc.DraftEffectiveDate = null;

                doc.Status = "Draft";
            }

            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Legal document draft discarded. Type:{Type}",
                normalizedType);

            return Map(doc);
        }

        // ============================================================
        // FIND DOCUMENT
        // ============================================================

        private Task<LegalDocument?> FindAsync(
            string type,
            bool tracking)
        {
            var normalized =
                NormalizeType(type);

            IQueryable<LegalDocument> query =
                tracking
                    ? _context.LegalDocuments
                    : _context.LegalDocuments.AsNoTracking();

            return query.FirstOrDefaultAsync(
                x => x.Type == normalized);
        }

        private static bool IsSupportedType(string type)
        {
            return type is
                "privacy" or
                "terms" or
                "cancellation-refund" or
                "shipping-delivery";
        }

        // ============================================================
        // MAP ENTITY -> DTO
        // ============================================================

        private static LegalDocumentAdminDto Map(
            LegalDocument doc)
        {
            var hasUnpublishedChanges =
     doc.PublishedContent != null &&
     (
         doc.DraftContent != doc.PublishedContent ||
         doc.DraftEffectiveDate != doc.PublishedEffectiveDate
     );

            return new LegalDocumentAdminDto
            {
                DocumentId =
                    doc.DocumentId,

                Type =
                    doc.Type,

                Title =
                    doc.Title,

                DraftContent =
                    doc.DraftContent,

                DraftEffectiveDate =
                    doc.DraftEffectiveDate,

                PublishedContent =
                    doc.PublishedContent,

                PublishedEffectiveDate =
                    doc.PublishedEffectiveDate,

                PublishedAt =
                    doc.PublishedAt,

                Status =
                    doc.Status,

                HasUnpublishedChanges =
                    hasUnpublishedChanges,

                UpdatedAt =
                    doc.UpdatedAt
            };
        }
    }
}
