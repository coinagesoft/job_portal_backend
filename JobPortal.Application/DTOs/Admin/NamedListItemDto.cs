using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class IndustryTradeSubTradeDto
    {
        public Guid IndustryId { get; set; }
        public string IndustryName { get; set; } = default!;

        public List<TradeWithSubTradesDto> Trades { get; set; }
            = new();
    }

    public class TradeWithSubTradesDto
    {
        public Guid TradeCategoryId { get; set; }
        public string Name { get; set; } = default!;

        public List<SubTradeDto> SubTrades { get; set; }
            = new();
    }

    public class SubTradeDto
    {
        public Guid SubTradeId { get; set; }
        public string Name { get; set; } = default!;
    }
}
