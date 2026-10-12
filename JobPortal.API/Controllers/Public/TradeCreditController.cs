using JobPortal.Application.DTOs.Recruiter.TradeCredit;
using JobPortal.Services.IImplement.IPublic;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.API.Controllers.Public
{

    [ApiController]
    [Route("api/trade-credit")]
    public class TradeCreditController : ControllerBase
    {
        private readonly ITradeCreditImportService _service;

        public TradeCreditController(ITradeCreditImportService service)
        {
            _service = service;
        }



        [HttpPost("import-excel")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportExcel(
        IFormFile file,
        CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please select an Excel file."
                });
            }

            if (!Path.GetExtension(file.FileName)
                .Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Only .xlsx files are supported."
                });
            }

            try
            {
                await using var stream = file.OpenReadStream();

                var count = await _service.ImportExcelAsync(
                    stream,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Excel trade credit mappings imported successfully.",
                    count
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to import trade credit mappings.",
                    error = ex.Message
                });
            }
        }


        [HttpGet("mappings")]
        public async Task<IActionResult> GetMappings(
            CancellationToken cancellationToken)
        {
            var mappings = await _service.GetMappingsAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                count = mappings.Count,
                data = mappings
            });
        }


        [HttpGet("tiers")]
        public async Task<IActionResult> GetTiers(CancellationToken cancellationToken)
        {
            var tiers = await _service.GetTiersAsync(cancellationToken);

            return Ok(new
            {
                success = true,
                count = tiers.Count,
                data = tiers.Select(t => new
                {
                    tierId = t.TierId,
                    tierName = t.TierName,
                    displayOrder = t.DisplayOrder,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt,
                    updatedAt = t.UpdatedAt
                })
            });
        }
    }

}
