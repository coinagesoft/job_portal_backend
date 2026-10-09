// Put in: JobPortal.API/Controllers/UploadsController.cs
// Replaces app.UseStaticFiles() for /uploads/*. The bucket stays PRIVATE;
// only your API (via the EC2 IAM role) can read it.
using JobPortal.Services.Implement.Recruiter;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers
{
    [ApiController]
    public class UploadsController : ControllerBase
    {
        private readonly S3FileStorageService _storage;
        public UploadsController(S3FileStorageService storage) => _storage = storage;

        [HttpGet("/uploads/{**key}")]
        public async Task<IActionResult> Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || key.Contains(".."))
                return BadRequest();

            var obj = await _storage.GetAsync(key);
            if (obj == null) return NotFound();

            Response.Headers["Cache-Control"] = "public, max-age=86400";
            return File(obj.Value.Stream, obj.Value.ContentType);
        }
    }
}