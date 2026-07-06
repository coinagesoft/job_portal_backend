using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{


    public class ScheduleInterviewRequestDto
    {
        [Required]
        public DateTime InterviewDate { get; set; }
    }
}
