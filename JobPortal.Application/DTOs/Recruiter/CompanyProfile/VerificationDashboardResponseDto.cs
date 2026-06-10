using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CompanyProfile
{
    public class VerificationDashboardResponseDto
    {
        public List<VerificationBadgeDto> Badges { get; set; } = new();

        public List<VerificationDocumentDto> Documents { get; set; } = new();
    }
}
