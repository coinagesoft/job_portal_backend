using JobPortal.Application.DTOs;
using JobPortal.Application.DTOs.Admin;
using JobPortal.Application.DTOs.Candidate;
using JobPortal.Domain.Enums.common;
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
    public class AdminCandidateService : IAdminCandidateService
    {
        private readonly AppDbContext _db; // rename to your actual DbContext class

        public AdminCandidateService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<AdminCandidateListItemDto>> GetCandidatesAsync()
        {
            var candidates = await _db.CandidateProfiles
                .AsNoTracking()
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CandidateId,
                    c.ProfilePhotoUrl,
                    c.FullName,
                    Email = c.User.Email,
                    c.PrimaryTrade,
                    Status = c.User.AccountStatus,
                    c.CreatedAt
                })
                .ToListAsync();

            // Formatting (CreatedAt -> "MMM d, yyyy") happens after the DB
            // round-trip since EF Core can't translate .ToString(format) to SQL.
            return candidates.Select(c => new AdminCandidateListItemDto
            {
                Id = c.CandidateId.ToString(),
                Img = c.ProfilePhotoUrl,
                Name = c.FullName,
                Email = c.Email,
                Trade = c.PrimaryTrade,
                Status = c.Status.ToString(),
                Joined = c.CreatedAt.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)
            }).ToList();
        }

        public async Task<AdminCandidateDetailDto?> GetCandidateDetailAsync(Guid candidateId)
        {
            var c = await _db.CandidateProfiles
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.CandidateId == candidateId)
                .Select(x => new
                {
                    x.CandidateId,
                    x.ProfilePhotoUrl,
                    AccountStatus = x.User.AccountStatus,
                    x.ProfileCompletionPct,
                    x.FullName,
                    Email = x.User.Email,
                    x.User.CountryCode,
                    x.User.MobileNumber,
                    x.CreatedAt,
                    x.PrimaryTrade,
                    x.CurrentCity,
                    x.CurrentState,
                    x.TotalExperienceYears,
                    x.AvailabilityStatus,
                    PaymentStatus = x.User.PaymentStatus
                })
                .FirstOrDefaultAsync();

            if (c == null) return null;

            var billing = await _db.PaymentTransactions
                .AsNoTracking()
                .Where(t => t.CandidateId == candidateId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.TransactionId,
                    t.CreatedAt,
                    t.TotalAmountPaise,
                    t.PaymentStatus
                })
                .ToListAsync();

            var documents = await _db.CandidateDocuments
                .AsNoTracking()
                .Where(d => d.CandidateId == candidateId)
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new
                {
                    d.DocumentId,
                    d.DocumentType,
                    d.FileUrl,
                    d.UploadedAt,
                    d.VerificationStatus
                })
                .ToListAsync();

            var location = string.Join(", ", new[] { c.CurrentCity, c.CurrentState }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            return new AdminCandidateDetailDto
            {
                Id = c.CandidateId.ToString(),
                Img = c.ProfilePhotoUrl,
                AccountStatus = c.AccountStatus.ToString(),
                CompletenessPct = c.ProfileCompletionPct,
                Name = c.FullName,
                Email = c.Email,
                Phone = string.IsNullOrWhiteSpace(c.MobileNumber)
                    ? null
                    : $"{c.CountryCode} {c.MobileNumber}".Trim(),
                RegisteredOn = c.CreatedAt.ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
                TradeCategory = c.PrimaryTrade,
                Location = string.IsNullOrWhiteSpace(location) ? null : location,
                Experience = $"{c.TotalExperienceYears} Years",
                PaymentStatus = c.PaymentStatus.ToString(),
                AvailableForWork = c.AvailabilityStatus == "Available",
                Billing = billing.Select(t => new AdminCandidateBillingItemDto
                {
                    TransactionId = t.TransactionId.ToString(),
                    Date = t.CreatedAt.ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
                    Amount = $"₹{t.TotalAmountPaise / 100.0:0.00}",
                    Status = t.PaymentStatus
                }).ToList(),
                Documents = documents.Select(d => new AdminCandidateDocumentItemDto
                {
                    DocId = d.DocumentId.ToString(),
                    Title = d.DocumentType,
                    Url = d.FileUrl,
                    UploadedOn = d.UploadedAt.ToString("MMM d, yyyy", CultureInfo.InvariantCulture),
                    VerificationStatus = d.VerificationStatus
                }).ToList()
            };
        }

        public async Task<bool> UpdateAccountStatusAsync(Guid candidateId, UpdateAccountStatusRequestDto request)
        {
            var user = await _db.CandidateProfiles
                .Where(x => x.CandidateId == candidateId)
                .Select(x => x.User)
                .FirstOrDefaultAsync();

            if (user == null) return false;

            if (!Enum.TryParse<AccountStatus>(request.AccountStatus, ignoreCase: true, out var parsedStatus))
                throw new ArgumentException($"'{request.AccountStatus}' is not a valid account status.");

            user.AccountStatus = parsedStatus;
            user.SuspensionReason = parsedStatus.ToString() == "Suspended" ? request.Reason : null;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }
    }
}
