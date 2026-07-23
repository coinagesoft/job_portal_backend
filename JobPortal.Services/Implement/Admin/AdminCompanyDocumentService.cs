using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
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
    public class AdminCompanyDocumentService : IAdminCompanyDocumentService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AdminCompanyDocumentService> _logger;

        public AdminCompanyDocumentService(
            AppDbContext context, ILogger<AdminCompanyDocumentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PendingCompanyDocumentDto>> GetPendingAsync()
        {
            var pending = await (
                from doc in _context.EmployerVerificationDocuments.AsNoTracking()
                join profile in _context.EmployerProfiles.AsNoTracking()
                    on doc.EmployerId equals profile.EmployerId
                where doc.Status == VerificationDocumentStatus.Pending && !doc.IsDeleted
                orderby doc.UploadedAt
                select new PendingCompanyDocumentDto
                {
                    DocumentId = doc.DocumentId,
                    EmployerId = doc.EmployerId,
                    CompanyName = profile.CompanyDisplayName,
                    DocumentName = doc.DocumentType.DocumentName ?? string.Empty,
                    Category = doc.DocumentType.Category ?? string.Empty,
                    FileUrl = doc.FileUrl,
                    UploadedAt = doc.UploadedAt
                }
            ).ToListAsync();

            return pending;
        }

        public async Task<bool> VerifyAsync(
         Guid adminUserId,
         Guid documentId,
         VerifyCompanyDocumentRequestDto request)
        {
            try
            {
                var doc = await _context.EmployerVerificationDocuments
                    .Include(x => x.DocumentType)
                    .FirstOrDefaultAsync(x => x.DocumentId == documentId && !x.IsDeleted);

                if (doc == null)
                    return false;

                if (!request.Approve && string.IsNullOrWhiteSpace(request.Remarks))
                    throw new Exception("Remarks are required when rejecting a document.");

                doc.Status = request.Approve
                    ? VerificationDocumentStatus.Approved
                    : VerificationDocumentStatus.Rejected;

                doc.VerifiedBy = adminUserId;
                doc.VerifiedAt = DateTime.UtcNow;
                doc.Remarks = request.Remarks;

                var badge = await _context.EmployerBadges
                    .FirstOrDefaultAsync(x => x.VerificationDocumentId == documentId);

                if (request.Approve)
                {
                    if (badge == null)
                    {
                        badge = new EmployerBadge
                        {
                            BadgeId = Guid.NewGuid(),
                            EmployerId = doc.EmployerId,

                            // Dynamic badge
                            BadgeType = null,

                            VerificationDocumentId = documentId,

                            BadgeStatus = BadgeStatus.Approved,

                            IssuedBy = adminUserId,
                            IssuedAt = DateTime.UtcNow
                        };

                        _context.EmployerBadges.Add(badge);
                    }
                    else
                    {
                        badge.BadgeStatus = BadgeStatus.Approved;
                        badge.RevocationReason = null;
                        badge.RevokedAt = null;
                        badge.IssuedBy = adminUserId;
                        badge.IssuedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    if (badge != null)
                    {
                        badge.BadgeStatus = BadgeStatus.Revoked;
                        badge.RevokedAt = DateTime.UtcNow;
                        badge.RevocationReason = request.Remarks;
                    }
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "VerifyAsync error. DocumentId:{DocumentId}",
                    documentId);

                return false;
            }
        }
    }
}
