using JobPortal.Application.DTOs;
using JobPortal.Application.DTOs.Admin;
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
                    unlockCredits = t.UnlockCredits,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt,
                    updatedAt = t.UpdatedAt
                })
            });
        }

        [HttpPut("mappings/{tradeCategoryId:guid}")]
        public async Task<IActionResult> SaveMainTradeMapping(
    Guid tradeCategoryId,
    [FromBody] SaveMainTradeMappingRequestDto request,
    CancellationToken cancellationToken)
        {
            if (request.TierId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please select a tier."
                });
            }

         

            try
            {
                var mapping = await _service.SaveMainTradeMappingAsync(
                    tradeCategoryId,
                    request.TierId,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Trade credit assignment saved successfully.",
                    data = mapping
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
        }


        [HttpPost("tiers")]
        public async Task<IActionResult> CreateTier(
    [FromBody] CreateTradeTierRequestDto request,
    CancellationToken cancellationToken)
        {
            try
            {
                var tier = await _service.CreateTierAsync(
                    request.TierName,
                    request.DisplayOrder,
                    request.UnlockCredits,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Tier created successfully.",
                    data = tier
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
        }

        [HttpPut("tiers/{tierId:guid}")]
        public async Task<IActionResult> UpdateTier(
        Guid tierId,
        [FromBody] CreateTradeTierRequestDto request,
        CancellationToken cancellationToken)
        {
            try
            {
                var tier = await _service.UpdateTierAsync(
                    tierId,
                    request.TierName,
                    request.DisplayOrder,
                    request.UnlockCredits,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = "Tier updated successfully.",
                    data = tier
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
        }

        [HttpPatch("tiers/{tierId:guid}/toggle")]
        public async Task<IActionResult> ToggleTier(
    Guid tierId,
    CancellationToken cancellationToken)
        {
            try
            {
                var tier = await _service.ToggleTierAsync(
                    tierId,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = tier.IsActive
                        ? "Tier activated successfully."
                        : "Tier deactivated successfully.",
                    data = tier
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
        }

    }

}
