using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{

    public class User
    {
        public Guid UserId { get; set; }

        public UserType UserType { get; set; } = default!;

        public string? MobileNumber { get; set; } = default!;

        public string? CountryCode { get; set; } = default!;

        public string? Email { get; set; }

        public string PasswordHash { get; set; } = default!;

        public AccountStatus AccountStatus { get; set; } = default!;

        public KycStatus KycStatus { get; set; } = default!;

        public PaymentStatus PaymentStatus { get; set; } = default!;

        public DateTime? LastLoginAt { get; set; }

        public string? SuspensionReason { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation Property
        public ICollection<OtpVerification> OtpVerifications { get; set; }
            = new List<OtpVerification>();
    }
}
