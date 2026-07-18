using JobPortal.Domain.Enums.RecruiterEnums;
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

        // Snapshot of who performed this action, taken at the moment it
        // happened. The owner/sub-user's name used to be resolved by a
        // live join at read time — that breaks the instant a sub-user is
        // deleted (their EmployerSubUsers row is gone), showing "Unknown
        // user" for perfectly real, already-spent credits. Storing it here
        // means the transaction stays readable forever, regardless of what
        // happens to the account later.
        public string? ActionByName { get; set; }

        public string? ActionByRole { get; set; }

        public Guid? CandidateId { get; set; }

        public Guid? UnlockId { get; set; }

        public TransactionType TransactionType { get; set; } = default!;

        public int CreditsUsed { get; set; }

        public int BalanceBefore { get; set; }

        public int BalanceAfter { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}