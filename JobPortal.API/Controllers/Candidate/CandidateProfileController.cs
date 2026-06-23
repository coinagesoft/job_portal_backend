// ============================================================
//  JobPortal.API/Controllers/Candidate/
//  CandidateProfileController.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Profile;
using JobPortal.Services.IImplement.ICandidate;
using JobPortal.Services.Implement.Candidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/profile")]
[Produces("application/json")]
[Authorize(Roles ="Candidate")]
// [Authorize(Roles = "Candidate")]   // Uncomment once JWT auth middleware is wired up
public class CandidateProfileController : ControllerBase
{
    private readonly ICandidateProfileService _profileService;
    private readonly ILogger<CandidateProfileController> _logger;

    public CandidateProfileController(
        ICandidateProfileService profileService,
        ILogger<CandidateProfileController> logger)
    {
        _profileService = profileService;
        _logger         = logger;
    }

    /// <summary>Extracts CandidateId from JWT claim; falls back to route/query param for dev.</summary>
    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("candidateId")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    // ════════════════════════════════════════════════
    // PROFILE SUMMARY — header card
    // GET /api/candidate/profile/summary
    // ════════════════════════════════════════════════
    /// <summary>
    /// Lightweight profile summary: name, photo, mobile, email, city, exp, notice period,
    /// about, profileCompletionPct.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(CandidateProfileSummaryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfileSummary(
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _profileService.GetProfileSummaryAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ════════════════════════════════════════════════
    // PERSONAL INFO — full edit form
    // GET  /api/candidate/profile/personal-info
    // PUT  /api/candidate/profile/personal-info
    // ════════════════════════════════════════════════
    /// <summary>
    /// Returns all personal-info fields: DOB, email, city, state, pincode,
    /// professional summary, about, notice period, experience years, newsletter opt-in.
    /// </summary>
    [HttpGet("personal-info")]
    [ProducesResponseType(typeof(CandidatePersonalInfoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPersonalInfo(
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _profileService.GetPersonalInfoAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Updates personal info.  Fields: FullName, DateOfBirth, Gender, Email,
    /// CurrentCity, CurrentState, Pincode, ProfessionalSummary, About,
    /// NoticePeriod, TotalExperienceYears, NewsletterOptIn.
    /// Returns updated profileCompletionPct.
    /// </summary>
    [HttpPut("personal-info")]
    [ProducesResponseType(typeof(UpdateCandidatePersonalInfoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePersonalInfo(
        [FromBody] UpdateCandidatePersonalInfoRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _profileService.UpdatePersonalInfoAsync(id, request);

        if (!result.Success && result.Message.Contains("already in use"))
            return Conflict(result);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // PROFILE PHOTO
    // POST   /api/candidate/profile/profile-photo
    // DELETE /api/candidate/profile/profile-photo
    // ════════════════════════════════════════════════
    /// <summary>
    /// Upload or replace the profile photo.
    /// Accepted: JPEG, PNG, WebP · Max 5 MB.
    /// </summary>
    [HttpPost("profile-photo")]
    [RequestSizeLimit(6 * 1024 * 1024)]  // 6 MB request ceiling
    [ProducesResponseType(typeof(UploadProfilePhotoResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadProfilePhoto(
        IFormFile photo,
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _profileService.UploadProfilePhotoAsync(id, photo);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Removes the profile photo.</summary>
    [HttpDelete("profile-photo")]
    [ProducesResponseType(typeof(UploadProfilePhotoResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteProfilePhoto([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _profileService.DeleteProfilePhotoAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }


    [HttpPost("personal-info")]
    public async Task<IActionResult> CreateProfile(
    [FromBody] CreateCandidateProfileRequestDto request)
    {
        var userId = GetCandidateId();

        var result = await _profileService
    .CreateProfileAsync(userId, request);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    // ════════════════════════════════════════════════
    // PROFILE COMPLETION BREAKDOWN
    // GET /api/candidate/profile/completion
    // ════════════════════════════════════════════════
    /// <summary>
    /// Returns itemised completion checklist: overallPct, each section flag,
    /// and a prioritised list of pending actions.
    /// </summary>
    [HttpGet("completion")]
    [ProducesResponseType(typeof(ProfileCompletionResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfileCompletion([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _profileService.GetProfileCompletionAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // ENUM OPTIONS
    // GET /api/candidate/profile/enum-options
    // ════════════════════════════════════════════════
    /// <summary>Dropdown options for gender, notice period, availability, document types.</summary>
    [HttpGet("enum-options")]
    [ProducesResponseType(typeof(CandidateProfileEnumOptionsDto), StatusCodes.Status200OK)]
    public IActionResult GetEnumOptions()
    {
        return Ok(new CandidateProfileEnumOptionsDto
        {
            GenderOptions       = new[] { "Male", "Female", "Prefer_Not_To_Say" },
            NoticePeriodOptions = new[] { "Immediate", "15 Days", "30 Days", "60 Days", "90 Days" },
            AvailabilityOptions = new[] { "Available", "Open_To_Opportunities", "Not_Looking" },
            DocumentTypes       = new[] { "Resume", "EducationCertificate", "Passport", "Aadhaar" }
        });
    }
}
