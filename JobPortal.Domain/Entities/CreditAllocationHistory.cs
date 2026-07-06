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

        public int CreditsAllocated { get; set; }

        public int BalanceBefore { get; set; }

        public int BalanceAfter { get; set; }


        public DateTime CreatedAt { get; set; }
    }
}
