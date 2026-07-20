using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class CreditAllocationHistory
    {
        [Key]
        public Guid HistoryId { get; set; }

        public Guid EmployerId { get; set; }

        public Guid SubUserId { get; set; }

        // Snapshot of the sub-user's display name at the time this
        // allocation/reclaim event happened. Needed because a sub-user can
        // later be deleted (their EmployerSubUsers row disappears), at
        // which point a live join on SubUserId can no longer resolve a
        // name — this keeps old history entries readable regardless.
        public string? SubUserName { get; set; }

        public int CreditsAllocated { get; set; }

        public int BalanceBefore { get; set; }

        public int BalanceAfter { get; set; }


        public DateTime CreatedAt { get; set; }
    }
}