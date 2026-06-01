using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.JobPosting;

public class LocationRequestDto
{
    [Required]
    public LocationType LocationType { get; set; }

    // Required if LocationType = Onshore
    public string? OnshoreCity { get; set; }
    public string? OnshoreState { get; set; }

    // Required if LocationType = Offshore
    public string? OffshoreVesselName { get; set; }
    public string? OffshoreRegion { get; set; }

    public string Country { get; set; } = "India";
}