namespace JobPortal.Application.DTOs.Recruiter.CreditWallet;

/// <summary>
/// In-memory result of a watermarked CV download. The bytes are streamed
/// straight to the recruiter and never persisted anywhere.
/// </summary>
public class WatermarkedCvResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public byte[]? FileBytes { get; set; }

    public string FileName { get; set; } = "CV.pdf";
}