using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class CandidateUnlock
{
    public Guid UnlockId { get; set; }

    public Guid EmployerId { get; set; }

    public Guid CandidateId { get; set; }

    public Guid UnlockRequestedBy { get; set; }

    public int CreditsDeducted { get; set; }

    public DateTime UnlockTimestamp { get; set; }

    public DateOnly UnlockExpiryDate { get; set; }

    public int WalletBalanceBefore { get; set; }

    public int WalletBalanceAfter { get; set; }

    public string UnlockStatus { get; set; } = default!;

    public bool CvDownloadAllowed { get; set; }

    public DateTime CreatedAt { get; set; }
}