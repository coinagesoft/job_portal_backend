using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class EmployerTransactionHistoryDto
    {
        public Guid TransactionId { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public Guid? CandidateId { get; set; }

        public string? CandidateName { get; set; }

        public int? CreditsUsed { get; set; }

        public decimal? AmountPaid { get; set; }

        public string? PlanName { get; set; }

        public DateTime CreatedAt { get; set; }

        // Who actually performed this transaction — the account owner or a
        // specific sub-user — so the owner can see which user used credits.
        public Guid ActionByUserId { get; set; }

        public string ActionByName { get; set; } = "Unknown user";

        /// <summary>"Account Owner" or the sub-user's role (e.g. "Recruiter").</summary>
        public string ActionByRole { get; set; } = string.Empty;
    }
}