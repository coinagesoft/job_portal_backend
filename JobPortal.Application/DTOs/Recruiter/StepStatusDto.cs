using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class StepStatusDto
    {
        public int CurrentStep { get; set; }
        public int LastCompletedStep { get; set; }
        public int TotalSteps { get; set; } = 5;
        public string SessionId { get; set; } = string.Empty;
        public List<string> CompletedSteps { get; set; } = new();
        public string? NextStep { get; set; }
        public bool CanResume { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
