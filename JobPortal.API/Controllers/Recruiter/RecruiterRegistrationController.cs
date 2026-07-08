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
      [FromBody] GstCheckRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> CompanyDetails(
          [FromForm] CompanyDetailsRequestDto request,
          [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new
                {
                    message = "X-Session-Id header is required."
                });
            }
            // BusinessType validation
            if (string.IsNullOrWhiteSpace(request.BusinessType))
            {
                return BadRequest(new
                {
                    message = "Business Type is required."
                });
            }

            // CompanySize validation
            if (request.CompanySize.HasValue &&
                !Enum.IsDefined(typeof(CompanySize), request.CompanySize.Value))
            {
                return BadRequest(new
                {
                    message = "Invalid CompanySize value."
                });
            }


            // Logo validation before hitting service
            if (request.CompanyLogo != null)
            {
                var allowedTypes = new[]
                {
            "image/jpeg",
            "image/jpg",
            "image/png"
        };

                if (!allowedTypes.Contains(request.CompanyLogo.ContentType))
                {
                    return BadRequest(new
                    {
                        message = "Company logo must be JPG or PNG."
                    });
                }

                if (request.CompanyLogo.Length > 2 * 1024 * 1024)
                {
                    return BadRequest(new
                    {
                        message = "Company logo must be under 2 MB."
                    });
                }
            }

            var result = await _service.SaveCompanyDetailsAsync(
                request,
                sessionId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        // ════════════════════════════════════════════════
        // STEP 3A — Save Contact Details + Send OTP
        // POST /api/recruiter/registration/contact-send-otp
        // Header: X-Session-Id
        // Body: form-data { contactPersonName, designation, companyEmail,
        //                   mobileNumber, countryCode, companyDescription }
        // ════════════════════════════════════════════════
        [HttpPost("contact-details")]
        [ProducesResponseType(typeof(ContactDetailsResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ContactDetails(
        [FromBody] ContactDetailsRequestDto request,
        [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new
                {
                    message = "X-Session-Id header is required."
                });

            // Validate mobile number
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    request.MobileNumber,
                    @"^\d{10,13}$"))
            {
                return BadRequest(new
                {
                    message = "Mobile number must be 10-13 digits."
                });
            }

            // Validate country code
            if (!request.CountryCode.StartsWith("+"))
            {
                return BadRequest(new
                {
                    message = "Country code must start with '+' (e.g. +91)."
                });
            }

            var result =
                await _service.SaveContactDetailsAsync(
                    request,
                    sessionId);

            // Duplicate mobile/email
            if (!result.Success &&
                (result.Message.Contains("already registered") ||
                 result.Message.Contains("already exists")))
            {
                return Conflict(result);
            }

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("send-mobile-otp")]
        [ProducesResponseType(typeof(OtpResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendMobileOtp(
      [FromBody] SendMobileOtpRequestDto request,
      [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new
                {
                    message = "X-Session-Id header is required."
                });

            var result =
                await _service.SendMobileOtpAsync(request, sessionId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("verify-mobile-otp")]
        [ProducesResponseType(typeof(OtpResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerifyMobileOtp(
    [FromBody] VerifyMobileOtpRequestDto request,
    [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new
                {
                    message = "X-Session-Id header is required."
                });

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    request.MobileOtpCode,
                    @"^\d{6}$"))
            {
                return BadRequest(new
                {
                    message = "OTP must be exactly 6 digits."
                });
            }

            var result =
                await _service.VerifyMobileOtpAsync(request, sessionId);

            if (!result.Success &&
                result.Message.Contains("Too many"))
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    result);
            }

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("resend-mobile-otp")]
        [ProducesResponseType(typeof(OtpResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendMobileOtp(
    [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new
                {
                    message = "X-Session-Id header is required."
                });
            }

            var result =
                await _service.ResendMobileOtpAsync(sessionId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("send-email-otp")]
        [ProducesResponseType(typeof(OtpResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendEmailOtp(
        [FromBody] SendEmailOtpRequestDto request,
        [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new
                {
                    message = "X-Session-Id header is required."
                });

            var result =
                await _service.SendEmailOtpAsync(request, sessionId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("verify-email-otp")]
        [ProducesResponseType(typeof(OtpResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerifyEmailOtp(
    [FromBody] VerifyEmailOtpRequestDto request,
    [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new
                {
                    message = "X-Session-Id header is required."
                });

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    request.EmailOtpCode,
                    @"^\d{6}$"))
            {
                return BadRequest(new
                {
                    message = "OTP must be exactly 6 digits."
                });
            }

            var result =
                await _service.VerifyEmailOtpAsync(request, sessionId);

            if (!result.Success &&
                result.Message.Contains("Too many"))
            {
                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    result);
            }

            return result.Success
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("resend-email-otp")]
        [ProducesResponseType(typeof(OtpResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendEmailOtp(
    [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return BadRequest(new
                {
                    message = "X-Session-Id header is required."
                });
            }

            var result =
                await _service.ResendEmailOtpAsync(sessionId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
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
        [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<IActionResult> UploadLicences(
       [FromForm] LicencesRequestDto request,
       [FromHeader(Name = "X-Session-Id")] string? sessionId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (string.IsNullOrWhiteSpace(sessionId))
                return BadRequest(new
                {
                    message = "X-Session-Id header is required."
                });

            var result = await _service.UploadLicencesAsync(
                request,
                sessionId);

            return result.Success
                ? Ok(result)
                : BadRequest(result);
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
        //[HttpGet("enum-options")]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //public IActionResult GetEnumOptions()
        //{
        //    return Ok(new
        //    {
        //        industryTypes = Enum.GetNames(typeof(IndustryType)),
        //        businessTypes = Enum.GetNames(typeof(BusinessType)),
        //        companySizes = Enum.GetNames(typeof(CompanySize)),
        //    });
        //}


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