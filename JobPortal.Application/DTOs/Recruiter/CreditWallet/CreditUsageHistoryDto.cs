using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class CreditUsageHistoryDto
    {
        public Guid TransactionId { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public Guid? CandidateId { get; set; }

        public int CreditsUsed { get; set; }

        public int BalanceBefore { get; set; }

        public int BalanceAfter { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
