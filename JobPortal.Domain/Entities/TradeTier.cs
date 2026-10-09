using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



    namespace JobPortal.Domain.Entities;

    public class TradeTier
    {
        public Guid TierId { get; set; } = Guid.NewGuid();

        // Example: Tier 1, Tier 2, Tier 3
        public string TierName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

