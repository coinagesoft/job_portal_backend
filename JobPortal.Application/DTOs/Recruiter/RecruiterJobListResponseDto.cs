using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class RecruiterJobListResponseDto : BaseResponseDto
    {
        public List<RecruiterJobDto> Jobs { get; set; } = new();
    }
}
