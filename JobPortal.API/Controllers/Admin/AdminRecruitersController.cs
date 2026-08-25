using JobPortal.API.Middleware;
using JobPortal.Application.DTOs;
using JobPortal.Application.DTOs.Admin;
using JobPortal.Domain.Enums;
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

            var jwtId = User.FindFirst("jti")?.Value;

            try
            {
                var updated = await _adminRecruiterService
                    .UpdateRecruiterStatusAsync(
                        id,
                        request.AccountStatus,
                        request.Reason,
                        performedByAdminId,
                        ipAddress,
                        userAgent,
                        jwtId
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

        // GET /api/admin/recruiters/{id}/transactions
        // Backs the "Transaction History" table on the recruiter detail
        // page (/admin/recruiters/details?id=). Every membership /
        // credit-pack / fee payment made by this recruiter, each with an
        // invoice number + a downloadable invoice URL when one exists.
        [HttpGet("{id:guid}/transactions")]
        public async Task<IActionResult> GetRecruiterTransactions(Guid id)
        {
            var transactions = await _adminRecruiterService
                .GetRecruiterTransactionsAsync(id);

            if (transactions == null)
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
                message = "Recruiter transactions retrieved successfully.",
                data = transactions
            });
        }

        // GET /api/admin/recruiters/{id}/transactions/{transactionId}/invoice/download
        // Streams a freshly generated invoice PDF for one of this
        // recruiter's transactions (nothing is stored on S3, so it's
        // regenerated on every request). Returns 404 if the transaction
        // doesn't exist, isn't this recruiter's, or has no invoice.
        [HttpGet("{id:guid}/transactions/{transactionId:guid}/invoice/download")]
        public async Task<IActionResult> DownloadRecruiterInvoice(Guid id, Guid transactionId)
        {
            var result = await _adminRecruiterService
                .DownloadRecruiterInvoicePdfAsync(id, transactionId);

            if (result == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Invoice not found for this transaction."
                });
            }

            return File(result.Value.Bytes, "application/pdf", result.Value.FileName);
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
                    Request.Headers["User-Agent"].ToString(),

                JwtId =
                    User.FindFirst("jti")?.Value
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

        [HttpGet("company-required-document-verification/{employerId:guid}")]
        public async Task<IActionResult> GetCompanyRequiredDocumentVerification(
    Guid employerId)
        {
            try
            {
                var result =
                    await _adminRecruiterService
                        .GetCompanyRequiredDocumentVerificationAsync(
                            employerId);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Company not found."
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Company required document verification fetched successfully.",
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
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "An error occurred while fetching company required document verification.",
                        error = ex.Message
                    });
            }
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


        [HttpPatch("document-types/{documentTypeId:guid}/updatStatus/requiredDoc")]
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


        /// <summary>
        /// Soft delete a document type (e.g. junk/test entries added via
        /// the "custom doc" input). Sets IsActive = false so it drops out
        /// of the admin dropdown, chip list, and required-doc set, while
        /// keeping the row for any already-linked employer documents.
        /// </summary>
        [HttpDelete("document-types/{documentTypeId:guid}")]
        [AuditLog("Delete Document Type", "Document Types", AuditSeverity.Warning)]
        public async Task<IActionResult> DeleteDocumentType(Guid documentTypeId)
        {
            var deleted = await _adminRecruiterService.DeleteDocumentTypeAsync(documentTypeId);

            if (!deleted)
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
                message = "Document type deleted successfully."
            });
        }


        [HttpGet("document-types/masterAllDocuments")]
        public async Task<IActionResult> GetDocumentRequirements()
        {
            try
            {
                var documents =
                    await _adminRecruiterService
                        .GetDocumentRequirementsAsync();

                return Ok(new
                {
                    success = true,
                    message = "Document requirements retrieved successfully.",
                    data = documents
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to retrieve document requirements.",
                    error = ex.Message
                });
            }
        }


        [HttpGet("Alloptional/names")]
        public async Task<IActionResult> GetOptionalDocumentNames()
        {
            var documentNames =
                await _adminRecruiterService.GetOptionalDocumentNamesAsync();

            return Ok(new
            {
                success = true,
                message = "Optional document names retrieved successfully.",
                data = documentNames
            });
        }


        [HttpPost("{employerId:guid}/document-requests")]
        public async Task<IActionResult> RequestRecruiterDocument(
          Guid employerId,
         [FromBody] RequestRecruiterDocumentDto request)
        {
            // --------------------------------------------------
            // GET ADMIN ID FROM JWT
            // --------------------------------------------------

            var adminIdClaim = User.FindFirst("AdminId")?.Value;

            if (!Guid.TryParse(adminIdClaim, out var adminId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Invalid admin authentication."
                });
            }

            try
            {
                // --------------------------------------------------
                // REQUEST DOCUMENT
                // --------------------------------------------------

                var result =
                    await _adminRecruiterService
                        .RequestRecruiterDocumentAsync(
                            employerId,
                            request,
                            adminId);

                // --------------------------------------------------
                // RESPONSE
                // --------------------------------------------------

                return Ok(new
                {
                    success = true,
                    message = "Document request sent successfully.",
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
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

    }
}