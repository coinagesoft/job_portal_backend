using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.TradeCredit
{

    public class TradeCreditImportRowDto
    {
        public string TradeName { get; set; } = string.Empty;

        public string? SubTradeName { get; set; }

        public string TierName { get; set; } = string.Empty;

        public int UnlockCredits { get; set; }
    }
}
