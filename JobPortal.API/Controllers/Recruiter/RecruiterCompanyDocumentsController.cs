using JobPortal.Application.DTOs.Recruiter.CompanyDocuments;
using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/company-documents")]
    [Authorize] // TODO: apply your Recruiter role/policy here to match the rest of the recruiter controllers
    public class RecruiterCompanyDocumentsController : ControllerBase
    {
        private readonly ICompanyDocumentService _companyDocumentService;

        public RecruiterCompanyDocumentsController(ICompanyDocumentService companyDocumentService)
        {
            _companyDocumentService = companyDocumentService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(
            [FromHeader] Guid employerId,
            [FromForm] UploadCompanyDocumentRequestDto request)
        {
            var result = await _companyDocumentService.UploadAsync(employerId, request);

            if (result == null)
                return BadRequest("Unable to upload document.");

            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMine([FromHeader] Guid employerId)
        {
            var result = await _companyDocumentService.GetMyDocumentsAsync(employerId);
            return Ok(result);
        }

        [HttpGet("{documentId:guid}")]
        public async Task<IActionResult> GetById(
            [FromHeader] Guid employerId, Guid documentId)
        {
            var result = await _companyDocumentService.GetByIdAsync(employerId, documentId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPut("{documentId:guid}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Update(
            [FromHeader] Guid employerId,
            Guid documentId,
            [FromForm] UpdateCompanyDocumentRequestDto request)
        {
            var result = await _companyDocumentService.UpdateAsync(employerId, documentId, request);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("{documentId:guid}")]
        public async Task<IActionResult> Delete(
            [FromHeader] Guid employerId, Guid documentId)
        {
            var success = await _companyDocumentService.DeleteAsync(employerId, documentId);

            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
