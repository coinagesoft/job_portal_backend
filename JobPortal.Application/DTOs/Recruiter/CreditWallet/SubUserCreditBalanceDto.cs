using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class SubUserCreditBalanceDto
    {
        public Guid SubUserId { get; set; }

        public int AllocatedCredits { get; set; }

        public int UsedCredits { get; set; }

        public int RemainingCredits { get; set; }
    }
}
