using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace JobPortal.Application.DTOs.TradeCredit;

public class TradeCreditMappingResponseDto
{
    public Guid MappingId { get; set; }

    public Guid TradeCategoryId { get; set; }
    public string TradeName { get; set; } = string.Empty;

    public Guid? SubTradeId { get; set; }
    public string? SubTradeName { get; set; }

    public Guid TierId { get; set; }
    public string TierName { get; set; } = string.Empty;

    public int UnlockCredits { get; set; }

    public bool IsActive { get; set; }
}

