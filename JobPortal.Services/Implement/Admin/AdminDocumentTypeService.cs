using JobPortal.Application.DTOs.Admin.CompanyDocuments;
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
    public class AdminDocumentTypeService : IAdminDocumentTypeService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminDocumentTypeService> _logger;

        public AdminDocumentTypeService(
            AppDbContext context, ILogger<AdminDocumentTypeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<DocumentTypeAdminDto>> GetAllAsync()
        {
            var types = await _context.VerificationDocumentMasters
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return types.Select(Map).ToList();
        }

        public async Task<DocumentTypeAdminDto?> CreateAsync(CreateDocumentTypeRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.DocumentName))
                    throw new Exception("Document name is required.");

                if (string.IsNullOrWhiteSpace(request.Category))
                    throw new Exception("Category is required.");

                var documentName = request.DocumentName.Trim();

                var exists = await _context.VerificationDocumentMasters
                    .AnyAsync(x => x.DocumentName.ToLower() == documentName.ToLower());

                if (exists)
                    throw new Exception("Document type already exists.");

                var maxDisplayOrder = await _context.VerificationDocumentMasters
                    .MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

                var entity = new VerificationDocumentMaster
                {
                    DocumentTypeId = Guid.NewGuid(),

                    // Auto-generated internal code
                    Code = Guid.NewGuid().ToString("N")[..8].ToUpper(),

                    DocumentName = documentName,
                    Category = request.Category.Trim(),

                    IsMandatory = request.IsMandatory,
                    RequiresVerification = request.RequiresVerification,

                    // System defaults
                    IsActive = true,
                    AllowMultipleUploads = false,
                    AllowCustomDocument = true,
                    IsSystemDocument = true,

                    DisplayOrder = maxDisplayOrder + 1,

                    CreatedAt = DateTime.UtcNow
                };

                _context.VerificationDocumentMasters.Add(entity);

                await _context.SaveChangesAsync();

                return Map(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating document type.");

                return null;
            }
        }
        public async Task<DocumentTypeAdminDto?> UpdateAsync(
            Guid id, UpdateDocumentTypeRequestDto request)
        {
            try
            {
                var type = await _context.VerificationDocumentMasters
                    .FirstOrDefaultAsync(x => x.DocumentTypeId == id);

                if (type == null)
                    return null;

                if (request.IsMandatory.HasValue)
                    type.IsMandatory = request.IsMandatory.Value;

                if (request.IsActive.HasValue)
                    type.IsActive = request.IsActive.Value;

                if (request.RequiresVerification.HasValue)
                    type.RequiresVerification = request.RequiresVerification.Value;

                type.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Map(type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateAsync error. DocumentTypeId:{Id}", id);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var type = await _context.VerificationDocumentMasters
                    .FirstOrDefaultAsync(x => x.DocumentTypeId == id);

                if (type == null)
                    return false;

                // Soft delete
                type.IsActive = false;
                type.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting document type. DocumentTypeId: {DocumentTypeId}",
                    id);

                return false;
            }
        }
        private static DocumentTypeAdminDto Map(VerificationDocumentMaster type)
        {
            return new DocumentTypeAdminDto
            {
                Id = type.DocumentTypeId,
                DocumentName = type.DocumentName,
                Category = type.Category,
                IsMandatory = type.IsMandatory,
                IsActive = type.IsActive,
                RequiresVerification = type.RequiresVerification,
                AllowMultipleUploads = type.AllowMultipleUploads,
                DisplayOrder = type.DisplayOrder,
                Description = type.Description
            };
        }
    }
}
