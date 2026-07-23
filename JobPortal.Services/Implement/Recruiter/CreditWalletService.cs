using JobPortal.Application.DTOs.JobPosting;
using JobPortal.Application.DTOs.Recruiter.CreditWallet;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using JobPortal.Services.IImplement.IRecruiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Recruiter
{
    public class CreditWalletService : ICreditWalletService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly IResumeWatermarkService _watermark;
        private readonly ICvGenerationService _cvGeneration;
        private readonly ILogger<CreditWalletService> _logger;

        public CreditWalletService(
            IConfiguration configuration,
            AppDbContext context,
            IResumeWatermarkService watermark,
            ICvGenerationService cvGeneration,
            ILogger<CreditWalletService> logger)
        {
            _configuration = configuration;
            _context = context;
            _watermark = watermark;
            _cvGeneration = cvGeneration;
            _logger = logger;
        }

        public async Task<WalletSummaryDto?> GetEmployerWalletAsync(Guid employerId)
        {
            var wallet =
                await GetEmployerWalletEntityAsync(employerId);

            if (wallet == null)
                return null;

            var allocatedCredits =
                await _context.SubUserCreditAllocation
                    .Where(x =>
                        x.EmployerId == employerId)
                    .SumAsync(x =>
                        (int?)x.RemainingCredits) ?? 0;

            return new WalletSummaryDto
            {
                EmployerId = employerId,

                CreditBalance =
                    wallet.CreditBalance,

                AllocatedCredits =
                    allocatedCredits,

                AvailableCredits =
                    wallet.CreditBalance -
                    allocatedCredits,

                PackageName =
                    wallet.PackageName,

                PackExpiresAt =
                    wallet.PackExpiresAt
            };
        }

        public async Task<AllocateCreditsResponseDto> AllocateCreditsAsync(Guid employerId, AllocateCreditsRequestDto request)
        {
            var wallet =
                await GetEmployerWalletEntityAsync(
                    employerId);

            if (wallet == null)
            {
                return new AllocateCreditsResponseDto
                {
                    Success = false,
                    Message = "Wallet not found."
                };
            }

            var subUser = await GetSubUserAsync(request.SubUserId);

            if (subUser == null)
            {
                return new AllocateCreditsResponseDto
                {
                    Success = false,
                    Message = "Sub user not found."
                };
            }

            var allocatedAlready =
                await _context.SubUserCreditAllocation
                    .Where(x =>
                        x.EmployerId == employerId)
                    .SumAsync(x =>
                        (int?)x.RemainingCredits) ?? 0;

            var availableCredits =
                wallet.CreditBalance -
                allocatedAlready;

            if (availableCredits < request.Credits)
            {
                return new AllocateCreditsResponseDto
                {
                    Success = false,
                    Message =
                        "Not enough available credits."
                };
            }

            // IMPORTANT: the balance-tracking allocation is keyed by the
            // sub-user's actual login identity (User.UserId) — that's what
            // DeductSubUserCreditsAsync/GetSubUserCreditBalanceAsync look
            // up at unlock/download time, resolved from the JWT. Keying
            // this by the EmployerSubUser row's own id (request.SubUserId)
            // instead would silently create an allocation the sub-user can
            // never actually spend from.
            var allocation =
                await GetSubUserAllocationAsync(
                    subUser.UserId);

            if (allocation == null)
            {
                allocation =
                    new SubUserCreditAllocation
                    {
                        AllocationId =
                            Guid.NewGuid(),

                        EmployerId =
                            employerId,

                        SubUserId =
                            subUser.UserId,

                        AllocatedCredits =
                            request.Credits,

                        UsedCredits = 0,

                        RemainingCredits =
                            request.Credits,

                        AllocatedAt =
                            DateTime.UtcNow,

                        UpdatedAt =
                            DateTime.UtcNow
                    };

                await _context
                    .SubUserCreditAllocation
                    .AddAsync(allocation);
            }
            else
            {
                allocation.AllocatedCredits +=
                    request.Credits;

                allocation.RemainingCredits +=
                    request.Credits;

                allocation.UpdatedAt =
                    DateTime.UtcNow;
            }

            // The history log is purely for display on the Credit Wallet
            // page, which resolves names via the EmployerSubUser row id —
            // keep using request.SubUserId here so that lookup keeps working.
            await _context.CreditAllocationHistory
                .AddAsync(
                    new CreditAllocationHistory
                    {
                        HistoryId =
                            Guid.NewGuid(),

                        EmployerId =
                            employerId,

                        SubUserId =
                            request.SubUserId,

                        SubUserName =
                            subUser.SubUserName,

                        CreditsAllocated =
                            request.Credits,

                        BalanceBefore =
                            availableCredits,

                        BalanceAfter =
                            availableCredits -
                            request.Credits,



                        CreatedAt =
                            DateTime.UtcNow
                    });

            await _context.SaveChangesAsync();

            return new AllocateCreditsResponseDto
            {
                Success = true,
                Message =
                    "Credits allocated successfully.",

                EmployerId =
                    employerId,

                SubUserId =
                    request.SubUserId,

                AllocatedCredits =
                    request.Credits,

                RemainingEmployerCredits =
                    availableCredits -
                    request.Credits
            };
        }

        public async Task<SubUserCreditBalanceDto?> GetSubUserCreditBalanceAsync(Guid subUserId)
        {
            var allocation =
                await GetSubUserAllocationAsync(
                    subUserId);

            if (allocation == null)
                return null;

            return new SubUserCreditBalanceDto
            {
                SubUserId =
                    allocation.SubUserId,

                AllocatedCredits =
                    allocation.AllocatedCredits,

                UsedCredits =
                    allocation.UsedCredits,

                RemainingCredits =
                    allocation.RemainingCredits
            };
        }

        public async Task<UnlockCandidateResponseDto> UnlockCandidateAsync(
        Guid employerId,
        Guid actionUserId,
        bool isSubUser,
        UnlockCandidateRequestDto request)
        {
            var permissionCheck = await CheckSubUserPermissionAsync(
                actionUserId, isSubUser, s => s.CanUnlockProfiles);

            if (!permissionCheck.Allowed)
            {
                return new UnlockCandidateResponseDto
                {
                    Success = false,
                    Message = permissionCheck.Message
                };
            }

            var candidate =
                await GetCandidateAsync(
                    request.CandidateId);

            if (candidate == null)
            {
                return new UnlockCandidateResponseDto
                {
                    Success = false,
                    Message = "Candidate not found."
                };
            }

            var existingAccess =
                await HasCandidateAccessAsync(
                    employerId,
                    request.CandidateId);

            if (existingAccess)
            {
                return new UnlockCandidateResponseDto
                {
                    Success = false,
                    Message =
                        "Candidate already unlocked."
                };
            }

            var config =
                await GetCreditConfigurationAsync();

            var wallet = await GetEmployerWalletEntityAsync(employerId);

            if (wallet == null)
            {
                return new UnlockCandidateResponseDto
                {
                    Success = false,
                    Message = "Wallet not found."
                };
            }

            if (wallet.PackExpiresAt.HasValue &&
                wallet.PackExpiresAt.Value < DateTime.UtcNow)
            {
                return new UnlockCandidateResponseDto
                {
                    Success = false,
                    Message =
                        "Credit package has expired. Please purchase a new package."
                };
            }

            if (config == null)
            {
                return new UnlockCandidateResponseDto
                {
                    Success = false,
                    Message =
                        "Credit configuration not found."
                };
            }

            var unlockCost =
                config.ProfileUnlockCredits;

            int balanceBefore;
            int balanceAfter;

            if (isSubUser)
            {
                var deduct =
                    await DeductSubUserCreditsAsync(
                        actionUserId,
                        unlockCost);

                if (!deduct.Success)
                {
                    return new UnlockCandidateResponseDto
                    {
                        Success = false,
                        Message = deduct.Message
                    };
                }

                balanceBefore = deduct.BalanceBefore;
                balanceAfter = deduct.BalanceAfter;
            }
            else
            {
                var deduct =
                    await DeductEmployerCreditsAsync(
                        employerId,
                        unlockCost);

                if (!deduct.Success)
                {
                    return new UnlockCandidateResponseDto
                    {
                        Success = false,
                        Message = deduct.Message
                    };
                }

                balanceBefore = deduct.BalanceBefore;
                balanceAfter = deduct.BalanceAfter;
            }

            var unlock =
                await CreateCandidateUnlockAsync(
                    employerId,
                    request.CandidateId,
                    actionUserId,
                    unlockCost,
                    balanceBefore,
                    balanceAfter,
                    config.CandidateAccessDays);

            await CreateCandidateAccessAsync(
                employerId,
                request.CandidateId,
                unlock.UnlockId,
                config.CandidateAccessDays);

            await CreateCreditUsageTransactionAsync(
                employerId,
                actionUserId,
                request.CandidateId,
                unlock.UnlockId,
                TransactionType.ProfileUnlock,
                unlockCost,
                balanceBefore,
                balanceAfter);

            await _context.SaveChangesAsync();

            return new UnlockCandidateResponseDto
            {
                Success = true,
                Message =
                    "Candidate unlocked successfully.",

                UnlockId =
                    unlock.UnlockId,

                CandidateId =
                    request.CandidateId,

                CreditsDeducted =
                    unlockCost,

                RemainingCredits = wallet.CreditBalance,

                AccessExpiresAt =
                    DateTime.UtcNow.AddDays(
                        config.CandidateAccessDays)
            };
        }
        public async Task<EmployerCandidateProfileDto?> GetCandidateProfileAsync(
        Guid employerId,
        Guid candidateId)
        {
            var candidate =
                await GetCandidateAsync(
                    candidateId);

            if (candidate == null)
                return null;

            var unlocked =
                await HasCandidateAccessAsync(
                    employerId,
                    candidateId);

            var latestCv =
                candidate.Cvs
                    .OrderByDescending(x =>
                        x.GeneratedAt)
                    .FirstOrDefault();

            return new EmployerCandidateProfileDto
            {
                CandidateId =
                    candidate.CandidateId,

                FullName =
                    candidate.FullName,

                ProfilePhotoUrl =
                    candidate.ProfilePhotoUrl,

                PrimaryTrade =
                    candidate.PrimaryTrade,

                TotalExperienceYears =
                    candidate.TotalExperienceYears,

                CurrentCity =
                    candidate.CurrentCity,

                CurrentState =
                    candidate.CurrentState,

                AvailabilityStatus =
                    candidate.AvailabilityStatus,

                IsUnlocked =
                    unlocked,

                Email =
                    unlocked
                        ? candidate.User.Email
                        : null,

                MobileNumber =
                    unlocked
                        ? candidate.User.MobileNumber
                        : null,

                CountryCode =
                    unlocked
                        ? candidate.User.CountryCode
                        : null,

                CvUrl =
                   unlocked
                        ? latestCv?.CvPdfUrl
                        ?? latestCv?.CvFileUrl
                        : null
            };
        }

        public async Task<DownloadCvResponseDto> DownloadCvAsync(
       Guid employerId,
       Guid actionUserId,
       bool isSubUser,
       DownloadCvRequestDto request)
        {
            var permissionCheck = await CheckSubUserPermissionAsync(
                actionUserId,
                isSubUser,
                s => s.CanUnlockProfiles);

            if (!permissionCheck.Allowed)
            {
                return new DownloadCvResponseDto
                {
                    Success = false,
                    Message = permissionCheck.Message
                };
            }

            // Candidate must already be unlocked
            var hasAccess = await HasCandidateAccessAsync(
                employerId,
                request.CandidateId);

            if (!hasAccess)
            {
                return new DownloadCvResponseDto
                {
                    Success = false,
                    Message = "Candidate profile is not unlocked."
                };
            }

            var cv = await GetLatestCandidateCvAsync(request.CandidateId);

            if (cv == null)
            {
                return new DownloadCvResponseDto
                {
                    Success = false,
                    Message = "CV not found."
                };
            }

            // Record download only (NO CREDIT DEDUCTION)
            await CreateCvDownloadRecordAsync(
                request.CandidateId,
                cv.CvId,
                employerId,
                isSubUser ? actionUserId : null,
                0);

            await _context.SaveChangesAsync();

            var wallet = await GetEmployerWalletEntityAsync(employerId);

            return new DownloadCvResponseDto
            {
                Success = true,
                Message = "CV download successful.",

                CandidateId = request.CandidateId,

                CvId = cv.CvId,

                CvUrl = cv.CvPdfUrl ??
                        cv.CvFileUrl ??
                        string.Empty,

                CreditsDeducted = 0,

                RemainingCredits = wallet?.CreditBalance ?? 0
            };
        }

        public async Task<List<CreditUsageHistoryDto>> GetCreditUsageHistoryAsync(Guid employerId)
        {
            var monthCredits =
        await _context.CreditUsageTransactions
        .Where(x =>
            x.EmployerId == employerId &&
            x.CreatedAt.Month == DateTime.UtcNow.Month &&
            x.CreatedAt.Year == DateTime.UtcNow.Year)
        .SumAsync(x => x.CreditsUsed);
            return await _context.CreditUsageTransactions
                .Where(x =>
                    x.EmployerId == employerId)
                .OrderByDescending(x =>
                    x.CreatedAt)
                .Select(x =>
                    new CreditUsageHistoryDto
                    {
                        TransactionId =
                            x.TransactionId,

                        CandidateId =
                            x.CandidateId ?? Guid.Empty,

                        TransactionType =
                            x.TransactionType.ToString(),

                        CreditsUsed =
                            x.CreditsUsed,

                        BalanceBefore =
                            x.BalanceBefore,

                        BalanceAfter =
                            x.BalanceAfter,

                        CreatedAt =
                            x.CreatedAt
                    })

                .ToListAsync();
        }

        public async Task<List<PurchaseHistoryDto>> GetPurchaseHistoryAsync(Guid employerId)
        {
            return await _context.EmployerPlanPurchase
                .Where(x =>
                    x.EmployerId == employerId)
                .OrderByDescending(x =>
                    x.AssignedAt)
                .Select(x =>
                    new PurchaseHistoryDto
                    {
                        PurchaseId =
                            x.EmployerCreditPlanId,

                        PlanId =
                            x.PlanId,

                        PlanName =
                            x.PlanName,

                        Credits =
                            x.Credits,

                        Price =
                            x.Price,

                        AssignedAt =
                            x.AssignedAt,

                        ExpiresAt =
                            x.ExpiresAt,

                        IsActive =
                            x.IsActive
                    })
                .ToListAsync();
        }

        public async Task<List<AllocationHistoryDto>> GetAllocationHistoryAsync(Guid employerId, Guid actionUserId, bool isSubUser)
        {
            // Allocation history shows how the owner divided credits across
            // every sub-user — that's owner-only account administration, not
            // something an individual sub-user should see about their peers.
            if (isSubUser)
            {
                return new List<AllocationHistoryDto>();
            }

            var rows = await _context.CreditAllocationHistory
                .Where(x =>
                    x.EmployerId == employerId)
                .OrderByDescending(x =>
                    x.CreatedAt)
                .Select(x =>
                    new AllocationHistoryDto
                    {
                        HistoryId =
                            x.HistoryId,

                        SubUserId =
                            x.SubUserId,

                        SubUserName =
                            x.SubUserName,

                        CreditsAllocated =
                            x.CreditsAllocated,

                        IsReclaim =
                            x.CreditsAllocated < 0,

                        BalanceBefore =
                            x.BalanceBefore,

                        BalanceAfter =
                            x.BalanceAfter,

                        CreatedAt =
                            x.CreatedAt
                    })
                .ToListAsync();

            // Legacy rows created before SubUserName was snapshotted won't
            // have it set. Sub-user deletion is a soft delete (the
            // EmployerSubUsers row stays forever, just marked "Deleted"),
            // so a live lookup — deliberately not filtered by status — can
            // usually still recover the name even for someone who's since
            // been removed. Only a row whose sub-user predates soft-delete
            // entirely (hard-deleted under old behaviour) stays unresolved.
            var missingNameIds = rows
                .Where(r => string.IsNullOrWhiteSpace(r.SubUserName))
                .Select(r => r.SubUserId)
                .Distinct()
                .ToList();

            if (missingNameIds.Count > 0)
            {
                var namesById = await _context.EmployerSubUsers
                    .AsNoTracking()
                    .Where(x => missingNameIds.Contains(x.SubUserId))
                    .Select(x => new { x.SubUserId, x.SubUserName })
                    .ToDictionaryAsync(x => x.SubUserId, x => x.SubUserName);

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.SubUserName) &&
                        namesById.TryGetValue(row.SubUserId, out var name))
                    {
                        row.SubUserName = name;
                    }
                }
            }

            return rows;
        }

        public async Task<List<CvDownloadHistoryDto>> GetCvDownloadHistoryAsync(Guid employerId)
        {
            return await _context.CandidateCvDownloads
                .Where(x =>
                    x.EmployerId == employerId)
                .OrderByDescending(x =>
                    x.DownloadedAt)
                .Select(x =>
                    new CvDownloadHistoryDto
                    {
                        DownloadId =
                            x.Id,

                        CandidateId =
                            x.CandidateId,

                        CvId =
                            x.CvId,

                        EmployerId =
                            x.EmployerId,

                        SubUserId =
                            x.SubUserId,

                        CreditsUsed =
                            x.CreditsUsed,

                        DownloadedAt =
                            x.DownloadedAt
                    })
                .ToListAsync();
        }

        public async Task<List<UnlockedCandidateDto>> GetUnlockedCandidatesAsync(Guid employerId, Guid actionUserId, bool isSubUser)
        {
            return await
                (
                    from unlock in _context.CandidateUnlocks

                    join candidate in _context.CandidateProfiles
                    on unlock.CandidateId equals candidate.CandidateId

                    where unlock.EmployerId == employerId
                        // A sub-user only ever sees candidates *they*
                        // unlocked — not every unlock across the company.
                        && (!isSubUser || unlock.UnlockRequestedBy == actionUserId)

                    orderby unlock.UnlockTimestamp descending

                    select new UnlockedCandidateDto
                    {
                        UnlockId =
                            unlock.UnlockId,

                        CandidateId =
                            unlock.CandidateId,

                        CandidateName =
                            candidate.FullName,

                        Trade =
                            candidate.PrimaryTrade,

                        ExperienceYears =
                            candidate.TotalExperienceYears,

                        CreditsDeducted =
                            unlock.CreditsDeducted,

                        UnlockTimestamp =
                            unlock.UnlockTimestamp,

                        UnlockExpiryDate =
                            unlock.UnlockExpiryDate,

                        CvDownloadAllowed =
                            unlock.CvDownloadAllowed
                    }
                )
                .ToListAsync();
        }

        public async Task<List<EmployerTransactionHistoryDto>> GetEmployerTransactionHistoryAsync(Guid employerId, Guid actionUserId, bool isSubUser)
        {
            var creditTransactions =
                await
                (
                    from t in _context.CreditUsageTransactions

                    join c in _context.CandidateProfiles
                    on t.CandidateId equals c.CandidateId
                    into candidateJoin

                    from candidate in candidateJoin.DefaultIfEmpty()

                    where t.EmployerId == employerId
                        // A sub-user only ever sees their own credit usage —
                        // not the whole company's activity.
                        && (!isSubUser || t.ActionByUserId == actionUserId)

                    select new EmployerTransactionHistoryDto
                    {
                        TransactionId =
                            t.TransactionId,

                        TransactionType =
                            t.TransactionType.ToString(),

                        Category =
                            "Credit",

                        CandidateId =
                            t.CandidateId,

                        CandidateName =
                            candidate != null
                                ? candidate.FullName
                                : null,

                        CreditsUsed =
                            t.CreditsUsed,

                        AmountPaid =
                            null,

                        PlanName =
                            null,

                        CreatedAt =
                            t.CreatedAt,

                        ActionByUserId =
                            t.ActionByUserId,

                        ActionByName =
                            t.ActionByName,

                        ActionByRole =
                            t.ActionByRole
                    }
                )
                .ToListAsync();

            List<EmployerTransactionHistoryDto> allRows;

            // Plan purchases are company-wide billing events made by the
            // account owner — a sub-user has no purchasing access, so they
            // shouldn't see this in their own history either.
            if (isSubUser)
            {
                allRows = creditTransactions;
            }
            else
            {
                var owner = await _context.EmployerProfiles
                    .AsNoTracking()
                    .Where(x => x.EmployerId == employerId)
                    .Select(x => new { x.UserId, x.ContactPersonName, x.CompanyDisplayName })
                    .FirstOrDefaultAsync();

                var ownerName = owner != null && !string.IsNullOrWhiteSpace(owner.ContactPersonName)
                    ? owner.ContactPersonName
                    : owner?.CompanyDisplayName;

                // Expression trees (which this Select is, since it's
                // chained on IQueryable and gets translated to SQL) can't
                // contain the null-conditional operator — so resolve
                // owner?.UserId to a plain nullable value first.
                Guid? ownerUserIdOrNull = owner?.UserId;

                var purchases =
                    await _context.EmployerPlanPurchase
                        .Where(x =>
                            x.EmployerId == employerId)
                        .Select(x =>
                            new EmployerTransactionHistoryDto
                            {
                                TransactionId =
                                    x.EmployerCreditPlanId,

                                TransactionType =
                                    "PlanPurchase",

                                Category =
                                    "Plan",

                                CandidateId =
                                    null,

                                CandidateName =
                                    null,

                                PlanName =
                                    x.PlanName,

                                CreditsUsed =
                                    x.Credits,

                                AmountPaid =
                                    x.Price,

                                CreatedAt =
                                    x.AssignedAt,

                                // NOTE: EmployerPlanPurchase.AssignedBy is
                                // historically populated with the EmployerId,
                                // not the owner's own UserId — it can never
                                // match owner.UserId in PopulateActionByDetailsAsync.
                                // Only a sub-user's shared wallet can even reach
                                // this code path (isSubUser branch above skips
                                // purchases entirely), and only the account owner
                                // can buy credits in the first place — so every
                                // row here is unambiguously theirs. Resolve it
                                // directly rather than depending on that id match.
                                ActionByUserId =
                                    ownerUserIdOrNull ?? x.AssignedBy,

                                ActionByName =
                                    ownerName,

                                ActionByRole =
                                    "Account Owner"
                            })
                        .ToListAsync();

                allRows = creditTransactions.Concat(purchases).ToList();
            }

            await PopulateActionByDetailsAsync(employerId, allRows);

            return allRows
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        // Resolves ActionByUserId → a display name and role ("Account
        // Owner" or the sub-user's role) for rows that don't already carry
        // a snapshotted name (PlanPurchase rows, and any CreditUsageTransaction
        // row created before the ActionByName/ActionByRole columns existed).
        // Rows with a snapshot already set are left untouched — that's the
        // name as it was at the time, which stays correct even if the
        // sub-user is deleted later.
        private async Task PopulateActionByDetailsAsync(
            Guid employerId,
            List<EmployerTransactionHistoryDto> rows)
        {
            var unresolvedRows = rows
                .Where(r => string.IsNullOrWhiteSpace(r.ActionByName))
                .ToList();

            if (unresolvedRows.Count == 0) return;

            var owner = await _context.EmployerProfiles
                .AsNoTracking()
                .Where(x => x.EmployerId == employerId)
                .Select(x => new { x.UserId, x.ContactPersonName, x.CompanyDisplayName })
                .FirstOrDefaultAsync();

            var subUsers = await _context.EmployerSubUsers
                .AsNoTracking()
                .Where(x => x.EmployerId == employerId)
                .Select(x => new { x.UserId, x.SubUserName, x.SubUserRole })
                .ToListAsync();

            var subUserLookup = subUsers
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.First());

            foreach (var row in unresolvedRows)
            {
                if (owner != null && row.ActionByUserId == owner.UserId)
                {
                    row.ActionByName = !string.IsNullOrWhiteSpace(owner.ContactPersonName)
                        ? owner.ContactPersonName
                        : owner.CompanyDisplayName;
                    row.ActionByRole = "Account Owner";
                }
                else if (subUserLookup.TryGetValue(row.ActionByUserId, out var subUser))
                {
                    row.ActionByName = subUser.SubUserName;
                    row.ActionByRole = subUser.SubUserRole;
                }
                else
                {
                    // The row's actor genuinely can't be resolved — their
                    // EmployerSubUsers row doesn't exist at all anymore
                    // (hard-deleted under the old delete behaviour, before
                    // deletion became a soft delete). There's no name left
                    // to recover for these specific legacy rows.
                    row.ActionByName = "Former sub-user";
                    row.ActionByRole = "";
                }
            }
        }
        public async Task<CreditWalletDashboardDto> GetCreditWalletDashboardAsync(Guid employerId)
        {
            var wallet =
                await GetEmployerWalletEntityAsync(employerId);

            var creditsUsedThisMonth =
                await _context.CreditUsageTransactions
                    .Where(x =>
                        x.EmployerId == employerId &&
                        x.CreatedAt.Month == DateTime.UtcNow.Month &&
                        x.CreatedAt.Year == DateTime.UtcNow.Year)
                    .SumAsync(x =>
                        x.CreditsUsed);

            var profilesUnlocked =
                await _context.CandidateUnlocks
                    .CountAsync(x =>
                        x.EmployerId == employerId);

            var totalSubUsers =
                await _context.EmployerSubUsers
                    .CountAsync(x =>
                        x.EmployerId == employerId &&
                        x.SubUserStatus == "Active");

            // How much of the shared pool is currently sitting in sub-user
            // allocations (handed out, still theirs to spend) vs. how much
            // is untouched and free for the owner to hand out next.
            var allocatedToSubUsers =
                await _context.SubUserCreditAllocation
                    .Where(x =>
                        x.EmployerId == employerId)
                    .SumAsync(x =>
                        (int?)x.RemainingCredits) ?? 0;

            var remainingCredits = wallet?.CreditBalance ?? 0;

            return new CreditWalletDashboardDto
            {
                RemainingCredits =
                    remainingCredits,

                PlanName =
                    wallet?.PackageName,

                PlanExpiryDate =
                    wallet?.PackExpiresAt,

                CreditsUsedThisMonth =
                    creditsUsedThisMonth,

                ProfilesUnlocked =
                    profilesUnlocked,

                SharedWalletEnabled =
                    wallet?.SharedWallet ?? false,

                TotalSubUsers =
                    totalSubUsers,

                AllocatedToSubUsers =
                    allocatedToSubUsers,

                AvailableToAllocate =
                    remainingCredits - allocatedToSubUsers
            };
        }

        private async Task<CreditConfiguration?> GetCreditConfigurationAsync()
        {
            return await _context.CreditConfigurations
                .FirstOrDefaultAsync(x => x.IsActive);
        }

        private async Task<CreditWallet?> GetEmployerWalletEntityAsync(Guid employerId)
        {
            var wallet = await _context.CreditWallets
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId);

            if (wallet != null)
                await ReconcileWalletBalanceAsync(wallet);

            return wallet;
        }

        /// <summary>
        /// Recomputes the wallet's true remaining balance from the
        /// immutable ledger — total credits ever granted (plan purchases)
        /// minus total credits ever used (by the owner AND every sub-user
        /// combined) — and corrects wallet.CreditBalance in place if it's
        /// drifted from that. This makes the balance self-healing: any past
        /// inconsistency (e.g. a period where sub-user spending wasn't being
        /// debited from the shared wallet) fixes itself the next time the
        /// wallet is read, rather than requiring a manual data correction.
        /// </summary>
        private async Task ReconcileWalletBalanceAsync(CreditWallet wallet)
        {
            var totalGranted = await _context.EmployerPlanPurchase
                .Where(x => x.EmployerId == wallet.EmployerId)
                .SumAsync(x => (int?)x.Credits) ?? 0;

            var totalUsed = await _context.CreditUsageTransactions
                .Where(x => x.EmployerId == wallet.EmployerId)
                .SumAsync(x => (int?)x.CreditsUsed) ?? 0;

            var trueRemaining = totalGranted - totalUsed;

            if (wallet.CreditBalance != trueRemaining)
            {
                wallet.CreditBalance = trueRemaining;
                wallet.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task<SubUserCreditAllocation?> GetSubUserAllocationAsync(Guid subUserId)
        {
            return await _context.SubUserCreditAllocation
                .FirstOrDefaultAsync(x =>
                    x.SubUserId == subUserId);
        }

        private async Task<CandidateProfile?> GetCandidateAsync(Guid candidateId)
        {
            Console.WriteLine($"Searching Candidate: {candidateId}");

            var candidate = await _context.CandidateProfiles
                .Include(x => x.User)
                .Include(x => x.Cvs)
                .FirstOrDefaultAsync(x => x.CandidateId == candidateId);

            Console.WriteLine(candidate == null
                ? "NOT FOUND"
                : "FOUND");

            return candidate;
        }

        public async Task<WatermarkedCvResult> DownloadWatermarkedCvAsync(
            Guid employerId,
            Guid candidateId)
        {
            // 1. Profile must be unlocked (active access, not expired)
            var hasAccess = await _context.EmployerCandidateAccesses
                .AnyAsync(x =>
                    x.EmployerId == employerId &&
                    x.CandidateId == candidateId &&
                    x.IsActive &&
                    x.ExpiresAt > DateTime.UtcNow);

            if (!hasAccess)
                return new WatermarkedCvResult
                {
                    Success = false,
                    Message = "Profile is locked. Unlock this candidate to download their CV."
                };

            // 2. Latest CV: prefer the portal-generated CV (reflects current
            //    profile data) if the candidate has generated one; otherwise
            //    fall back to the originally uploaded resume.
            var candidateProfile = await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            string? cvFileUrl = candidateProfile?.GeneratedCvFileUrl;
            bool isGeneratedCv = !string.IsNullOrWhiteSpace(cvFileUrl);

            if (string.IsNullOrWhiteSpace(cvFileUrl))
            {
                // Only a genuinely uploaded-and-parsed resume counts as a
                // fallback here — not a synthetic stub row (see
                // GetLatestCandidateCvAsync) created purely to satisfy the
                // credit-download-history foreign key. Stub rows can go
                // stale the moment the Portal CV is regenerated again, since
                // nothing updates them in place; AffindaJobId is only ever
                // set on a real upload, so it's a safe way to tell them apart.
                var cv = await _context.CandidateCvs
                    .Where(c => c.CandidateId == candidateId
                        && c.CvFileUrl != null
                        && c.AffindaJobId != null)
                    .OrderByDescending(c => c.GeneratedAt)
                    .FirstOrDefaultAsync();

                cvFileUrl = cv?.CvFileUrl;
                isGeneratedCv = false;
            }

            if (string.IsNullOrWhiteSpace(cvFileUrl) && candidateProfile != null)
            {
                // Nothing uploaded and nothing generated yet — build a Portal
                // CV from whatever profile data the candidate has filled in
                // rather than telling the employer there's nothing to give them.
                var generated = await _cvGeneration.GenerateCvAsync(candidateId);

                if (generated.Success && !string.IsNullOrWhiteSpace(generated.GeneratedCvUrl))
                {
                    cvFileUrl = generated.GeneratedCvUrl;
                    isGeneratedCv = true;
                }
            }

            if (string.IsNullOrWhiteSpace(cvFileUrl))
                return new WatermarkedCvResult
                {
                    Success = false,
                    Message = "No CV is available for this candidate."
                };

            // 3. Names for the watermark + filename
            var candidateName = await _context.CandidateProfiles
                .Where(p => p.CandidateId == candidateId)
                .Select(p => p.FullName)
                .FirstOrDefaultAsync() ?? "Candidate";

            var companyName = await _context.EmployerProfiles
                .Where(e => e.EmployerId == employerId)
                .Select(e => e.CompanyDisplayName)
                .FirstOrDefaultAsync() ?? "Company";

            // 4. Build the watermarked PDF entirely in memory.
            //    Nothing is written to disk or storage — the bytes are
            //    streamed to the recruiter and then garbage-collected.
            byte[] bytes;
            try
            {
                bytes = await _watermark.AddWatermarkAsync(
                    cvFileUrl,
                    companyName,
                    employerId,
                    DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                // Whatever file we resolved above — a previously generated
                // Portal CV, or a genuinely uploaded resume — isn't readable
                // right now (e.g. it was generated by a different app
                // instance / build config, or the uploads folder was
                // cleared). Since the candidate's profile data is always
                // available, rebuild a Portal CV from it on the spot rather
                // than failing the download outright.
                _logger.LogWarning(
                    ex,
                    "CV file unreadable for candidate {CandidateId} (source: {Source}); regenerating a Portal CV from profile data.",
                    candidateId,
                    isGeneratedCv ? "previously generated" : "uploaded resume");

                var regenerated = await _cvGeneration.GenerateCvAsync(candidateId);

                if (!regenerated.Success || string.IsNullOrWhiteSpace(regenerated.GeneratedCvUrl))
                {
                    return new WatermarkedCvResult
                    {
                        Success = false,
                        Message = "This candidate's CV file could not be found and a Portal CV could not be generated from their profile. Please ask them to complete their profile or re-upload their resume."
                    };
                }

                try
                {
                    bytes = await _watermark.AddWatermarkAsync(
                        regenerated.GeneratedCvUrl,
                        companyName,
                        employerId,
                        DateTime.UtcNow);
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(
                        retryEx,
                        "Portal CV still unreadable for candidate {CandidateId} after regeneration.",
                        candidateId);

                    return new WatermarkedCvResult
                    {
                        Success = false,
                        Message = "Unable to prepare this candidate's CV for download. Please try again."
                    };
                }
            }

            var safeName = new string(
                candidateName.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ').ToArray())
                .Trim()
                .Replace(' ', '_');

            if (string.IsNullOrWhiteSpace(safeName))
                safeName = "Candidate";

            return new WatermarkedCvResult
            {
                Success = true,
                FileBytes = bytes,
                FileName = $"{safeName}_CV.pdf"
            };
        }

        private async Task<bool> HasCandidateAccessAsync(Guid employerId, Guid candidateId)
        {
            return await _context.EmployerCandidateAccesses
                .AnyAsync(x =>
                    x.EmployerId == employerId &&
                    x.CandidateId == candidateId &&
                    x.IsActive &&
                    x.ExpiresAt > DateTime.UtcNow);
        }

        // ────────────────────────────────────────────────────────────
        // Enforces a sub-user's ACTUAL permission flags from the DB
        // before letting a credit-consuming action through. isSubUser
        // and actionUserId still come from client-supplied headers
        // (a broader, separate concern), but whatever the client
        // claims, this looks the sub-user up fresh and checks their
        // real, current status and permission — a deactivated or
        // under-permissioned sub-user can never slip through just
        // because the request says otherwise.
        // ────────────────────────────────────────────────────────────
        private async Task<(bool Allowed, string Message)> CheckSubUserPermissionAsync(
            Guid actionUserId,
            bool isSubUser,
            Func<EmployerSubUser, bool> requiredPermission)
        {
            if (!isSubUser)
            {
                return (true, string.Empty);
            }

            var subUser = await _context.EmployerSubUsers
                .FirstOrDefaultAsync(s => s.UserId == actionUserId);

            if (subUser == null)
            {
                return (false, "Sub-user account not found.");
            }

            if (subUser.SubUserStatus == "Deactivated")
            {
                return (false, "This sub-user account has been deactivated.");
            }

            if (!subUser.InviteAccepted)
            {
                return (false, "This sub-user has not accepted their invitation yet.");
            }

            if (!requiredPermission(subUser))
            {
                return (false, "You don't have permission to perform this action.");
            }

            return (true, string.Empty);
        }

        private async Task<EmployerCandidateAccess?> GetCandidateAccessAsync(Guid employerId, Guid candidateId)
        {
            return await _context.EmployerCandidateAccesses
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId &&
                    x.CandidateId == candidateId &&
                    x.IsActive &&
                    x.ExpiresAt > DateTime.UtcNow);
        }
        private async Task<EmployerSubUser?> GetSubUserAsync(Guid subUserId)
        {
            return await _context.EmployerSubUsers
                .FirstOrDefaultAsync(x =>
                    x.SubUserId == subUserId &&
                    x.SubUserStatus == "Active");
        }

        private async Task<(bool Success, string Message, int BalanceBefore, int BalanceAfter)> DeductEmployerCreditsAsync(
        Guid employerId,
        int credits)
        {
            var wallet =
                await GetEmployerWalletEntityAsync(
                    employerId);

            if (wallet == null)
            {
                return (
                    false,
                    "Wallet not found.",
                    0,
                    0);
            }

            if (wallet.CreditBalance < credits)
            {
                return (
                    false,
                    "Insufficient credits.",
                    wallet.CreditBalance,
                    wallet.CreditBalance);
            }

            var before = wallet.CreditBalance;

            wallet.CreditBalance -= credits;

            wallet.UpdatedAt =
                DateTime.UtcNow;

            return (
                true,
                "Credits deducted.",
                before,
                wallet.CreditBalance);
        }

        private async Task<(bool Success,
    string Message,
    int BalanceBefore,
    int BalanceAfter)>
    DeductSubUserCreditsAsync(
        Guid subUserId,
        int credits)
        {
            var allocation =
                await GetSubUserAllocationAsync(
                    subUserId);

            if (allocation == null)
            {
                return (
                    false,
                    "Credit allocation not found.",
                    0,
                    0);
            }

            if (allocation.RemainingCredits < credits)
            {
                return (
                    false,
                    "Insufficient allocated credits.",
                    allocation.RemainingCredits,
                    allocation.RemainingCredits);
            }

            // Sub-user credits are drawn from the same shared company pool —
            // the allocation is just a per-sub-user quota/permission ledger
            // on top of it. Every spend has to come off both: the
            // allocation (so the sub-user's own remaining quota shrinks)
            // and the shared wallet (so the owner's total remaining
            // reflects everyone's usage, not just their own).
            var wallet =
                await GetEmployerWalletEntityAsync(
                    allocation.EmployerId);

            if (wallet == null)
            {
                return (
                    false,
                    "Wallet not found.",
                    allocation.RemainingCredits,
                    allocation.RemainingCredits);
            }

            if (wallet.CreditBalance < credits)
            {
                return (
                    false,
                    "Insufficient credits.",
                    allocation.RemainingCredits,
                    allocation.RemainingCredits);
            }

            var before =
                allocation.RemainingCredits;

            allocation.RemainingCredits -= credits;

            allocation.UsedCredits += credits;

            allocation.UpdatedAt =
                DateTime.UtcNow;

            wallet.CreditBalance -= credits;

            wallet.UpdatedAt =
                DateTime.UtcNow;

            return (
                true,
                "Credits deducted.",
                before,
                allocation.RemainingCredits);
        }

        private async Task CreateCreditUsageTransactionAsync(
    Guid employerId,
    Guid actionByUserId,
    Guid? candidateId,
    Guid? unlockId,
    TransactionType transactionType,
    int creditsUsed,
    int balanceBefore,
    int balanceAfter)
        {
            var (actionByName, actionByRole) =
                await ResolveActionByDetailsAsync(employerId, actionByUserId);

            var transaction =
                new CreditUsageTransaction
                {
                    TransactionId = Guid.NewGuid(),

                    EmployerId = employerId,

                    ActionByUserId =
                        actionByUserId,

                    ActionByName =
                        actionByName,

                    ActionByRole =
                        actionByRole,

                    CandidateId =
                        candidateId,

                    UnlockId =
                        unlockId,

                    TransactionType =
                        transactionType,

                    CreditsUsed =
                        creditsUsed,

                    BalanceBefore =
                        balanceBefore,

                    BalanceAfter =
                        balanceAfter,

                    CreatedAt =
                        DateTime.UtcNow
                };

            await _context.CreditUsageTransactions
                .AddAsync(transaction);
        }

        /// <summary>
        /// Resolves a single actor's display name + role ("Account Owner"
        /// or the sub-user's role) at the moment an action happens, so it
        /// can be permanently snapshotted onto the record — see
        /// PopulateActionByDetailsAsync for the batch/read-time version of
        /// this same lookup used for legacy rows that predate the snapshot.
        /// </summary>
        private async Task<(string? Name, string? Role)> ResolveActionByDetailsAsync(
            Guid employerId,
            Guid actionByUserId)
        {
            var owner = await _context.EmployerProfiles
                .AsNoTracking()
                .Where(x => x.EmployerId == employerId)
                .Select(x => new { x.UserId, x.ContactPersonName, x.CompanyDisplayName })
                .FirstOrDefaultAsync();

            if (owner != null && actionByUserId == owner.UserId)
            {
                var name = !string.IsNullOrWhiteSpace(owner.ContactPersonName)
                    ? owner.ContactPersonName
                    : owner.CompanyDisplayName;

                return (name, "Account Owner");
            }

            var subUser = await _context.EmployerSubUsers
                .AsNoTracking()
                .Where(x => x.EmployerId == employerId && x.UserId == actionByUserId)
                .Select(x => new { x.SubUserName, x.SubUserRole })
                .FirstOrDefaultAsync();

            return subUser != null
                ? (subUser.SubUserName, subUser.SubUserRole)
                : (null, null);
        }

        private async Task<EmployerCandidateAccess>
    CreateCandidateAccessAsync(
        Guid employerId,
        Guid candidateId,
        Guid unlockId,
        int accessDays)
        {
            var access =
                new EmployerCandidateAccess
                {
                    AccessId =
                        Guid.NewGuid(),

                    EmployerId =
                        employerId,

                    CandidateId =
                        candidateId,

                    UnlockId =
                        unlockId,

                    GrantedAt =
                        DateTime.UtcNow,

                    ExpiresAt =
                        DateTime.UtcNow.AddDays(
                            accessDays),

                    IsActive = true
                };

            await _context
                .EmployerCandidateAccesses
                .AddAsync(access);

            return access;
        }

        private async Task<CandidateUnlock>
    CreateCandidateUnlockAsync(
        Guid employerId,
        Guid candidateId,
        Guid requestedBy,
        int creditsDeducted,
        int balanceBefore,
        int balanceAfter,
        int accessDays)
        {
            var unlock =
                new CandidateUnlock
                {
                    UnlockId =
                        Guid.NewGuid(),

                    EmployerId =
                        employerId,

                    CandidateId =
                        candidateId,

                    UnlockRequestedBy =
                        requestedBy,

                    CreditsDeducted =
                        creditsDeducted,

                    UnlockTimestamp =
                        DateTime.UtcNow,

                    UnlockExpiryDate =
                        DateOnly.FromDateTime(
                            DateTime.UtcNow
                                .AddDays(accessDays)),

                    WalletBalanceBefore =
                        balanceBefore,

                    WalletBalanceAfter =
                        balanceAfter,

                    UnlockStatus =
                        "Unlocked",

                    CvDownloadAllowed =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };

            await _context.CandidateUnlocks
                .AddAsync(unlock);

            return unlock;
        }

        private async Task<CandidateCv?>
    GetLatestCandidateCvAsync(
        Guid candidateId)
        {
            var existing = await _context.CandidateCvs
                .Where(x =>
                    x.CandidateId == candidateId)
                .OrderByDescending(x =>
                    x.GeneratedAt)
                .FirstOrDefaultAsync();

            if (existing != null)
                return existing;

            // No uploaded resume on file — fall back to the auto-generated
            // Portal CV (built from the candidate's profile data) so an
            // employer can still download something for candidates who
            // filled out their profile but never uploaded a physical file.
            var profile = await _context.CandidateProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.CandidateId == candidateId);

            if (profile == null)
                return null;

            var generatedCvUrl = profile.GeneratedCvFileUrl;

            if (string.IsNullOrWhiteSpace(generatedCvUrl))
            {
                // Nobody has ever generated a Portal CV for this candidate —
                // usually because their profile predates the auto-generate-
                // on-save behaviour. Whatever profile data they do have
                // (personal info, work history, education, skills) is
                // enough to build one now rather than telling the employer
                // there's simply nothing to download.
                var generated = await _cvGeneration.GenerateCvAsync(candidateId);

                if (!generated.Success || string.IsNullOrWhiteSpace(generated.GeneratedCvUrl))
                    return null;

                generatedCvUrl = generated.GeneratedCvUrl;
            }

            var cvRecord = new CandidateCv
            {
                CvId = Guid.NewGuid(),
                CandidateId = candidateId,
                CvFileUrl = generatedCvUrl,
                ParsedName = profile.FullName,
                GeneratedAt = profile.GeneratedCvUpdatedAt ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.CandidateCvs.AddAsync(cvRecord);
            await _context.SaveChangesAsync();

            return cvRecord;
        }

        private async Task CreateCvDownloadRecordAsync(
        Guid candidateId,
        Guid cvId,
        Guid employerId,
        Guid? subUserId,
        int creditsUsed)
        {
            var download =
                new CandidateCvDownload
                {
                    Id = Guid.NewGuid(),

                    CandidateId =
                        candidateId,

                    CvId =
                        cvId,

                    EmployerId =
                        employerId,

                    SubUserId =
                        subUserId,

                    CreditsUsed =
                        creditsUsed,

                    DownloadedAt =
                        DateTime.UtcNow
                };

            await _context.CandidateCvDownloads
                .AddAsync(download);
        }
    }
}