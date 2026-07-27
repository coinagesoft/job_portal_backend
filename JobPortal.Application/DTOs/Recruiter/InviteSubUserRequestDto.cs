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

    // ── Permissions — chosen directly via checkboxes on the invite form.
    // No more Role dropdown; these four flags are the single source of
    // truth for what the sub-user can do. ─────────────────────────────
    public bool CanSearchCandidates { get; set; }
    public bool CanUnlockProfiles { get; set; }
    public bool CanPostJobs { get; set; }
    public bool CanManageApplications { get; set; }

    // ── Optional credit allocation at invite time — lets the owner
    // hand the sub-user a starting balance in the same step as the
    // invite, instead of having to invite first and allocate separately
    // from the Credits & Wallets page. Defaults to 0 (no allocation),
    // which keeps existing invite flows/tests unaffected. ─────────────
    [Range(0, int.MaxValue, ErrorMessage = "Initial credits cannot be negative.")]
    public int InitialCredits { get; set; } = 0;
}