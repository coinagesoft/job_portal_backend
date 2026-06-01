namespace JobPortal.Application.DTOs.JobPosting;

public class JobStepStatusDto
{
    public Guid JobId { get; set; }
    public int CurrentStep { get; set; }
    public int LastCompletedStep { get; set; }
    public int TotalSteps { get; set; } = 7;
    public string JobStatus { get; set; } = "Draft";
    public List<string> CompletedSteps { get; set; } = new();
    public string? NextStep { get; set; }
}