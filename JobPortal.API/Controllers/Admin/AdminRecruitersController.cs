using JobPortal.Application.DTOs;
using JobPortal.Application.DTOs.Admin;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Admin
{

    [ApiController]
    [Route("api/admin/recruiters")]
    [Authorize(Roles = "Admin")]
    public class AdminRecruitersController : ControllerBase
    {
        private readonly IAdminRecruiterService _adminRecruiterService;

        public AdminRecruitersController(IAdminRecruiterService adminRecruiterService)
        {
            _adminRecruiterService = adminRecruiterService;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecruiters()
        {
            var recruiters = await _adminRecruiterService.GetRecruitersAsync();
            return Ok(recruiters);
        }

        [HttpPatch("{id:guid}/account-status")]
        public async Task<IActionResult> UpdateAccountStatus(
            Guid id,
            [FromBody] UpdateAccountStatusRequestDto request)
        {
            var adminIdClaim = User.FindFirst("AdminId")?.Value;

            if (!Guid.TryParse(adminIdClaim, out var performedByAdminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid admin authentication."
                });
            }

            var ipAddress =
                HttpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown";

            var userAgent =
                Request.Headers["User-Agent"].ToString();

            try
            {
                var updated = await _adminRecruiterService
                    .UpdateRecruiterStatusAsync(
                        id,
                        request.AccountStatus,
                        request.Reason,
                        performedByAdminId,
                        ipAddress,
                        userAgent
                    );

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Recruiter account not found."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"Recruiter account status updated to '{request.AccountStatus}' successfully.",
                    status = request.AccountStatus
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetRecruiterDetail(Guid id)
        {
            var detail = await _adminRecruiterService
                .GetRecruiterDetailAsync(id);

            if (detail == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Recruiter not found."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Recruiter details retrieved successfully.",
                data = detail
            });
        }

        [HttpGet("{id:guid}/documents")]
        public async Task<IActionResult> GetRecruiterDocuments(Guid id)
        {
            var documents =
                await _adminRecruiterService
                    .GetRecruiterDocumentsAsync(id);

            if (documents == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Recruiter not found."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Recruiter documents retrieved successfully.",
                data = documents
            });
        }


        [HttpPatch("documents/{documentId:guid}/status")]
        public async Task<IActionResult> UpdateDocumentStatus(
            Guid documentId,
            [FromBody] UpdateRecruiterDocumentStatusRequestDto request)
        {
            var adminIdClaim =
                User.FindFirst("AdminId")?.Value;

            if (!Guid.TryParse(
                adminIdClaim,
                out var adminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid admin authentication."
                });
            }

            var audit = new AdminAuditContext
            {
                AdminId = adminId,

                AdminName =
                    User.FindFirst(ClaimTypes.Name)?.Value
                    ?? "Unknown",

                AdminRole =
                    User.FindFirst(ClaimTypes.Role)?.Value
                    ?? "Admin",

                IpAddress =
                    HttpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown",

                UserAgent =
                    Request.Headers["User-Agent"].ToString()
            };

            try
            {
                var updated =
                    await _adminRecruiterService
                        .UpdateRecruiterDocumentStatusAsync(
                            documentId,
                            request,
                            audit);

                if (!updated)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Recruiter document not found."
                    });
                }

                var message = request.Status.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase)
                        ? "Recruiter document verified successfully."
                        : request.Status.Equals(
                            "Rejected",
                            StringComparison.OrdinalIgnoreCase)
                            ? "Recruiter document rejected successfully."
                            : request.Status.Equals(
                                "Resubmission",
                                StringComparison.OrdinalIgnoreCase)
                                ? "Document resubmission requested successfully."
                                : "Recruiter document status updated successfully.";

                return Ok(new
                {
                    success = true,
                    message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [HttpGet("{id:guid}/document-checklist")]
        public async Task<IActionResult> GetDocumentChecklist(Guid id)
        {
            var checklist = await _adminRecruiterService
                .GetRecruiterDocumentChecklistAsync(id);

            if (checklist == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Recruiter not found."
                });
            }

            return Ok(new
            {
                success = true,
                message = "Recruiter document checklist retrieved successfully.",
                data = checklist
            });
        }

        [HttpPost("document-types/optional")]
        public async Task<IActionResult> CreateOptionalDocumentType(
    [FromBody] CreateOptionalDocumentTypeRequestDto request)
        {
            try
            {
                var result =
                    await _adminRecruiterService
                        .CreateOptionalDocumentTypeAsync(request);

                if (result == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Unable to create document type."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Optional document type created successfully.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating document type.",
                    error = ex.Message
                });
            }
        }


        [HttpPatch("document-types/{documentTypeId:guid}/requirement")]
        public async Task<IActionResult> UpdateDocumentRequirement(
            Guid documentTypeId,
            [FromBody] UpdateDocumentRequirementRequestDto request)
        {
            try
            {
                var result =
                    await _adminRecruiterService.UpdateDocumentRequirementAsync(
                        documentTypeId,
                        request);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Document type not found."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = request.IsMandatory
                        ? "Document marked as required successfully."
                        : "Document marked as optional successfully.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


    }
}
