using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.Candidate.Missing;

// ════════════════════════════════════════════════════════════════
// LIVE LOCATION
//    GET /api/candidate/profile/location
//    PUT /api/candidate/profile/location
// ════════════════════════════════════════════════════════════════

public class UpdateCandidateLocationRequestDto
{
    [Required]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    /// <summary>
    /// True the first time the candidate grants browser/device location
    /// permission. Subsequent periodic sync calls should keep sending true.
    /// </summary>
    public bool PermissionGranted { get; set; } = true;
}

public class CandidateLocationResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public CandidateLocationData? Data { get; set; }
}

public class CandidateLocationData
{
    public Guid CandidateId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool PermissionGranted { get; set; }
    public DateTime? LocationUpdatedAt { get; set; }
}