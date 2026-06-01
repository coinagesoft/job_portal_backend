using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.JobPosting;

public class CompensationRequestDto
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Min salary must be greater than 0.")]
    public int SalaryMin { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Max salary must be greater than 0.")]
    public int SalaryMax { get; set; }

    [Required]
    public SalaryCurrency SalaryCurrency { get; set; } = SalaryCurrency.INR;

    [Required]
    public SalaryDisplayOption SalaryDisplayOption { get; set; } = SalaryDisplayOption.Show_Range;
}
