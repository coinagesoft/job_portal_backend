using JobPortal.Domain.Enums.RecruiterEnums;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Application.DTOs.JobPosting;

public class CompensationRequestDto
{
    [Range(1, int.MaxValue,
      ErrorMessage = "Min salary must be greater than 0.")]
    public int? SalaryMin { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "Max salary must be greater than 0.")]
    public int? SalaryMax { get; set; }
    public SalaryCurrency? SalaryCurrency { get; set; }

    public string? SalaryDisplayOption { get; set; }

   

}
