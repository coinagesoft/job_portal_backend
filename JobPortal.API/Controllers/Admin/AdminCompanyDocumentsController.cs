using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.CompanyDocuments;
using JobPortal.Domain.Enums;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/company-documents")]
    //[Authorize] // TODO: apply your Admin role/policy here
    public class AdminCompanyDocumentsController : ControllerBase
    {
        private readonly IAdminCompanyDocumentService _adminCompanyDocumentService;

        public AdminCompanyDocumentsController(IAdminCompanyDocumentService adminCompanyDocumentService)
        {
            _adminCompanyDocumentService = adminCompanyDocumentService;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var result = await _adminCompanyDocumentService.GetPendingAsync();
            return Ok(result);
        }

        [HttpPost("{documentId:guid}/verify")]
        [AuditLog("Verify Company Document", "Company Documents", AuditSeverity.Warning)]
        public async Task<IActionResult> Verify(
            [FromHeader] Guid adminUserId,
            Guid documentId,
            [FromBody] VerifyCompanyDocumentRequestDto request)
        {
            var success = await _adminCompanyDocumentService.VerifyAsync(
                adminUserId, documentId, request);

            if (!success)
                return BadRequest("Unable to verify document.");

            return NoContent();
        }
    }
}