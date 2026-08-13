using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate.Auth
{
    // The amount is deliberately NOT sent by the client anymore — it's
    // always resolved server-side from the active, admin-managed
    // Candidate MembershipPlan for the given pricing region. Trusting a
    // client-supplied amount would let anyone create a Razorpay order
    // for any price they choose.
    public class CreateCandidateOrderRequestDto
    {
        // Pricing-region code, e.g. "in", "us", "ae". Defaults to "in"
        // (server-side) since the candidate membership fee is currently
        // an India-focused ₹ flow.
        public string? Region { get; set; }
    }
}