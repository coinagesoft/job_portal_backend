using JobPortal.Services.Implement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Public;

[ApiController]
[Route("api/visitor")]
[AllowAnonymous]
public sealed class VisitorCountryController : ControllerBase
{
    private readonly VisitorCountryService _visitorCountryService;

    public VisitorCountryController(VisitorCountryService visitorCountryService) =>
        _visitorCountryService = visitorCountryService;

    /// <summary>Detects visitor country from the client IP and selects the payment gateway.</summary>
    [HttpGet("payment-region")]
    public async Task<IActionResult> GetPaymentRegion(CancellationToken cancellationToken)
    {
        var result = await _visitorCountryService.ResolveAsync(HttpContext.Connection.RemoteIpAddress, cancellationToken);
        if (result is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                message = "We could not detect your country. Please refresh the page or contact support."
            });
        }

        return Ok(new { success = true, data = result });
    }
}
