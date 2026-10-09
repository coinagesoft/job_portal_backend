using System;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class CreateRecruiterPlanOrderRequestDto
    {
        // Registration session id from Step 1 (X-Session-Id)
        public string SessionId { get; set; } = string.Empty;

        // Pricing region, e.g. "in", "us", "ae"
        public string? Region { get; set; }

        // The membership plan selected by the recruiter.
        // Price will ALWAYS be resolved server-side.
        public Guid PlanId { get; set; }

        // Optional coupon entered by the recruiter.
        // The server will validate it against the selected plan.
        public string? CouponCode { get; set; }
    }

    public class CreateRecruiterPlanOrderResponseDto
    {
        public bool Success { get; set; }

        public string OrderId { get; set; } = string.Empty;

        // Original membership plan price in rupees.
        public decimal Amount { get; set; }

        // Original membership plan price in paise.
        public int AmountPaise { get; set; }

        public string Currency { get; set; } = "INR";

        public string RazorpayKeyId { get; set; } = string.Empty;

        // Selected Recruiter membership plan.
        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        // Coupon information
        public decimal DiscountAmount { get; set; }

        public int DiscountAmountPaise { get; set; }

        // Final amount after coupon discount.
        public decimal FinalAmount { get; set; }

        public int FinalAmountPaise { get; set; }

        public string? CouponCode { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}