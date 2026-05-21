namespace JobPortal.Domain.Entities;

public class OtpVerification
{
    public Guid OtpId { get; set; }

    public Guid? UserId { get; set; }

    public string MobileNumber { get; set; } = default!;

    public string CountryCode { get; set; } = default!;

    public string OtpCode { get; set; } = default!;

    public DateTime OtpSentAt { get; set; }

    public DateTime OtpExpiresAt { get; set; }

    public int ResendCooldownSec { get; set; }

    public byte OtpAttempts { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? LockedUntil { get; set; }

    // Navigation
    public User? User { get; set; }
}