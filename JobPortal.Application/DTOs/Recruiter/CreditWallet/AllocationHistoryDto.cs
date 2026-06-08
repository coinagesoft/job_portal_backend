using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{

    public class AllocationHistoryDto
    {
        public Guid HistoryId { get; set; }

        public Guid SubUserId { get; set; }

        public int CreditsAllocated { get; set; }

        public int BalanceBefore { get; set; }

        public int BalanceAfter { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
