using Microsoft.AspNetCore.Mvc;

namespace job_portal_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelloController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                message = "Hello API Working Successfully"
            });
        }
    }
}