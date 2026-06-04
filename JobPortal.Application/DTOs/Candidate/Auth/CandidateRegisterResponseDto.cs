namespace JobPortal.Application.DTOs.Candidate.Auth;

public class CandidateRegisterResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Token { get; set; }

    public Guid? CandidateId { get; set; }

    public string? UserName { get; set; }

    public string? RedirectTo { get; set; }
}