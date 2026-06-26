// File: JobPortal.Services/IImplement/IRecruiter/IRecruiterCreditPlanService.cs

using JobPortal.Application.DTOs.Admin.CreditWallet;
using JobPortal.Application.DTOs.Recruiter;

namespace JobPortal.Services.IImplement.IRecruiter
{
    public interface IRecruiterCreditPlanService
    {
        /// <summary>
        /// Returns all active plans a recruiter can purchase.
        /// </summary>
        Task<List<CreditPlanResponseDto>> GetActivePlansAsync();

        /// <summary>
        /// Creates a Razorpay order for the selected plan.
        /// UserId is resolved internally from EmployerId — caller does NOT pass it.
        /// </summary>
        Task<CreatePlanOrderResponseDto> CreatePlanOrderAsync(
            Guid employerId,
            CreatePlanOrderRequestDto request);

        /// <summary>
        /// Verifies Razorpay signature, credits the wallet, records the purchase.
        /// </summary>
        Task<VerifyPlanPaymentResponseDto> VerifyPlanPaymentAsync(
            Guid employerId,
            VerifyPlanPaymentRequestDto request);

        // Legacy admin-assign (kept for backward compat)
        Task<CommonResponseDto> BuyPlanAsync(Guid employerId, Guid planId);
    }
}