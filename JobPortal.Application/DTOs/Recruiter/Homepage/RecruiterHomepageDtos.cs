// ============================================================
//  JobPortal.Application/DTOs/Recruiter/Homepage/RecruiterHomepageDtos.cs
//
//  Backs the recruiter-facing dropdown + suggestion APIs. Split by where
//  each field actually lives in the product:
//
//    - Industry Type  → Employer Registration, Step 1 (GST check step).
//                        Anonymous — no recruiter account exists yet.
//                        See RecruiterRegistrationController.
//
//    - Trade/Role,
//      Department     → Job Posting form. Requires a logged-in recruiter.
//                        See RecruiterJobPostingController.
//
//  Both read sides pull from the same admin-managed lists shown on
//  "Recruiter Registration Management" → Recruiter tab
//  (https://job-portal-admin-gray.vercel.app/admin/homepage-management),
//  and both write "Other" suggestions into the same HomepageSuggestion
//  table the admin Suggestions inbox reviews.
// ============================================================

using System;
using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Recruiter.Homepage
{
    /// <summary>One selectable option in a recruiter-facing dropdown.</summary>
    public class RecruiterDropdownOptionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    // ── Registration — Step 1 (Industry Type) ──────────────────────

    /// <summary>
    /// GET api/recruiter/registration/industries response.
    /// Active-only, admin display order. Frontend appends its own "Other"
    /// option — picking it reveals the free-text field that posts to
    /// api/recruiter/registration/industry-suggestions below.
    /// </summary>
    public class RecruiterIndustriesResponseDto
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public List<RecruiterDropdownOptionDto> Industries { get; set; } = new();
    }

    // ── Job Posting (Trade/Role, Department) ────────────────────────

    /// <summary>
    /// GET api/recruiter/jobs/dropdowns response.
    /// Active-only, admin display order. Frontend appends its own "Other"
    /// option to each list — picking it reveals the free-text field that
    /// posts to api/recruiter/jobs/suggestions below.
    /// </summary>
    public class RecruiterJobPostingDropdownsResponseDto
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        /// <summary>Job posting "Trade / Role" dropdown.</summary>
        public List<RecruiterDropdownOptionDto> TradeRoles { get; set; } = new();

        /// <summary>Job posting "Department" dropdown.</summary>
        public List<RecruiterDropdownOptionDto> Departments { get; set; } = new();
    }

    // ── Suggestions (shared shape, used by both flows above) ────────

    /// <summary>
    /// POST body for both api/recruiter/registration/industry-suggestions
    /// and api/recruiter/jobs/suggestions. Sent when the recruiter picks
    /// "Other" and types in a value that isn't in the list. Lands in the
    /// admin "Suggestions inbox"; approving it adds the value straight
    /// into the matching dropdown.
    /// </summary>
    public class RecruiterSuggestionRequestDto
    {
        public string Field { get; set; } = default!;

        public string SuggestedName { get; set; } = default!;

        public string? Note { get; set; }

        public string? SubmittedByName { get; set; }

        public string? SubmittedByEmail { get; set; }

        // Parent hierarchy
        public Guid? RegistrationIndustryId { get; set; }

        public Guid? TradeCategoryId { get; set; }
    }

    public class RecruiterSuggestionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = default!;
        public Guid? SuggestionId { get; set; }
    }
}