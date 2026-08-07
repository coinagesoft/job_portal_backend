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
    [Route("api/admin/document-types")]
    //[Authorize] // TODO: apply your Admin role/policy here
    public class AdminDocumentTypesController : ControllerBase
    {
        private readonly IAdminDocumentTypeService _adminDocumentTypeService;

        public AdminDocumentTypesController(IAdminDocumentTypeService adminDocumentTypeService)
        {
            _adminDocumentTypeService = adminDocumentTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _adminDocumentTypeService.GetAllAsync();
            return Ok(result);
        }


        /// <summary>
        /// Create a new document type.
        /// </summary>
        [HttpPost]
        [AuditLog("Create Document Type", "Document Types", AuditSeverity.Info)]
        public async Task<IActionResult> Create(
            [FromBody] CreateDocumentTypeRequestDto request)
        {
            var result = await _adminDocumentTypeService.CreateAsync(request);

            if (result == null)
                return BadRequest(new
                {
                    success = false,
                    message = "Unable to create document type."
                });

            return Ok(new
            {
                success = true,
                message = "Document type created successfully.",
                data = result
            });
        }

        [HttpPatch("{id:guid}")]
        [AuditLog("Update Document Type", "Document Types", AuditSeverity.Warning)]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateDocumentTypeRequestDto request)
        {
            var result = await _adminDocumentTypeService.UpdateAsync(id, request);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Soft delete (deactivate) a document type.
        /// </summary>
        // Explicitly Warning, not Critical: this is a soft delete
        // (deactivation) of a reference/config value, not destructive
        // removal of user data. Without this attribute the generic
        // "any action with 'delete' in it is Critical" heuristic in
        // AuditLogMiddleware would tag it Critical, which overstates
        // the real impact.
        [HttpDelete("{id:guid}")]
        [AuditLog("Delete Document Type", "Document Types", AuditSeverity.Warning)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _adminDocumentTypeService.DeleteAsync(id);

            if (!deleted)
                return NotFound(new
                {
                    success = false,
                    message = "Document type not found."
                });

            return Ok(new
            {
                success = true,
                message = "Document type deleted successfully."
            });
        }


    }
}