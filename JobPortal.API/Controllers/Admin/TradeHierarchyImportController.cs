using JobPortal.Services.IImplement.IAdmin;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/trade-hierarchy")]
    public class TradeHierarchyImportController : ControllerBase
    {
        private readonly ITradeHierarchyImportService _importService;

        public TradeHierarchyImportController(
            ITradeHierarchyImportService importService)
        {
            _importService = importService;
        }

        [HttpPost("import")]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB
        public async Task<IActionResult> ImportExcel(
            IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please upload an Excel file."
                });
            }

            var extension = Path.GetExtension(file.FileName);

            if (!string.Equals(
                    extension,
                    ".xlsx",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Only .xlsx Excel files are allowed."
                });
            }

            var tempDirectory = Path.Combine(
                Path.GetTempPath(),
                "JobPortal",
                "TradeHierarchyImport");

            Directory.CreateDirectory(tempDirectory);

            var tempFileName =
                $"{Guid.NewGuid()}{extension}";

            var filePath =
                Path.Combine(tempDirectory, tempFileName);

            try
            {
                // Save uploaded Excel temporarily
                await using (var stream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await file.CopyToAsync(stream);
                }

                // Import Excel data
                var result =
                    await _importService.ImportAsync(filePath);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "An error occurred while importing the Excel file.",
                        error = ex.Message
                    });
            }
            finally
            {
                // Always remove temporary Excel file
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch
                    {
                        // Ignore cleanup failure
                    }
                }
            }
        }
    }
}