using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CandidateProfile
{
    public class CandidateSummaryResponseDto
    {
        public string? About { get; set; }

        public string? ProfessionalSummary { get; set; }

        public string? Nationality { get; set; }

        public int? PreferredSalary { get; set; }

        public bool DisabilityStatus { get; set; }

        public string? DisabilityNote { get; set; }

        public bool ItiCertified { get; set; }

        public string? ItiTrade { get; set; }

        public string? ItiCollege { get; set; }

        public string? ItiMarks { get; set; }
    }
}
