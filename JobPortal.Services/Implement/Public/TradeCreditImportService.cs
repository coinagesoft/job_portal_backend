using JobPortal.Application.DTOs.Recruiter.TradeCredit;
using JobPortal.Application.DTOs.TradeCredit;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Entities.Homepage;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IPublic;
using ClosedXML.Excel;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Public
{


    public class TradeCreditImportService : ITradeCreditImportService
    {
        private readonly AppDbContext _context;

        public TradeCreditImportService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<int> ImportExcelAsync(
 Stream excelStream,
 CancellationToken cancellationToken = default)
        {
            if (excelStream == null || !excelStream.CanRead)
            {
                throw new ArgumentException(
                    "A valid Excel file stream is required.");
            }

            var rows = new List<TradeCreditImportRowDto>();

            using var workbook = new ClosedXML.Excel.XLWorkbook(excelStream);

            var worksheet = workbook.Worksheets.FirstOrDefault(
                ws => string.Equals(
                    ws.Name,
                    "ALL TRADES",
                    StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                throw new ArgumentException(
                    "No worksheet was found in the Excel file.");
            }

            var headerRow = worksheet.FirstRowUsed();

            if (headerRow == null)
            {
                throw new ArgumentException(
                    "The Excel worksheet is empty.");
            }

            var headers = headerRow.CellsUsed()
                .Where(c => !string.IsNullOrWhiteSpace(c.GetString()))
                .ToDictionary(
                    c => c.GetString().Trim(),
                    c => c.Address.ColumnNumber,
                    StringComparer.OrdinalIgnoreCase);

            var requiredHeaders = new[]
            {
        "TRADE",
        "CATEGORIES / SUB-TRADES",
        "TIER",
        "CREDITS"
    };

            var missingHeaders = requiredHeaders
                .Where(h => !headers.ContainsKey(h))
                .ToList();

            if (missingHeaders.Count > 0)
            {
                throw new ArgumentException(
                    $"Missing Excel columns: {string.Join(", ", missingHeaders)}");
            }

            foreach (var excelRow in worksheet.RowsUsed()
                         .Where(r => r.RowNumber() > headerRow.RowNumber()))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var tradeName = excelRow
                    .Cell(headers["TRADE"]).GetString().Trim();

                var subTradeName = excelRow
                    .Cell(headers["CATEGORIES / SUB-TRADES"])
                    .GetString().Trim();

                var tierName = excelRow
                    .Cell(headers["TIER"]).GetString().Trim();

                var creditsCell = excelRow.Cell(headers["CREDITS"]);

                if (string.IsNullOrWhiteSpace(tradeName) &&
                    string.IsNullOrWhiteSpace(subTradeName) &&
                    string.IsNullOrWhiteSpace(tierName) &&
                    creditsCell.IsEmpty())
                {
                    continue;
                }

                if (!creditsCell.TryGetValue<int>(out var credits))
                {
                    throw new ArgumentException(
                        $"Invalid credits at Excel row {excelRow.RowNumber()}.");
                }

                rows.Add(new TradeCreditImportRowDto
                {
                    TradeName = tradeName,
                    SubTradeName = string.IsNullOrWhiteSpace(subTradeName)
                        ? null
                        : subTradeName,
                    TierName = tierName,
                    UnlockCredits = credits
                });
            }

            return await ImportAsync(rows, cancellationToken);
        }

        public async Task<int> ImportAsync(
          List<TradeCreditImportRowDto> rows,
          CancellationToken cancellationToken = default)
        {
            if (rows == null || rows.Count == 0)
                throw new ArgumentException("Import rows cannot be empty.");

            // 1. Validate all rows before deleting existing data.
            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.TradeName))
                    throw new ArgumentException("Trade name is required.");

                if (string.IsNullOrWhiteSpace(row.TierName))
                {
                    throw new ArgumentException(
                        $"Tier name is required for trade '{row.TradeName}'.");
                }

                if (row.UnlockCredits <= 0)
                {
                    throw new ArgumentException(
                        $"Unlock credits must be greater than zero for trade '{row.TradeName}'.");
                }
            }

            // 2. Validate that every row for the same main trade
            // has the same tier and credit value.
            var tradeSettings = new Dictionary<
                string,
                (string TierName, int UnlockCredits)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var tradeName = row.TradeName.Trim();
                var tierName = row.TierName.Trim();

                if (tradeSettings.TryGetValue(tradeName, out var existing))
                {
                    if (!string.Equals(
                            existing.TierName,
                            tierName,
                            StringComparison.OrdinalIgnoreCase) ||
                        existing.UnlockCredits != row.UnlockCredits)
                    {
                        throw new ArgumentException(
                            $"Conflicting tier or credit values found for trade '{tradeName}'. " +
                            "All rows for the same main trade must have identical tier and credits.");
                    }
                }
                else
                {
                    tradeSettings.Add(
                        tradeName,
                        (tierName, row.UnlockCredits));
                }
            }

            var now = DateTime.UtcNow;

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 3. Remove existing credit mappings.
                var existingMappings = await _context.TradeCreditMappings
                    .ToListAsync(cancellationToken);

                _context.TradeCreditMappings.RemoveRange(existingMappings);

                await _context.SaveChangesAsync(cancellationToken);

                // 4. Remove existing tiers.
                var existingTiers = await _context.TradeTiers
                    .ToListAsync(cancellationToken);

                _context.TradeTiers.RemoveRange(existingTiers);

                await _context.SaveChangesAsync(cancellationToken);

                // 5. Load existing main trades.
                // Subtrade records are not modified or created by this import.
                var categories = await _context.HomepageTradeCategories
                    .ToListAsync(cancellationToken);

                // 6. Recreate tiers.
                var tiersByName = new Dictionary<string, TradeTier>(
                    StringComparer.OrdinalIgnoreCase);

                foreach (var settings in tradeSettings.Values)
                {
                    var tierName = settings.TierName.Trim();

                    if (tiersByName.ContainsKey(tierName))
                        continue;

                    var tier = new TradeTier
                    {
                        TierId = Guid.NewGuid(),
                        TierName = tierName,
                        DisplayOrder = GetTierOrder(tierName),
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    tiersByName.Add(tierName, tier);

                    _context.TradeTiers.Add(tier);
                }

                await _context.SaveChangesAsync(cancellationToken);

                // 7. Create or reuse main trades.
                // Always store SubTradeId = null.
                var mappingsByTrade = new Dictionary<Guid, TradeCreditMapping>();

                foreach (var tradeSetting in tradeSettings)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var tradeName = tradeSetting.Key;
                    var tierName = tradeSetting.Value.TierName;
                    var unlockCredits = tradeSetting.Value.UnlockCredits;

                    var category = categories.FirstOrDefault(x =>
                        string.Equals(
                            x.Name.Trim(),
                            tradeName,
                            StringComparison.OrdinalIgnoreCase));

                    if (category == null)
                    {
                        category = new HomepageTradeCategory
                        {
                            TradeCategoryId = Guid.NewGuid(),
                            Name = tradeName,
                            DisplayOrder = categories.Count + 1,
                            IsActive = true,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        _context.HomepageTradeCategories.Add(category);
                        categories.Add(category);
                    }

                    var tier = tiersByName[tierName];

                    var mapping = new TradeCreditMapping
                    {
                        MappingId = Guid.NewGuid(),
                        TradeCategoryId = category.TradeCategoryId,

                        // Ignore Excel subtrade values completely.
                        SubTradeId = null,

                        TierId = tier.TierId,
                        UnlockCredits = unlockCredits,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    mappingsByTrade.Add(category.TradeCategoryId, mapping);

                    _context.TradeCreditMappings.Add(mapping);
                }

                // 8. Save mappings.
                await _context.SaveChangesAsync(cancellationToken);

                // 9. Commit the transaction.
                await transaction.CommitAsync(cancellationToken);

                // Return the number of unique main-trade mappings created.
                return mappingsByTrade.Count;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        private static int GetTierOrder(string tierName)
        {
            var digits = new string(
                tierName.Where(char.IsDigit).ToArray());

            return int.TryParse(digits, out var order)
                ? order
                : 0;
        }



        public async Task<List<TradeCreditMappingResponseDto>> GetMappingsAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.TradeCreditMappings
                .AsNoTracking()
                .Include(x => x.TradeCategory)
                .Include(x => x.SubTrade)
                .Include(x => x.Tier)
                .OrderBy(x => x.TradeCategory.Name)
                .ThenBy(x => x.SubTrade == null ? "" : x.SubTrade.Name)
                .Select(x => new TradeCreditMappingResponseDto
                {
                    MappingId = x.MappingId,

                    TradeCategoryId = x.TradeCategoryId,
                    TradeName = x.TradeCategory.Name,

                    SubTradeId = x.SubTradeId,
                    SubTradeName = x.SubTrade != null
                        ? x.SubTrade.Name
                        : null,

                    TierId = x.TierId,
                    TierName = x.Tier.TierName,

                    UnlockCredits = x.UnlockCredits,
                    IsActive = x.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TradeTier>> GetTiersAsync( CancellationToken cancellationToken = default)
        {
            return await _context.TradeTiers
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.TierName)
                .ToListAsync(cancellationToken);
        }
    }

}
