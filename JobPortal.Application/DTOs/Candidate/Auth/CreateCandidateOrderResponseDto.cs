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

        // Original MembershipPlan price in rupees.
        public decimal Amount { get; set; }

        // Original MembershipPlan price in paise.
        public int AmountPaise { get; set; }

        // Coupon discount in rupees.
        public decimal DiscountAmount { get; set; }

        // Coupon discount in paise.
        public int DiscountAmountPaise { get; set; }

        // Final amount actually charged by Razorpay in rupees.
        public decimal FinalAmount { get; set; }

        // Final amount actually charged by Razorpay in paise.
        public int FinalAmountPaise { get; set; }

        public string Currency { get; set; } = "INR";

        public string RazorpayKeyId { get; set; } = string.Empty;

        public string PaymentGateway { get; set; } = "Razorpay";

        public string? PayPalOrderId { get; set; }

        public string? PayPalApprovalUrl { get; set; }

        public string? PayPalClientId { get; set; }

        // The Candidate MembershipPlan used for this order.
        public Guid PlanId { get; set; }

        public string PlanName { get; set; } = string.Empty;

        // The coupon that was applied, if any.
        public string? CouponCode { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}