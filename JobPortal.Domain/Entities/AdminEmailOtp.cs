using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{

    public class AdminEmailOtp
    {
        public Guid OtpId { get; set; }

        public Guid AdminId { get; set; }

        public string Email { get; set; } = default!;

        public string OtpCode { get; set; } = default!;

        public string Purpose { get; set; } = default!;

        public short Attempts { get; set; }

        public bool IsVerified { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public AdminUser AdminUser { get; set; } = default!;
    }
}
