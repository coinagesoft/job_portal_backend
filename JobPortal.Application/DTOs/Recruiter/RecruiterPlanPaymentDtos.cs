// File: JobPortal.Application/DTOs/Recruiter/RecruiterPlanPaymentDtos.cs

namespace JobPortal.Application.DTOs.Recruiter
{
    // ── Step 1 request body ──────────────────────────────────────────
    public class CreatePlanOrderRequestDto
    {
        public Guid PlanId { get; set; }
    }

    // ── Step 1 response ──────────────────────────────────────────────
    public class CreatePlanOrderResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        // Pass these directly to the Razorpay JS SDK
        public string? RazorpayOrderId { get; set; }
        public int AmountPaise { get; set; }
        public string Currency { get; set; } = "INR";
        public string? RazorpayKeyId { get; set; }

        // Plan metadata — useful for the checkout UI
        public Guid PlanId { get; set; }
        public string? PlanName { get; set; }
        public int Credits { get; set; }
        public int ValidityMonths { get; set; }

        // Send this back in the verify call
        public Guid TransactionId { get; set; }
    }

    // ── Step 2 request body ──────────────────────────────────────────
    public class VerifyPlanPaymentRequestDto
    {
        public Guid TransactionId { get; set; }   // from Step 1
        public string RazorpayOrderId { get; set; } = default!;
        public string RazorpayPaymentId { get; set; } = default!;
        public string RazorpaySignature { get; set; } = default!;
    }

    // ── Step 2 response ──────────────────────────────────────────────
    public class VerifyPlanPaymentResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? NewCreditBalance { get; set; }
        public Guid? PurchaseId { get; set; }
    }
}