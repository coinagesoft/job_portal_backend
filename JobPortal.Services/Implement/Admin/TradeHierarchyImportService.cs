using ClosedXML.Excel;
using JobPortal.Application.DTOs;
using JobPortal.Domain.Entities.Homepage;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Services.Implement.Admin
{
    public class TradeHierarchyImportService : ITradeHierarchyImportService
    {
        private readonly AppDbContext _context;

        public TradeHierarchyImportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TradeHierarchyImportResultDto> ImportAsync(string filePath)
        {
            int industriesAdded = 0;
            int tradesAdded = 0;
            int subTradesAdded = 0;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return new TradeHierarchyImportResultDto
                {
                    Success = false,
                    Message = "Excel file path is required."
                };
            }

            if (!File.Exists(filePath))
            {
                return new TradeHierarchyImportResultDto
                {
                    Success = false,
                    Message = "Excel file was not found."
                };
            }

            try
            {
                // ---------------------------------------------------------
                // Open Excel
                // ---------------------------------------------------------

                using var workbook = new XLWorkbook(filePath);

                // ---------------------------------------------------------
                // Check ALL TRADES sheet
                // ---------------------------------------------------------

                if (!workbook.Worksheets.Contains("ALL TRADES"))
                {
                    return new TradeHierarchyImportResultDto
                    {
                        Success = false,
                        Message =
                            "The Excel file must contain an 'ALL TRADES' sheet."
                    };
                }

                var worksheet = workbook.Worksheet("ALL TRADES");

                // Skip header row
                var rows = worksheet
                    .RowsUsed()
                    .Skip(1)
                    .ToList();

                if (!rows.Any())
                {
                    return new TradeHierarchyImportResultDto
                    {
                        Success = false,
                        Message =
                            "The 'ALL TRADES' sheet does not contain any data."
                    };
                }

                // ---------------------------------------------------------
                // Start transaction
                // ---------------------------------------------------------

                await using var transaction =
                    await _context.Database.BeginTransactionAsync();

                // ---------------------------------------------------------
                // Load existing data once
                // ---------------------------------------------------------

                var industries =
                    await _context.HomepageRegistrationIndustries
                        .ToListAsync();

                var trades =
                    await _context.HomepageTradeCategories
                        .ToListAsync();

                var subTrades =
                    await _context.HomepageSubTrades
                        .ToListAsync();

                // ---------------------------------------------------------
                // Industry Display Order
                // ---------------------------------------------------------

                int nextIndustryOrder =
                    industries.Any()
                        ? industries.Max(x => x.DisplayOrder) + 1
                        : 1;

                // ---------------------------------------------------------
                // Process Excel rows
                // ---------------------------------------------------------

                foreach (var row in rows)
                {
                    // -----------------------------------------------------
                    // Read Excel columns
                    //
                    // Column 1 = SECTOR
                    // Column 2 = TRADE
                    // Column 3 = CATEGORIES / SUB-TRADES
                    // -----------------------------------------------------

                    var industryName =
                        row.Cell(1)
                            .GetString()
                            .Trim();

                    var tradeName =
                        row.Cell(2)
                            .GetString()
                            .Trim();

                    var subTradeText =
                        row.Cell(3)
                            .GetString()
                            .Trim();

                    // -----------------------------------------------------
                    // Validate Industry
                    // -----------------------------------------------------

                    if (string.IsNullOrWhiteSpace(industryName))
                        continue;

                    // -----------------------------------------------------
                    // Validate Trade
                    // -----------------------------------------------------

                    if (string.IsNullOrWhiteSpace(tradeName))
                        continue;

                    // =====================================================
                    // INDUSTRY
                    // =====================================================

                    var industry =
                        industries.FirstOrDefault(x =>
                            string.Equals(
                                x.Name?.Trim(),
                                industryName,
                                StringComparison.OrdinalIgnoreCase));

                    // -----------------------------------------------------
                    // Create Industry if it doesn't exist
                    // -----------------------------------------------------

                    if (industry == null)
                    {
                        industry = new HomepageRegistrationIndustry
                        {
                            RegistrationIndustryId = Guid.NewGuid(),

                            Name = industryName,

                            DisplayOrder = nextIndustryOrder++,

                            IsActive = true,

                            CreatedAt = DateTime.UtcNow,

                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.HomepageRegistrationIndustries
                            .Add(industry);

                        // Add to in-memory list so repeated Excel rows
                        // don't create another Industry.
                        industries.Add(industry);

                        industriesAdded++;
                    }

                    // =====================================================
                    // TRADE
                    // =====================================================

                    var trade =
                        trades.FirstOrDefault(x =>
                            x.RegistrationIndustryId ==
                                industry.RegistrationIndustryId
                            &&
                            string.Equals(
                                x.Name?.Trim(),
                                tradeName,
                                StringComparison.OrdinalIgnoreCase));

                    // -----------------------------------------------------
                    // Create Trade if it doesn't exist
                    // -----------------------------------------------------

                    if (trade == null)
                    {
                        // DisplayOrder starts from 1 for every Industry.
                        int nextTradeOrder =
                            trades
                                .Where(x =>
                                    x.RegistrationIndustryId ==
                                    industry.RegistrationIndustryId)
                                .Select(x => (int?)x.DisplayOrder)
                                .Max() ?? 0;

                        trade = new HomepageTradeCategory
                        {
                            TradeCategoryId = Guid.NewGuid(),

                            RegistrationIndustryId =
                                industry.RegistrationIndustryId,

                            Name = tradeName,

                            DisplayOrder = nextTradeOrder + 1,

                            IsActive = true,

                            CreatedAt = DateTime.UtcNow,

                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.HomepageTradeCategories
                            .Add(trade);

                        // Add to in-memory list so repeated rows
                        // don't create another Trade.
                        trades.Add(trade);

                        tradesAdded++;
                    }

                    // =====================================================
                    // SUB-TRADES
                    // =====================================================

                    if (string.IsNullOrWhiteSpace(subTradeText))
                        continue;

                    // -----------------------------------------------------
                    // Split:
                    //
                    // "Piping, E&I, Mechanical, Welding"
                    //
                    // into:
                    //
                    // Piping
                    // E&I
                    // Mechanical
                    // Welding
                    // -----------------------------------------------------

                    var subTradeNames =
                        subTradeText
                            .Split(
                                ',',
                                StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x))
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase)
                            .ToList();

                    // -----------------------------------------------------
                    // Process each SubTrade
                    // -----------------------------------------------------

                    foreach (var subTradeName in subTradeNames)
                    {
                        // -------------------------------------------------
                        // Check duplicate SubTrade
                        //
                        // Duplicate rule:
                        //
                        // TradeCategoryId + SubTrade Name
                        // -------------------------------------------------

                        var existingSubTrade =
                            subTrades.FirstOrDefault(x =>
                                x.TradeCategoryId ==
                                    trade.TradeCategoryId
                                &&
                                string.Equals(
                                    x.Name?.Trim(),
                                    subTradeName,
                                    StringComparison.OrdinalIgnoreCase));

                        if (existingSubTrade != null)
                            continue;

                        // -------------------------------------------------
                        // Calculate DisplayOrder per Trade
                        // -------------------------------------------------

                        int nextSubTradeOrder =
                            subTrades
                                .Where(x =>
                                    x.TradeCategoryId ==
                                    trade.TradeCategoryId)
                                .Select(x => (int?)x.DisplayOrder)
                                .Max() ?? 0;

                        // -------------------------------------------------
                        // Create SubTrade
                        // -------------------------------------------------

                        var newSubTrade =
                            new HomepageSubTrade
                            {
                                SubTradeId = Guid.NewGuid(),

                                TradeCategoryId =
                                    trade.TradeCategoryId,

                                Name = subTradeName,

                                DisplayOrder =
                                    nextSubTradeOrder + 1,

                                IsActive = true,

                                CreatedAt = DateTime.UtcNow,

                                UpdatedAt = DateTime.UtcNow
                            };

                        _context.HomepageSubTrades
                            .Add(newSubTrade);

                        // Add to in-memory list so repeated Excel rows
                        // don't create duplicate SubTrades.
                        subTrades.Add(newSubTrade);

                        subTradesAdded++;
                    }
                }

                // =========================================================
                // Save everything
                // =========================================================

                await _context.SaveChangesAsync();

                // ---------------------------------------------------------
                // Commit transaction
                // ---------------------------------------------------------

                await transaction.CommitAsync();

                // =========================================================
                // Return success
                // =========================================================

                return new TradeHierarchyImportResultDto
                {
                    Success = true,

                    Message =
                        "Trade hierarchy imported successfully.",

                    IndustriesAdded =
                        industriesAdded,

                    TradesAdded =
                        tradesAdded,

                    SubTradesAdded =
                        subTradesAdded
                };
            }
            catch (Exception ex)
            {
                // ---------------------------------------------------------
                // Transaction will be rolled back because CommitAsync()
                // was not reached.
                // ---------------------------------------------------------

                return new TradeHierarchyImportResultDto
                {
                    Success = false,

                    Message =
                        $"Trade hierarchy import failed: {ex.Message}",

                    IndustriesAdded =
                        industriesAdded,

                    TradesAdded =
                        tradesAdded,

                    SubTradesAdded =
                        subTradesAdded
                };
            }
        }
    }
}