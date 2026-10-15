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
        Task<List<CreditPlanResponseDto>> GetActivePlansAsync(string? region = null);

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

        /// <summary>
        /// Creates a PayPal order. visitorRegion is the pricing region resolved
        /// server-side from the caller's IP; the plan must belong to that region.
        /// </summary>
        Task<CreatePlanOrderResponseDto> CreatePayPalPlanOrderAsync(
            Guid employerId,
            CreatePlanOrderRequestDto request,
            string visitorRegion);

        Task<VerifyPlanPaymentResponseDto> VerifyPayPalPlanPaymentAsync(
            Guid employerId,
            VerifyPlanPaymentRequestDto request);

        /// <summary>
        /// Webhook backstop: completes a PayPal purchase whose browser callback never
        /// arrived (tab closed, network drop). Idempotent — safe to call repeatedly.
        /// </summary>
        Task<PayPalWebhookResult> ReconcilePayPalPaymentAsync(Guid transactionId, string paypalOrderId);

        /// <summary>Webhook: the capture was denied/declined — close the pending transaction.</summary>
        Task MarkPayPalTransactionFailedAsync(Guid transactionId);

        // Legacy admin-assign (kept for backward compat)
        Task<CommonResponseDto> BuyPlanAsync(Guid employerId, Guid planId);
    }

    public enum PayPalWebhookResult
    {
        /// <summary>Credits applied now, or already applied earlier.</summary>
        Completed,
        /// <summary>No matching transaction of ours (other app on the same PayPal account, old event, etc.).</summary>
        Ignored,
        /// <summary>Matched, but not safe to apply (mismatch / not completed). Logged for manual review.</summary>
        Rejected,
        /// <summary>Temporary problem — return 5xx so PayPal retries the webhook.</summary>
        Retry
    }
}