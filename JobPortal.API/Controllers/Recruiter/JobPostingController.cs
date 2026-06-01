
using global::JobPortal.Application.DTOs.Recruiter;
using global::JobPortal.Services.IImplement.IRecruiter;
using JobPortal.Application.DTOs.JobPosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Recruiter;

    [ApiController]
    [Route("api/recruiter/jobs")]
    //[Authorize(Roles = "Recruiter")]
    public class JobPostingController : ControllerBase
    {
        private readonly IJobPostingService _service;

        public JobPostingController(IJobPostingService service)
            => _service = service;

    //private Guid GetEmployerId() =>
    //    Guid.Parse(User.FindFirst("employer_id")?.Value
    //        ?? throw new UnauthorizedAccessException("Employer ID not found in token."));

    private Guid GetEmployerId() =>
    Guid.Parse("64de0929-cf0c-4e8f-b842-d536cc1dd012");

    // ── Role Search (no auth needed) ───────────────────
    [HttpGet("search-roles")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchRoles([FromQuery] string q)
        {
            var result = await _service.SearchRolesAsync(q);
            return Ok(result);
        }

        // ── STEP 1 ─────────────────────────────────────────
        /// <summary>
        /// Step 1 — Job Details. Creates a Draft job. Returns jobId for all next steps.
        /// </summary>
        [HttpPost("step1-job-details")]
        public async Task<IActionResult> Step1JobDetails(
            [FromForm] JobDetailsRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.SaveJobDetailsAsync(request, GetEmployerId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── STEP 2 ─────────────────────────────────────────
        /// <summary>
        /// Step 2 — Compensation. Pass jobId from Step 1.
        /// </summary>
        [HttpPut("{jobId}/step2-compensation")]
        public async Task<IActionResult> Step2Compensation(
            Guid jobId, [FromBody] CompensationRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.SaveCompensationAsync(request, jobId, GetEmployerId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── STEP 3 ─────────────────────────────────────────
        [HttpPut("{jobId}/step3-skills")]
        public async Task<IActionResult> Step3Skills(
            Guid jobId, [FromForm] SkillsRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.SaveSkillsAsync(request, jobId, GetEmployerId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── STEP 4 ─────────────────────────────────────────
        [HttpPut("{jobId}/step4-eligibility")]
        public async Task<IActionResult> Step4Eligibility(
            Guid jobId, [FromForm] EligibilityRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.SaveEligibilityAsync(request, jobId, GetEmployerId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── STEP 5 ─────────────────────────────────────────
        [HttpPut("{jobId}/step5-location")]
        public async Task<IActionResult> Step5Location(
            Guid jobId, [FromForm] LocationRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.SaveLocationAsync(request, jobId, GetEmployerId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── STEP 6 ─────────────────────────────────────────
        [HttpPut("{jobId}/step6-questions")]
        public async Task<IActionResult> Step6Questions(
            Guid jobId, [FromForm] QuestionsRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.SaveQuestionsAsync(request, jobId, GetEmployerId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── STEP 7 ─────────────────────────────────────────
        /// <summary>
        /// Step 7 — Publish or Save Draft. Set publishNow=true to go live.
        /// </summary>
        [HttpPut("step7-publish")]
        public async Task<IActionResult> Step7Publish(
            [FromForm] PublishingRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _service.PublishJobAsync(request, GetEmployerId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── SAVE DRAFT (any time) ──────────────────────────
        [HttpPut("{jobId}/save-draft")]
        public async Task<IActionResult> SaveDraft(Guid jobId)
        {
            var result = await _service.SaveDraftAsync(jobId, GetEmployerId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ── RESUME ─────────────────────────────────────────
        [HttpGet("{jobId}/resume")]
        public async Task<IActionResult> ResumeJob(Guid jobId)
        {
            var result = await _service.ResumeJobAsync(jobId, GetEmployerId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
