using JobPortal.Application.DTOs.Recruiter;
using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.Common;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/registration")]
    [Produces("application/json")]
    public class RecruiterRegistrationController : ControllerBase
    {
        private readonly IRecruiterRegistrationService _service;

        public RecruiterRegistrationController(
            IRecruiterRegistrationService service)
            => _service = service;

        // ════════════════════════════════════════════════
        // STEP 1 — GST Check
        // POST /api/recruiter/registration/gst-check
        // Body: JSON { gstRegistered: bool, industryType: string }
        // Returns: sessionId to carry through all other steps
        // ════════════════════════════════════════════════
        [HttpPost("gst-check")]
        [ProducesResponseType(typeof(GstCheckResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GstCheck(
            [FromForm] GstCheckRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validate IndustryType enum value
            if (!Enum.IsDefined(typeof(IndustryType), request.IndustryType))
                return BadRequest(new { message = "Invalid IndustryType value." });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var result = await _service.CheckGstAsync(request, ip);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ════════════════════════════════════════════════
        // STEP 2 — Company Details + Logo Upload
        // POST /api/recruiter/registration/company-details
        // Header: X-Session-Id (from step 1)
        // Body: multipart/form-data (has CompanyLogo file)
        // ════════════════════════════════════════════════
        [HttpPost("company-details")]
        [ProducesResponseType(typeof(CompanyDetailsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10MB max request
        public async Task<IActionResult> CompanyDetails(
            [FromForm] CompanyDetailsRequestDto request,
            [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { message = "X-Session-Id header is required." });

            // Validate BusinessType enum
            if (!Enum.IsDefined(typeof(BusinessType), request.BusinessType))
                return BadRequest(new { message = "Invalid BusinessType value." });

            // Validate CompanySize enum if provided
            if (request.CompanySize.HasValue &&
                !Enum.IsDefined(typeof(CompanySize), request.CompanySize.Value))
                return BadRequest(new { message = "Invalid CompanySize value." });

            var result = await _service.SaveCompanyDetailsAsync(request, sessionId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ════════════════════════════════════════════════
        // STEP 3A — Save Contact Details + Send OTP
        // POST /api/recruiter/registration/contact-send-otp
        // Header: X-Session-Id
        // Body: form-data { contactPersonName, designation, companyEmail,
        //                   mobileNumber, countryCode, companyDescription }
        // ════════════════════════════════════════════════
        [HttpPost("contact-send-otp")]
        [ProducesResponseType(typeof(ContactDetailsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ContactSendOtp(
            [FromForm] ContactDetailsRequestDto request,
            [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { message = "X-Session-Id header is required." });

            // Validate mobile number format
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    request.MobileNumber, @"^\d{10,13}$"))
                return BadRequest(new { message = "Mobile number must be 10–13 digits." });

            // Validate country code format
            if (!request.CountryCode.StartsWith("+"))
                return BadRequest(new { message = "Country code must start with '+' (e.g. +91)." });

            var result = await _service.SaveContactAndSendOtpAsync(request, sessionId);

            // Duplicate mobile/email → 409 Conflict, not 400
            if (!result.Success &&
                (result.Message.Contains("already registered") ||
                 result.Message.Contains("already exists")))
                return Conflict(result);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ════════════════════════════════════════════════
        // STEP 3B — Verify OTP
        // POST /api/recruiter/registration/verify-otp
        // Header: X-Session-Id
        // Body: form-data { mobileNumber, countryCode, otpCode }
        // ════════════════════════════════════════════════
        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(VerifyContactOtpResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerifyOtp(
            [FromForm] VerifyContactOtpRequestDto request,
            [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { message = "X-Session-Id header is required." });

            // Validate OTP format — must be exactly 6 digits
            if (!System.Text.RegularExpressions.Regex.IsMatch(request.OtpCode, @"^\d{6}$"))
                return BadRequest(new { message = "OTP must be exactly 6 digits." });

            var result = await _service.VerifyContactOtpAsync(request, sessionId);

            // Too many attempts → 429
            if (!result.Success && result.Message.Contains("Too many"))
                return StatusCode(StatusCodes.Status429TooManyRequests, result);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ════════════════════════════════════════════════
        // STEP 4 — Upload Licences (POE / RPSL)
        // POST /api/recruiter/registration/upload-licences
        // Header: X-Session-Id
        // Body: multipart/form-data
        //   - poeLicence  (file, optional)
        //   - rpslLicence (file, optional)
        //   - skipLicences (bool)
        // ════════════════════════════════════════════════
        [HttpPost("upload-licences")]
        [ProducesResponseType(typeof(LicencesResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20MB max (two 5MB files + overhead)
        public async Task<IActionResult> UploadLicences(
            [FromForm] LicencesRequestDto request,
            [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new { message = "X-Session-Id header is required." });

            // If not skipping, at least one file should be present
            if (!request.SkipLicences &&
                (request.PoeLicence == null || request.PoeLicence.Length == 0) &&
                (request.RpslLicence == null || request.RpslLicence.Length == 0))
                return BadRequest(new
                {
                    message = "Please upload at least one licence file, or set skipLicences=true."
                });

            var result = await _service.UploadLicencesAsync(request, sessionId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ════════════════════════════════════════════════
        // STEP 5 — Final Review & Submit
        // POST /api/recruiter/registration/submit
        // Body: JSON (all data assembled from previous steps)
        //   ReviewSubmitRequestDto contains:
        //     - GstDetails    (GstCheckRequestDto)
        //     - CompanyDetails (CompanyDetailsRequestDto — with logoUrl, not file)
        //     - ContactDetails (ContactDetailsRequestDto)
        //     - Licences      (LicencesResponseDto — with S3 URLs, not files)
        //     - ConsentGiven  (bool)
        // ════════════════════════════════════════════════
        [HttpPost("submit-registration")]
        [ProducesResponseType(typeof(ReviewSubmitResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SubmitRegistration(
    [FromBody] ReviewSubmitRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var result = await _service.SubmitRegistrationAsync(request, ip);

            return result.Success ? Ok(result) : BadRequest(result);
        }
        // ════════════════════════════════════════════════
        // UTILITY — Get enum values for dropdowns
        // GET /api/recruiter/registration/enum-options
        // Frontend can call this once to populate all dropdowns
        // ════════════════════════════════════════════════
        [HttpGet("enum-options")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetEnumOptions()
        {
            return Ok(new
            {
                industryTypes = Enum.GetNames(typeof(IndustryType)),
                businessTypes = Enum.GetNames(typeof(BusinessType)),
                companySizes = Enum.GetNames(typeof(CompanySize)),
            });
        }


        /// <summary>
        /// Resume a saved registration from any step.
        /// Returns current progress and pre-filled data.
        /// </summary>
        [HttpGet("resume/{sessionId}")]
        public async Task<IActionResult> ResumeSession(string sessionId)
        {
            var result = await _service.ResumeSessionAsync(sessionId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

    }
}