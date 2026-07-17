using JobPortal.Application.DTOs.Recruiter.CreditWallet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.IImplement.IRecruiter
{

    public interface ICreditWalletService
    {
        // ==========================================
        // Wallet
        // ==========================================

        Task<WalletSummaryDto?> GetEmployerWalletAsync(
                Guid employerId);

        // ==========================================
        // Credit Allocation
        // ==========================================

        Task<AllocateCreditsResponseDto> AllocateCreditsAsync(
                Guid employerId,
                AllocateCreditsRequestDto request);

        Task<SubUserCreditBalanceDto?> GetSubUserCreditBalanceAsync(Guid subUserId);

        // ==========================================
        // Candidate Unlock
        // ==========================================

        Task<UnlockCandidateResponseDto> UnlockCandidateAsync(
                Guid employerId,
                Guid actionUserId,
                bool isSubUser,
                UnlockCandidateRequestDto request);

        // ==========================================
        // Candidate Profile Access
        // ==========================================

        Task<EmployerCandidateProfileDto?> GetCandidateProfileAsync(
                Guid employerId,
                Guid candidateId);

        // ==========================================
        // CV Download
        // ==========================================

        Task<DownloadCvResponseDto> DownloadCvAsync(
                Guid employerId,
                Guid actionUserId,
                bool isSubUser,
                DownloadCvRequestDto request);

        /// <summary>
        /// Streams the candidate's CV with a per-employer watermark
        /// (company name + download date) applied in memory. Only succeeds
        /// when the profile is unlocked for the employer. Nothing is stored.
        /// </summary>
        Task<WatermarkedCvResult> DownloadWatermarkedCvAsync(
                Guid employerId,
                Guid candidateId);


        Task<List<CreditUsageHistoryDto>> GetCreditUsageHistoryAsync(Guid employerId);

        Task<List<PurchaseHistoryDto>> GetPurchaseHistoryAsync(Guid employerId);

        Task<List<AllocationHistoryDto>> GetAllocationHistoryAsync(Guid employerId, Guid actionUserId, bool isSubUser);

        Task<List<CvDownloadHistoryDto>> GetCvDownloadHistoryAsync(Guid employerId);

        Task<List<UnlockedCandidateDto>> GetUnlockedCandidatesAsync(Guid employerId, Guid actionUserId, bool isSubUser);

        Task<List<EmployerTransactionHistoryDto>> GetEmployerTransactionHistoryAsync(Guid employerId, Guid actionUserId, bool isSubUser);

        Task<CreditWalletDashboardDto> GetCreditWalletDashboardAsync(Guid employerId);
    }
}