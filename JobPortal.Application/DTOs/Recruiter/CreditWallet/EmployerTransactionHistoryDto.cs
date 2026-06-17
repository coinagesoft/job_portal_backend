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
    }
}
