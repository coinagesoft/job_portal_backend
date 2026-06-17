using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Recruiter.CVSearch
{
    public class CvSearchRequestDto
    {
        public string? Keyword { get; set; }

        public string? TradeCategory { get; set; }

        public int? MinExperience { get; set; }

        public int? MaxExperience { get; set; }

        public string? Location { get; set; }

        public string? AvailabilityStatus { get; set; }

        public bool ItiCertifiedOnly { get; set; }

        public bool PassportValidOnly { get; set; }

        public bool UnlockedProfilesOnly { get; set; }

        public string? SortBy { get; set; } = "KeywordMatch";

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}
