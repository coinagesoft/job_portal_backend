using JobPortal.Services.IImplement.IRecruiter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace JobPortal.API.Controllers.Recruiter
{
    [ApiController]
    [Route("api/recruiter/document-types")]
    [Authorize] // TODO: apply your Recruiter role/policy here
    public class DocumentTypesController : ControllerBase
    {
        private readonly IDocumentTypeService _documentTypeService;

        public DocumentTypesController(IDocumentTypeService documentTypeService)
        {
            _documentTypeService = documentTypeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveTypes([FromHeader] Guid employerId)
        {
            var result = await _documentTypeService.GetActiveDocumentTypesAsync(employerId);
            return Ok(result);
        }
    }
}
