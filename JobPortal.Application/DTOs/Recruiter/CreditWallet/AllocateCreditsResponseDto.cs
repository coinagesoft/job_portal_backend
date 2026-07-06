using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class AllocateCreditsResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid EmployerId { get; set; }

        public Guid SubUserId { get; set; }

        public int AllocatedCredits { get; set; }

        public int RemainingEmployerCredits { get; set; }
    }
}
