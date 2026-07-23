using JobPortal.Application.DTOs.Recruiter.CompanyDocuments;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Recruiter
{
    public class DocumentTypeService : IDocumentTypeService
    {
        private readonly AppDbContext _context;

        public DocumentTypeService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RecruiterDocumentTypeDto>> GetActiveDocumentTypesAsync(Guid employerId)
        {
            var types = await _context.VerificationDocumentMasters
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var myDocs = await _context.EmployerVerificationDocuments
                .AsNoTracking()
                .Where(x =>
                    x.EmployerId == employerId &&
                    !x.IsDeleted)
                .ToListAsync();

            // If multiple uploads are allowed, show the latest uploaded document
            var latestByType = myDocs
                .GroupBy(x => x.DocumentTypeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(d => d.UploadedAt).First());

            return types.Select(t =>
            {
                latestByType.TryGetValue(t.DocumentTypeId, out var mine);

                return new RecruiterDocumentTypeDto
                {
                    DocumentTypeId = t.DocumentTypeId,
                    DocumentName = t.DocumentName,
                    Category = t.Category,
                    IsMandatory = t.IsMandatory,
                    RequiresVerification = t.RequiresVerification,
                    AllowMultipleUploads = t.AllowMultipleUploads,
                    DisplayOrder = t.DisplayOrder,
                    Description = t.Description,

                    MyDocumentId = mine?.DocumentId,
                    MyStatus = mine?.Status,
                    MyUploadedAt = mine?.UploadedAt
                };
            }).ToList();
        }
    }
}
