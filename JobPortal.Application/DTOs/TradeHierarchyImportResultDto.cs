using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs
{
    public class TradeHierarchyImportResultDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int IndustriesAdded { get; set; }

        public int TradesAdded { get; set; }

        public int SubTradesAdded { get; set; }
    }
}
