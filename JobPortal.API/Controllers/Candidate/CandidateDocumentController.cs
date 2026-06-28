// ============================================================
//  JobPortal.API/Controllers/Candidate/
//  CandidateDocumentController.cs
// ============================================================

using JobPortal.Application.DTOs.Candidate;
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
        _logger = logger;
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
    // UNIFIED DOCUMENT UPLOAD
    // POST /api/candidate/profile/documents
    //
    // One endpoint for EVERY document type (Aadhaar, Passport,
    // Education Certificate, …) — identified by the documentType field.
    // Flow: OCR-parse → verify parsed name matches the candidate's
    // profile name → store in Cloudinary + DB. Rejected on name mismatch.
    // ════════════════════════════════════════════════
    /// <summary>
    /// Upload &amp; verify any candidate document. Send multipart/form-data with:
    /// documentType (e.g. "Aadhaar", "Passport", "EducationCertificate") and
    /// Document (the file). Accepted: PDF, JPEG, PNG · Max 10 MB.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] UploadDocumentRequest request,
        [FromQuery] Guid? candidateId = null)
    {
        if (request?.Document == null || request.Document.Length == 0)
            return BadRequest(new { success = false, message = "Please upload a document." });

        var id = candidateId ?? GetCandidateId();
        if (id == Guid.Empty)
            return BadRequest(new { message = "Unable to resolve candidate identity." });

        var result = await _docService.UploadAndVerifyDocumentAsync(
            id, request.Document);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}