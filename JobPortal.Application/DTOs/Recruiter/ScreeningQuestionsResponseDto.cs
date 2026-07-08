using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class ScreeningQuestionsResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid JobId { get; set; }

        public List<string> Questions { get; set; } = new();
    }
}
