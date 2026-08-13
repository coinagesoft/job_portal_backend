using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate.Auth
{
    public class CreateCandidateOrderResponseDto
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

        // The candidate MembershipPlan this order was created for. The
        // frontend must echo this back on the register call so the
        // amount can be re-verified server-side at that point too.
        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}