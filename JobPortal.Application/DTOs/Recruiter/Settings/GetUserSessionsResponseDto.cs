using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    public class GetUserSessionsResponseDto
    {
        public int TotalSessions { get; set; }

        public List<UserSessionDto> Sessions { get; set; }
            = new();
    }
}
