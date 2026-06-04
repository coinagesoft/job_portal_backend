using JobPortal.Application.DTOs.JobPosting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class CreditUsageTransaction
    {
        [Key]
        public Guid TransactionId { get; set; }

        public Guid EmployerId { get; set; }

        public Guid ActionByUserId { get; set; }

        public Guid? CandidateId { get; set; }

        public Guid? UnlockId { get; set; }

        public TransactionType TransactionType { get; set; } = default!;

        public int CreditsUsed { get; set; }

        public int BalanceBefore { get; set; }

        public int BalanceAfter { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
