using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.JobPosting;

public class EligibilityRequestDto
{
    [Range(1, 9999,
        ErrorMessage = "Vacancies must be at least 1.")]
    public int? Vacancies { get; set; }

    public EducationLevel? EducationRequired { get; set; }

    [Range(16, 99)]
    public int? AgeMin { get; set; }

    [Range(16, 99)]
    public int? AgeMax { get; set; }

    public GenderPreferred? GenderPreferred { get; set; }

    public bool? DisabilityEligible { get; set; }

    public bool? PassportRequired { get; set; }

    [Range(1, 120)]
    public int? PassportValidityMonths { get; set; }
}