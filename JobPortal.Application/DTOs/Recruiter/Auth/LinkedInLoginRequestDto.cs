using JobPortal.Application.DTOs.Recruiter.Auth;
using JobPortal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Auth;

public class LinkedInLoginRequestDto
{

    public string LinkedInCode { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public UserType UserType { get; set; }
}
