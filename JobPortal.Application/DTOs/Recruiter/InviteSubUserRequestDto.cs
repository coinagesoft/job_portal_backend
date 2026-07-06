using JobPortal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.SubUser;

public class InviteSubUserRequestDto
{
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(150)]
    public string SubUserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Corporate email is required.")]
    [EmailAddress]
    public string SubUserEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    public string SubUserMobile { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^\+\d{1,4}$")]
    public string CountryCode { get; set; } = "+91";

    [Required(ErrorMessage = "Role is required.")]
    public SubUserRole Role { get; set; }
}
