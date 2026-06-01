using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.JobPosting;

public class EligibilityRequestDto
{
    [Required]
    [Range(1, 9999, ErrorMessage = "Vacancies must be at least 1.")]
    public int Vacancies { get; set; } = 1;

    [Required]
    public EducationLevel EducationRequired { get; set; }

    [Range(16, 99)]
    public int? AgeMin { get; set; }

    [Range(16, 99)]
    public int? AgeMax { get; set; }

    public GenderPreferred GenderPreferred { get; set; } = GenderPreferred.Any;

    public bool DisabilityEligible { get; set; } = false;

    public bool PassportRequired { get; set; } = false;

    [Range(1, 120)]
    public int? PassportValidityMonths { get; set; }
}