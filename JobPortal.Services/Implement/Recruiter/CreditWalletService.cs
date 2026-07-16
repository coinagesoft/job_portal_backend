using JobPortal.Application.DTOs.JobPosting;
using JobPortal.Application.DTOs.Recruiter.CreditWallet;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums.RecruiterEnums;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        public CreditWalletService(
            IConfiguration configuration,
            AppDbContext context,
            IResumeWatermarkService watermark)
        {
            _configuration = configuration;
            _context = context;
            _watermark = watermark;
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
                actionUserId, isSubUser, s => s.CanUnlockProfiles);

            if (!permissionCheck.Allowed)
            {
                return new DownloadCvResponseDto
                {
                    Success = false,
                    Message = permissionCheck.Message
                };
            }

            var hasAccess =
                await HasCandidateAccessAsync(
                    employerId,
                    request.CandidateId);

            if (!hasAccess)
            {
                return new DownloadCvResponseDto
                {
                    Success = false,
                    Message =
                        "Candidate profile is not unlocked."
                };
            }

            var cv =
                await GetLatestCandidateCvAsync(
                    request.CandidateId);

            if (cv == null)
            {
                return new DownloadCvResponseDto
                {
                    Success = false,
                    Message = "CV not found."
                };
            }

            var config =
                await GetCreditConfigurationAsync();

            var wallet = await GetEmployerWalletEntityAsync(employerId);

            if (wallet == null)
            {
                return new DownloadCvResponseDto
                {
                    Success = false,
                    Message = "Wallet not found."
                };
            }

            if (wallet.PackExpiresAt.HasValue &&
                wallet.PackExpiresAt.Value < DateTime.UtcNow)
            {
                return new DownloadCvResponseDto
                {
                    Success = false,
                    Message =
                        "Credit package has expired. Please purchase a new package."
                };
            }

            if (config == null)
            {
                return new DownloadCvResponseDto
                {
                    Success = false,
                    Message =
                        "Credit configuration not found."
                };
            }

            var cvCost = config.CvDownloadCredits;

            int balanceBefore;
            int balanceAfter;

            if (isSubUser)
            {
                var deduct = await DeductSubUserCreditsAsync(
                        actionUserId,
                        cvCost);

                if (!deduct.Success)
                {
                    return new DownloadCvResponseDto
                    {
                        Success = false,
                        Message =
                            deduct.Message
                    };
                }

                balanceBefore =
                    deduct.BalanceBefore;

                balanceAfter =
                    deduct.BalanceAfter;
            }
            else
            {
                var deduct =
                    await DeductEmployerCreditsAsync(
                        employerId,
                        cvCost);

                if (!deduct.Success)
                {
                    return new DownloadCvResponseDto
                    {
                        Success = false,
                        Message =
                            deduct.Message
                    };
                }

                balanceBefore =
                    deduct.BalanceBefore;

                balanceAfter =
                    deduct.BalanceAfter;
            }

            await CreateCvDownloadRecordAsync(
                request.CandidateId,
                cv.CvId,
                employerId,
                isSubUser
                    ? actionUserId
                    : null,
                cvCost);

            await CreateCreditUsageTransactionAsync(
                employerId,
                actionUserId,
                request.CandidateId,
                null,
                TransactionType.CvDownload,
                cvCost,
                balanceBefore,
                balanceAfter);

            await _context.SaveChangesAsync();

            return new DownloadCvResponseDto
            {
                Success = true,
                Message =
                    "CV download successful.",

                CandidateId =
                    request.CandidateId,

                CvId =
                    cv.CvId,

                CvUrl =
                    cv.CvPdfUrl ??
                    cv.CvFileUrl ??
                    string.Empty,

                CreditsDeducted =
                    cvCost,

                RemainingCredits =
                    balanceAfter
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

            return await _context.CreditAllocationHistory
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

                        CreditsAllocated =
                            x.CreditsAllocated,

                        BalanceBefore =
                            x.BalanceBefore,

                        BalanceAfter =
                            x.BalanceAfter,

                        CreatedAt =
                            x.CreatedAt
                    })
                .ToListAsync();
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
                            t.ActionByUserId
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

                                ActionByUserId =
                                    x.AssignedBy
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
        // Owner" or the sub-user's role) for a batch of transaction rows,
        // so the owner can see exactly which user used credits.
        private async Task PopulateActionByDetailsAsync(
            Guid employerId,
            List<EmployerTransactionHistoryDto> rows)
        {
            if (rows.Count == 0) return;

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

            foreach (var row in rows)
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
                    row.ActionByName = "Unknown user";
                    row.ActionByRole = "";
                }
            }
        }
        public async Task<CreditWalletDashboardDto> GetCreditWalletDashboardAsync(Guid employerId)
        {
            var wallet =
                await _context.CreditWallets
                    .FirstOrDefaultAsync(x =>
                        x.EmployerId == employerId);

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

            return new CreditWalletDashboardDto
            {
                RemainingCredits =
                    wallet?.CreditBalance ?? 0,

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
                    totalSubUsers
            };
        }

        private async Task<CreditConfiguration?> GetCreditConfigurationAsync()
        {
            return await _context.CreditConfigurations
                .FirstOrDefaultAsync(x => x.IsActive);
        }

        private async Task<CreditWallet?> GetEmployerWalletEntityAsync(Guid employerId)
        {
            return await _context.CreditWallets
                .FirstOrDefaultAsync(x =>
                    x.EmployerId == employerId);
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

            if (string.IsNullOrWhiteSpace(cvFileUrl))
            {
                var cv = await _context.CandidateCvs
                    .Where(c => c.CandidateId == candidateId && c.CvFileUrl != null)
                    .OrderByDescending(c => c.GeneratedAt)
                    .FirstOrDefaultAsync();

                cvFileUrl = cv?.CvFileUrl;
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
            var bytes = await _watermark.AddWatermarkAsync(
                cvFileUrl,
                companyName,
                employerId,
                DateTime.UtcNow);

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

            var before =
                allocation.RemainingCredits;

            allocation.RemainingCredits -= credits;

            allocation.UsedCredits += credits;

            allocation.UpdatedAt =
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
            var transaction =
                new CreditUsageTransaction
                {
                    TransactionId = Guid.NewGuid(),

                    EmployerId = employerId,

                    ActionByUserId =
                        actionByUserId,

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
            return await _context.CandidateCvs
                .Where(x =>
                    x.CandidateId == candidateId)
                .OrderByDescending(x =>
                    x.GeneratedAt)
                .FirstOrDefaultAsync();
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