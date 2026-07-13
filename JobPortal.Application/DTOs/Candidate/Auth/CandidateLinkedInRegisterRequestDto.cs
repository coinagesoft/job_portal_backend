using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate.Auth
{
    public class CandidateLinkedInRegisterRequestDto
    {
        public string LinkedInCode { get; set; } = default!;
        public string RedirectUri { get; set; } = default!;
        public string? MobileNumber { get; set; }
        public string? CountryCode { get; set; }
        public bool TermsAccepted { get; set; }
    }
}
