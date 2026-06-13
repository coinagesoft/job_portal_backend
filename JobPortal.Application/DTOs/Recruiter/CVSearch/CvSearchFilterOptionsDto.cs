using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CVSearch
{
    public class CvSearchFilterOptionsDto
    {
        public List<string> TradeCategories { get; set; }
            = new();

        public List<string> Locations { get; set; }
            = new();

        public List<string> AvailabilityStatuses { get; set; }
            = new();
    }
}
