using JobPortal.Application.DTOs.Public;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IPublic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Public
{
    public class LegalDocumentPublicService : ILegalDocumentPublicService
    {
        private readonly AppDbContext _context;

        public LegalDocumentPublicService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<LegalDocumentPublicDto?> GetPublishedAsync(string type)
        {
            var normalized = (type ?? string.Empty).Trim().ToLowerInvariant();

            var doc = await _context.LegalDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Type == normalized);

            // Never expose a document that hasn't been published yet, even if
            // it exists with draft-only content sitting in the admin editor.
            if (doc == null || doc.PublishedContent == null || doc.PublishedAt == null)
                return null;

            return new LegalDocumentPublicDto
            {
                Type = doc.Type,
                Title = doc.Title,
                Content = doc.PublishedContent,
                EffectiveDate = doc.PublishedEffectiveDate ?? doc.PublishedAt.Value,
                PublishedAt = doc.PublishedAt.Value
            };
        }
    }
}