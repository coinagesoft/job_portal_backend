using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RegistrationStatusResponseDto
    {
        public Guid EmployerId { get; set; }
        public string AccountStatus { get; set; } = string.Empty;   // Pending | Active | Rejected
        public string NextStep { get; set; } = string.Empty;        // pay_deposit | start_trial | complete_kyc
        public bool RequiresSecurityDeposit { get; set; }
        public int? SecurityDepositAmountRs { get; set; }
        public int ProfileCompletionScore { get; set; }
        public DateTime CreatedAt { get; set; }

        public StepStatusDto? StepStatus { get; set; }
    }
}
