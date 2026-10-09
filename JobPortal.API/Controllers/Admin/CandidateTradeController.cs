
using JobPortal.Infrastructure.Persistence;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    namespace JobPortal.API.Controllers
    {
        [ApiController]
        [Route("api/candidate-trades")]
        public class CandidateTradeController : ControllerBase
        {
            private readonly AppDbContext _context;

            public CandidateTradeController(AppDbContext context)
            {
                _context = context;
            }

            // GET: api/candidate-trades
            [HttpGet]
            public async Task<IActionResult> GetTrades()
            {
                var trades = await _context.HomepageTradeCategories
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.DisplayOrder)
                    .Select(t => new
                    {
                        tradeCategoryId = t.TradeCategoryId,
                        name = t.Name,
                        subTrades = _context.HomepageSubTrades
                            .Where(s =>
                                s.TradeCategoryId == t.TradeCategoryId &&
                                s.IsActive)
                            .OrderBy(s => s.DisplayOrder)
                            .Select(s => new
                            {
                                subTradeId = s.SubTradeId,
                                name = s.Name
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(trades);
            }
        }
    }
