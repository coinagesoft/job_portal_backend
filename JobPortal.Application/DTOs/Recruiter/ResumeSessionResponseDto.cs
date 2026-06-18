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

    public GstCheckResponseDto? Step1Data { get; set; }

    public ResumeCompanyDetailsDto? Step2Data { get; set; }

    public ResumeContactDetailsDto? Step3Data { get; set; }

    public ResumeLicenceDetailsDto? Step4Data { get; set; }
}
