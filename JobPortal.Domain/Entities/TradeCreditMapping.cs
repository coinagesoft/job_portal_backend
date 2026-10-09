using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobPortal.Domain.Entities.Homepage;

namespace JobPortal.Domain.Entities;

public class TradeCreditMapping
{
    public Guid MappingId { get; set; } = Guid.NewGuid();

    // Existing main trade category
    public Guid TradeCategoryId { get; set; }

    public HomepageTradeCategory TradeCategory { get; set; } = default!;

    // Null means the default mapping for the main category.
    // Populated when a specific sub-trade is configured.
    public Guid? SubTradeId { get; set; }

    public HomepageSubTrade? SubTrade { get; set; }

    // The tier is assigned independently of the credit cost.
    public Guid TierId { get; set; }

    public TradeTier Tier { get; set; } = default!;

    // Exact cost for unlocking a candidate profile.
    public int UnlockCredits { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

