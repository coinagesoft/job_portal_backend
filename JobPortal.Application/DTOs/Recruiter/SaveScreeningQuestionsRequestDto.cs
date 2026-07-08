using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class SaveScreeningQuestionsRequestDto
    {
        public List<string> Questions { get; set; } = new();
    }
}
