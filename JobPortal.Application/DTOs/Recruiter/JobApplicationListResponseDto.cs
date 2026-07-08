using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class JobApplicationListResponseDto : BaseResponseDto
    {
        public Guid JobId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public List<JobApplicationItemDto> Applications { get; set; } = new();
    }
}
