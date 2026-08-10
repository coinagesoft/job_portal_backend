// ============================================================
//  JobPortal.API/Controllers/Public/LegalPagesController.cs
//  Base route: api/public
// ============================================================

using JobPortal.Services.IImplement.IPublic;
using JobPortal.Services.Implement.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace JobPortal.API.Controllers.Public;

[ApiController]
[Route("api/public")]
[Produces("application/json")]
public class LegalPagesController : ControllerBase
{
    private readonly ILegalDocumentPublicService _legalDocumentPublicService;

    public LegalPagesController(ILegalDocumentPublicService legalDocumentPublicService)
    {
        _legalDocumentPublicService = legalDocumentPublicService;
    }

    /// <summary>
    /// GET api/public/legal-pages/{type}
    /// The one public endpoint candidates/employers use to show a legal page.
    /// {type} is "privacy" or "terms".
    ///
    /// e.g. GET api/public/legal-pages/privacy
    ///      GET api/public/legal-pages/terms
    /// </summary>
    [HttpGet("legal-pages/{type}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLegalPage(string type)
    {
        var result = await _legalDocumentPublicService.GetPublishedAsync(type);

        if (result == null)
            return NotFound(new
            {
                success = false,
                message = $"'{type}' has not been published yet."
            });

        return Ok(new { success = true, data = result });
    }
}