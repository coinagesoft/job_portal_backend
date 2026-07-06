namespace JobPortal.Application.DTOs.JobPosting;

public class RoleSearchResponseDto
{
    public List<string> Suggestions { get; set; } = new();
    public bool AllowCustom { get; set; } = true;   // always true
    public string Message { get; set; } = string.Empty;
}