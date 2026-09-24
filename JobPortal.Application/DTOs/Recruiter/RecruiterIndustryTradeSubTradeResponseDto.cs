using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RecruiterIndustryTradeSubTradeResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;

        public List<RecruiterIndustryWithTradesDto> Industries { get; set; } = new();
    }

    public class RecruiterIndustryWithTradesDto
    {
        public Guid IndustryId { get; set; }

        public string Name { get; set; } = default!;

        public List<RecruiterTradeWithSubTradesDto> Trades { get; set; } = new();
    }

    public class RecruiterTradeWithSubTradesDto
    {
        public Guid TradeCategoryId { get; set; }

        public string Name { get; set; } = default!;

        public List<RecruiterSubTradeDto> SubTrades { get; set; } = new();
    }

    public class RecruiterSubTradeDto
    {
        public Guid SubTradeId { get; set; }

        public string Name { get; set; } = default!;
    }
}
