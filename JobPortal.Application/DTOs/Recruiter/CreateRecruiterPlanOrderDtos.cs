using System;

namespace JobPortal.Application.DTOs.Recruiter
{
    // The amount is deliberately NOT sent by the client — it's always
    // resolved server-side from the active, admin-managed Recruiter
    // MembershipPlan for the given pricing region, exactly like
    // CreateCandidateOrderRequestDto does for candidates. Trusting a
    // client-supplied amount would let anyone create a Razorpay order for
    // any price they choose.
    public class CreateRecruiterPlanOrderRequestDto
    {
        // Registration session id from Step 1 (X-Session-Id). Kept so the
        // order can eventually be tied back to the in-progress
        // registration, and so we can fail fast if the session is
        // missing/expired before hitting the payment gateway.
        public string SessionId { get; set; } = string.Empty;

        // Pricing-region code, e.g. "in", "us", "ae". Defaults to "in"
        // (server-side) — same convention as the candidate membership fee.
        public string? Region { get; set; }
    }

    public class CreateRecruiterPlanOrderResponseDto
    {
        public bool Success { get; set; }

        public string OrderId { get; set; } = string.Empty;

        // Rupees (for display) and paise (what Razorpay/the checkout
        // widget actually needs) — both derived from the admin-configured
        // MembershipPlan, never from client input.
        public decimal Amount { get; set; }

        public int AmountPaise { get; set; }

        public string Currency { get; set; } = "INR";

        public string RazorpayKeyId { get; set; } = string.Empty;

        // The recruiter MembershipPlan this order was created for. The
        // frontend must echo this back on submit-registration so the
        // amount can be re-verified server-side at that point too.
        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}