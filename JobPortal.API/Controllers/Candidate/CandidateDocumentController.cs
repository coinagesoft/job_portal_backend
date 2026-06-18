// ============================================================
//  JobPortal.API/Controllers/Candidate/
//  CandidateDocumentController.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate.Profile;
using JobPortal.Services.IImplement.ICandidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobPortal.API.Controllers.Candidate;

[ApiController]
[Route("api/candidate/profile/documents")]
[Produces("application/json")]
// [Authorize(Roles = "Candidate")]
public class CandidateDocumentController : ControllerBase
{
    private readonly ICandidateDocumentService _docService;
    private readonly ILogger<CandidateDocumentController> _logger;

    public CandidateDocumentController(
        ICandidateDocumentService docService,
        ILogger<CandidateDocumentController> logger)
    {
        _docService = docService;
        _logger     = logger;
    }

    private Guid GetCandidateId()
    {
        var claim = User.FindFirstValue("candidateId")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    // ════════════════════════════════════════════════
    // GET ALL DOCUMENTS
    // GET /api/candidate/profile/documents
    // ════════════════════════════════════════════════
    /// <summary>
    /// Returns all uploaded documents in one response:
    /// resume, education certificates, passport, Aadhaar.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CandidateDocumentsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDocuments([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.GetAllDocumentsAsync(id);
        if (result.Success) return Ok(result);

        return result.Message == "Candidate profile not found."
            ? NotFound(result)
            : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // 2A — RESUME
    // POST   /api/candidate/profile/documents/resume
    // DELETE /api/candidate/profile/documents/resume
    // ════════════════════════════════════════════════
    /// <summary>
    /// Upload or replace resume.
    /// Accepted: PDF, JPEG, PNG · Max 10 MB.
    /// AI parsing (name, phone, email, trade, skills, exp) runs asynchronously.
    /// </summary>
    [HttpPost("resume")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadResumeResponseDtos), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadResume(
        IFormFile resume,
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.UploadResumeAsync(id, resume);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete the candidate's current resume.</summary>
    [HttpDelete("resume")]
    [ProducesResponseType(typeof(DeleteResumeResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteResume([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.DeleteResumeAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // 2B — EDUCATION CERTIFICATE
    // GET    /api/candidate/profile/documents/education-certificate
    // POST   /api/candidate/profile/documents/education-certificate
    // DELETE /api/candidate/profile/documents/education-certificate/{educationId}
    // ════════════════════════════════════════════════
    /// <summary>List all education certificates.</summary>
    [HttpGet("education-certificate")]
    [ProducesResponseType(typeof(CandidateDocumentsResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEducationCertificates([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.GetEducationCertificatesAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Upload an education certificate.
    /// Form fields: educationLevel (10th|12th|ITI|Diploma|Graduate|Other),
    /// instituteName, marksPercentage, passoutYear.
    /// File field: certificate (PDF / JPEG / PNG, max 10 MB).
    /// </summary>
    [HttpPost("education-certificate")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadEducationCertificateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadEducationCertificate(
        [FromForm] UploadEducationCertificateRequestDto request,
        IFormFile certificate,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var validLevels = new[] { "10th", "12th", "ITI", "Diploma", "Graduate", "Other" };
        if (!validLevels.Contains(request.EducationLevel))
            return BadRequest(new { message = $"educationLevel must be one of: {string.Join(", ", validLevels)}" });

        var result = await _docService.UploadEducationCertificateAsync(id, request, certificate);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete a specific education certificate entry.</summary>
    [HttpDelete("education-certificate/{educationId:guid}")]
    [ProducesResponseType(typeof(DeleteEducationCertificateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEducationCertificate(
        Guid educationId,
        [FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.DeleteEducationCertificateAsync(id, educationId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // ════════════════════════════════════════════════
    // 2C — PASSPORT
    // POST   /api/candidate/profile/documents/passport
    // DELETE /api/candidate/profile/documents/passport
    // ════════════════════════════════════════════════
    /// <summary>
    /// Upload passport (front required, back optional).
    /// consentGiven must be true (PDPA/GDPR consent for ID data).
    /// Images: JPEG / PNG / WebP · Max 5 MB each.
    /// Admin review sets adminDecision → Pending → Approved | Rejected.
    /// </summary>
    [HttpPost("passport")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadPassportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPassport(
         [FromForm] UploadPassportRequestDto request,
         [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.UploadPassportAsync(id, request, request.FrontImage, request.BackImage);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete the passport document.</summary>
    [HttpDelete("passport")]
    [ProducesResponseType(typeof(DeletePassportResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePassport([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.DeletePassportAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ════════════════════════════════════════════════
    // 2D — AADHAAR (KYC)
    // POST   /api/candidate/profile/documents/aadhaar
    // DELETE /api/candidate/profile/documents/aadhaar
    // ════════════════════════════════════════════════
    /// <summary>
    /// Upload Aadhaar for KYC verification (front required, back optional).
    /// consentGiven must be true (required by UIDAI guidelines).
    /// OCR + AI name/DOB/address extraction runs automatically.
    /// Admin review sets adminDecision → Pending → Approved | Rejected.
    /// </summary>
    [HttpPost("aadhaar")]
    [RequestSizeLimit(12 * 1024 * 1024)]
    [ProducesResponseType(typeof(UploadAadhaarResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAadhaar(
        [FromForm] UploadAadhaarRequestDto request,
        [FromQuery] Guid? candidateId = null)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.UploadAadhaarAsync(id, request, request.FrontImage, request.BackImage);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete the Aadhaar document (also clears KYC record).</summary>
    [HttpDelete("aadhaar")]
    [ProducesResponseType(typeof(DeleteAadhaarResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAadhaar([FromQuery] Guid? candidateId = null)
    {
        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.DeleteAadhaarAsync(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
