using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate.Auth
{
    public class CandidateLinkedInRegisterRequestDto
    {
        [Required]
        public string AccessToken { get; set; } = default!;   // LinkedIn's access token, obtained from verify step

        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; } = default!;

        public string? MobileNumber { get; set; }
        public string? CountryCode { get; set; }

        public bool TermsAccepted { get; set; }

        [Required(ErrorMessage = "Payment verification required.")]
        public string RazorpayOrderId { get; set; } = default!;
        [Required(ErrorMessage = "Payment verification required.")]
        public string RazorpayPaymentId { get; set; } = default!;
        [Required(ErrorMessage = "Payment verification required.")]
        public string RazorpaySignature { get; set; } = default!;

        // The MembershipPlan (PlanType.Candidate) the paid order was
        // created for — echoed back from /api/candidate/auth/create-order.
        [Required(ErrorMessage = "Membership plan is required.")]
        public Guid PlanId { get; set; }
    }
}