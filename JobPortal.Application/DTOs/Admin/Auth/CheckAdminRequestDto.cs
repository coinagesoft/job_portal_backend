using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class CheckAdminRequestDto
    {
        [Required]
        public string MobileNumber { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\+\d{1,4}$",
            ErrorMessage = "e.g. +91, +971, +1")]
        public string CountryCode { get; set; } = string.Empty;
    }
}
