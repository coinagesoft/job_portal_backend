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

        // Pass these directly to the Razorpay JS SDK.
        // AmountPaise is the GST-inclusive total actually charged.
        public string? RazorpayOrderId { get; set; }
        public int AmountPaise { get; set; }
        public string Currency { get; set; } = "INR";
        public string? RazorpayKeyId { get; set; }

        public string? PaymentGateway { get; set; }
        public string? PayPalOrderId { get; set; }
        public string? PayPalApprovalUrl { get; set; }
        public string? PayPalClientId { get; set; }

        // Breakdown, for display / invoicing
        public int BaseAmountPaise { get; set; }
        public int GstAmountPaise { get; set; }

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

        // Razorpay verification
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public string? RazorpaySignature { get; set; }

        // PayPal verification
        public string? PayPalOrderId { get; set; }

        // Informational only — the server decides the gateway from the stored transaction.
        public string? PaymentGateway { get; set; }
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