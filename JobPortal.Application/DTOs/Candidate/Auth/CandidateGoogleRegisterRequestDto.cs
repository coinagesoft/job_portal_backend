using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate.Auth
{
    public class CandidateGoogleRegisterRequestDto
    {
        [Required]
        public string AccessToken { get; set; } = default!;

        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; } = default!;

        public string? MobileNumber { get; set; }
        public string? CountryCode { get; set; }

        public bool TermsAccepted { get; set; }

        // Exactly one gateway's proof is sent (see CandidateRegisterRequestDto).
        public string? RazorpayOrderId { get; set; }
        public string? RazorpayPaymentId { get; set; }
        public string? RazorpaySignature { get; set; }
        public string? PayPalOrderId { get; set; }

        // Coupon applied on the checkout. Validated again server-side.
        public string? CouponCode { get; set; }

        // The MembershipPlan (PlanType.Candidate) the paid order was
        // created for — echoed back from /api/candidate/auth/create-order.
        [Required(ErrorMessage = "Membership plan is required.")]
        public Guid PlanId { get; set; }
    }
}