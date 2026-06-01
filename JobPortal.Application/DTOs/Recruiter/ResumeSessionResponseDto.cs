using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter;

/// <summary>
/// Resume a saved registration session
/// </summary>
public class ResumeSessionResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public StepStatusDto? StepStatus { get; set; }

    // Pre-filled data for each completed step
    public GstCheckResponseDto? Step1Data { get; set; }
    public CompanyDetailsResponseDto? Step2Data { get; set; }
    public bool Step3Verified { get; set; }
    public bool Step4LicencesSkipped { get; set; }
}
