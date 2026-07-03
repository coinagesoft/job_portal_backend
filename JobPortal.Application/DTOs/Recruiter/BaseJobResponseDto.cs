namespace JobPortal.Application.DTOs.JobPosting;

public class BaseJobResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Guid JobId { get; set; }
    public JobStepStatusDto? StepStatus { get; set; }
}

public class ResumeJobResponseDto : BaseJobResponseDto
{
    public JobDetailsRequestDto? Step1Data { get; set; }
    public CompensationRequestDto? Step2Data { get; set; }
    public SkillsRequestDto? Step3Data { get; set; }
    public EligibilityRequestDto? Step4Data { get; set; }
    public LocationRequestDto? Step5Data { get; set; }
    public QuestionsRequestDto? Step6Data { get; set; }
    public PublishingRequestDto? Step7Data { get; set; }
}