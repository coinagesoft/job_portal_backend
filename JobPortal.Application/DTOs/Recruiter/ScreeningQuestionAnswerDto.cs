using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class ScreeningQuestionAnswerDto
    {
        public string Question { get; set; } = string.Empty;

        public string? Answer { get; set; }
    }
}
