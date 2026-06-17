namespace JobPortal.Domain.Entities;

public class CandidateLogoutSession
{
    public Guid LogoutSessionId { get; set; }
    public Guid CandidateId { get; set; }
    public string? FcmToken { get; set; }
    public string? JwtJti { get; set; }
    public DateTime LoggedOutAt { get; set; }
    public DateTime? JwtExpiresAt { get; set; }

    // Navigation
    public CandidateProfile? CandidateProfile { get; set; }
}