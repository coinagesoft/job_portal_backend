using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class EmployerCandidateProfileDto
    {
        public Guid CandidateId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? ProfilePhotoUrl { get; set; }

        public string? PrimaryTrade { get; set; }

        public int TotalExperienceYears { get; set; }

        public string? CurrentCity { get; set; }

        public string? CurrentState { get; set; }

        public string? AvailabilityStatus { get; set; }

        public bool IsUnlocked { get; set; }

        // Visible only after unlock

        public string? Email { get; set; }

        public string? MobileNumber { get; set; }

        public string? CountryCode { get; set; }

        public string? CvUrl { get; set; }
    }
}
