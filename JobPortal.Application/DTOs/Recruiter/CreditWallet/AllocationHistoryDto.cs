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

        public string? SubUserName { get; set; }

        public int CreditsAllocated { get; set; }

        // True when this row represents credits being reclaimed back to
        // the shared pool (e.g. because the sub-user was deleted) rather
        // than newly handed out. CreditsAllocated is negative in that case.
        public bool IsReclaim { get; set; }

        public int BalanceBefore { get; set; }

        public int BalanceAfter { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}