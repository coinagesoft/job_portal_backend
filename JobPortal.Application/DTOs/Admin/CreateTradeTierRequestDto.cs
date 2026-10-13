using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin
{
    public class CreateTradeTierRequestDto
    {
        public string TierName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public int UnlockCredits { get; set; }
    }
}
