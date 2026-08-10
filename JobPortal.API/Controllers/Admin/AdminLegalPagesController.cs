using JobPortal.API.Middleware;
using JobPortal.Application.DTOs.Admin.LegalPages;
using JobPortal.Domain.Enums;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JobPortal.API.Controllers.Admin
{
    /// <summary>
    /// Backs the "Legal Pages" admin screen (Privacy Policy / Terms &amp; Conditions
    /// editor). {type} is always "privacy" or "terms".
    /// </summary>
    [ApiController]
    [Route("api/admin/legal-pages")]
    //[Authorize] // TODO: apply your Admin role/policy here
    public class AdminLegalPagesController : ControllerBase
    {
        private readonly ILegalDocumentService _legalDocumentService;

        public AdminLegalPagesController(ILegalDocumentService legalDocumentService)
        {
            _legalDocumentService = legalDocumentService;
        }

        /// <summary>GET api/admin/legal-pages — both documents, for the tabbed editor.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _legalDocumentService.GetAllAsync();
            return Ok(result);
        }

        /// <summary>GET api/admin/legal-pages/{type} — e.g. privacy | terms.</summary>
        [HttpGet("{type}")]
        public async Task<IActionResult> GetByType(string type)
        {
            var result = await _legalDocumentService.GetByTypeAsync(type);

            if (result == null)
                return NotFound(new { success = false, message = $"Legal document '{type}' not found." });

            return Ok(result);
        }

        /// <summary>PUT api/admin/legal-pages/{type} — save editor changes without publishing.</summary>
        [HttpPut("{type}")]
        [AuditLog("Save Legal Document Draft", "Legal Pages", AuditSeverity.Info)]
        public async Task<IActionResult> SaveDraft(string type, [FromBody] SaveLegalDocumentRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // TODO: replace with the authenticated admin's id once [Authorize] is wired up.
            var adminId = Guid.Parse("5c1ecae1-543d-11f1-9571-3448ed0f248a");

            var result = await _legalDocumentService.SaveDraftAsync(type, request, adminId);

            if (result == null)
                return NotFound(new { success = false, message = $"Legal document '{type}' not found." });

            return Ok(new { success = true, message = "Draft saved.", data = result });
        }

        /// <summary>POST api/admin/legal-pages/{type}/publish — publish the given content live.</summary>
        [HttpPost("{type}/publish")]
        [AuditLog("Publish Legal Document", "Legal Pages", AuditSeverity.Warning)]
        public async Task<IActionResult> Publish(string type, [FromBody] SaveLegalDocumentRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var adminId = Guid.Parse("5c1ecae1-543d-11f1-9571-3448ed0f248a");

            var result = await _legalDocumentService.PublishAsync(type, request, adminId);

            if (result == null)
                return NotFound(new { success = false, message = $"Legal document '{type}' not found." });

            return Ok(new { success = true, message = "Published successfully.", data = result });
        }

        /// <summary>POST api/admin/legal-pages/{type}/discard — revert unpublished draft edits.</summary>
        [HttpPost("{type}/discard")]
        [AuditLog("Discard Legal Document Draft", "Legal Pages", AuditSeverity.Info)]
        public async Task<IActionResult> Discard(string type)
        {
            var result = await _legalDocumentService.DiscardDraftAsync(type);

            if (result == null)
                return NotFound(new { success = false, message = $"Legal document '{type}' not found." });

            return Ok(new { success = true, message = "Draft changes discarded.", data = result });
        }
    }
}