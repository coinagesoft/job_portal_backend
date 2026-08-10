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

        public LegalDocumentService(AppDbContext context, ILogger<LegalDocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<LegalDocumentAdminDto>> GetAllAsync()
        {
            var docs = await _context.LegalDocuments
                .AsNoTracking()
                .OrderBy(x => x.Type)
                .ToListAsync();

            return docs.Select(Map).ToList();
        }

        public async Task<LegalDocumentAdminDto?> GetByTypeAsync(string type)
        {
            var doc = await FindAsync(type, tracking: false);
            return doc == null ? null : Map(doc);
        }

        /// <summary>
        /// Dates coming in from JSON (e.g. "2026-09-01") deserialize with
        /// DateTime.Kind = Unspecified, but the "timestamp with time zone"
        /// columns require Utc — Npgsql throws otherwise. Treat any
        /// unspecified/local value as UTC rather than rejecting it.
        /// </summary>
        private static DateTime? EnsureUtc(DateTime? value)
        {
            if (value == null) return null;

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }

        public async Task<LegalDocumentAdminDto?> SaveDraftAsync(
            string type, SaveLegalDocumentRequestDto request, Guid? adminId)
        {
            var doc = await FindAsync(type, tracking: true);
            if (doc == null) return null;

            doc.DraftContent = request.Content;
            doc.DraftEffectiveDate = EnsureUtc(request.EffectiveDate);
            doc.Status = "Draft";
            doc.UpdatedBy = adminId;
            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Legal document draft saved. Type:{Type} AdminId:{AdminId}", type, adminId);

            return Map(doc);
        }

        public async Task<LegalDocumentAdminDto?> PublishAsync(
            string type, SaveLegalDocumentRequestDto request, Guid? adminId)
        {
            var doc = await FindAsync(type, tracking: true);
            if (doc == null) return null;

            var effectiveDate = EnsureUtc(request.EffectiveDate) ?? DateTime.UtcNow.Date;

            // Publishing both updates the draft (so the editor reflects exactly
            // what went live) and promotes it to the published copy candidates see.
            doc.DraftContent = request.Content;
            doc.DraftEffectiveDate = effectiveDate;

            doc.PublishedContent = request.Content;
            doc.PublishedEffectiveDate = effectiveDate;
            doc.PublishedAt = DateTime.UtcNow;

            doc.Status = "Published";
            doc.UpdatedBy = adminId;
            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Legal document published. Type:{Type} AdminId:{AdminId}", type, adminId);

            return Map(doc);
        }

        public async Task<LegalDocumentAdminDto?> DiscardDraftAsync(string type)
        {
            var doc = await FindAsync(type, tracking: true);
            if (doc == null) return null;

            // Nothing published yet -> discarding just clears the draft back to empty defaults.
            doc.DraftContent = doc.PublishedContent ?? doc.DraftContent;
            doc.DraftEffectiveDate = doc.PublishedEffectiveDate;
            doc.Status = doc.PublishedContent != null ? "Published" : "Draft";
            doc.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Map(doc);
        }

        private Task<LegalDocument?> FindAsync(string type, bool tracking)
        {
            var normalized = (type ?? string.Empty).Trim().ToLowerInvariant();

            var query = tracking
                ? _context.LegalDocuments.AsQueryable()
                : _context.LegalDocuments.AsNoTracking();

            return query.FirstOrDefaultAsync(x => x.Type == normalized)!;
        }

        private static LegalDocumentAdminDto Map(LegalDocument doc)
        {
            var hasUnpublishedChanges =
                doc.DraftContent != (doc.PublishedContent ?? string.Empty) ||
                doc.DraftEffectiveDate != doc.PublishedEffectiveDate;

            return new LegalDocumentAdminDto
            {
                DocumentId = doc.DocumentId,
                Type = doc.Type,
                Title = doc.Title,
                DraftContent = doc.DraftContent,
                DraftEffectiveDate = doc.DraftEffectiveDate,
                PublishedContent = doc.PublishedContent,
                PublishedEffectiveDate = doc.PublishedEffectiveDate,
                PublishedAt = doc.PublishedAt,
                Status = doc.Status,
                HasUnpublishedChanges = hasUnpublishedChanges,
                UpdatedAt = doc.UpdatedAt
            };
        }
    }
}