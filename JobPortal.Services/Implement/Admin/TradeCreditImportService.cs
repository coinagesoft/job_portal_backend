using ClosedXML.Excel;
using JobPortal.Application.DTOs.Recruiter.TradeCredit;
using JobPortal.Application.DTOs.TradeCredit;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Entities.Homepage;
using JobPortal.Infrastructure.Persistence;
using JobPortal.Services.IImplement.IAdmin;
using JobPortal.Services.IImplement.IPublic;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JobPortal.Services.Implement.Admin
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

            using var workbook = new XLWorkbook(excelStream);

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

            // 2. Every row for the same main trade must have the same tier.
            // Names are normalized (trimmed, repeated spaces collapsed, case ignored)
            // so "Chargehand" and "chargehand " are treated as one trade.
            var tradeSettings = new Dictionary<
                string,
                (string TierName, int UnlockCredits)>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                var tradeName = NormalizeName(row.TradeName);
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
                    tradeSettings.Add(tradeName, (tierName, row.UnlockCredits));
                }
            }

            // 3. Credits belong to the tier, so each tier must have exactly one credit value.
            var tierCredits = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in tradeSettings.Values)
            {
                if (tierCredits.TryGetValue(s.TierName, out var credits) &&
                    credits != s.UnlockCredits)
                {
                    throw new ArgumentException(
                        $"Tier '{s.TierName}' has different credit values in the Excel file. " +
                        "Each tier must have one credit value.");
                }

                tierCredits[s.TierName] = s.UnlockCredits;
            }

            var now = DateTime.UtcNow;

            await using var transaction =
                await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // 4. Remove existing mappings.
                var existingMappings = await _context.TradeCreditMappings
                    .ToListAsync(cancellationToken);

                _context.TradeCreditMappings.RemoveRange(existingMappings);

                await _context.SaveChangesAsync(cancellationToken);

                // 5. Remove existing tiers.
                var existingTiers = await _context.TradeTiers
                    .ToListAsync(cancellationToken);

                _context.TradeTiers.RemoveRange(existingTiers);

                await _context.SaveChangesAsync(cancellationToken);

                // 6. Load existing main trades.
                // Subtrade records are not modified or created by this import.
                var categories = await _context.HomepageTradeCategories
                    .ToListAsync(cancellationToken);

                // 7. Recreate tiers (credits live on the tier).
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
                        UnlockCredits = tierCredits[tierName],
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    tiersByName.Add(tierName, tier);

                    _context.TradeTiers.Add(tier);
                }

                await _context.SaveChangesAsync(cancellationToken);

                // 8. Create a mapping for EVERY main trade that has this name.
                // The same trade can exist more than once (for example under different
                // industries). Each copy gets the same tier.
                // Always store SubTradeId = null.
                var mappingsCreated = 0;

                foreach (var tradeSetting in tradeSettings)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var tradeName = tradeSetting.Key;
                    var tier = tiersByName[tradeSetting.Value.TierName];

                    var matchingCategories = categories
                        .Where(x => string.Equals(
                            NormalizeName(x.Name),
                            tradeName,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    // Trade is in the Excel but not in the database yet: create it once.
                    if (matchingCategories.Count == 0)
                    {
                        var newCategory = new HomepageTradeCategory
                        {
                            TradeCategoryId = Guid.NewGuid(),
                            Name = tradeName,
                            DisplayOrder = categories.Count + 1,
                            IsActive = true,
                            CreatedAt = now,
                            UpdatedAt = now
                        };

                        _context.HomepageTradeCategories.Add(newCategory);
                        categories.Add(newCategory);
                        matchingCategories.Add(newCategory);
                    }

                    foreach (var category in matchingCategories)
                    {
                        _context.TradeCreditMappings.Add(new TradeCreditMapping
                        {
                            MappingId = Guid.NewGuid(),
                            TradeCategoryId = category.TradeCategoryId,

                            // Ignore Excel subtrade values completely.
                            SubTradeId = null,

                            TierId = tier.TierId,
                            IsActive = true,
                            CreatedAt = now,
                            UpdatedAt = now
                        });

                        mappingsCreated++;
                    }
                }

                // 9. Save mappings.
                await _context.SaveChangesAsync(cancellationToken);

                // 10. Commit the transaction.
                await transaction.CommitAsync(cancellationToken);

                // Number of main-trade mappings created (one per trade category).
                return mappingsCreated;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static string NormalizeName(string? value) =>
            Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");

        private static int GetTierOrder(string tierName)
        {
            var digits = new string(
                tierName.Where(char.IsDigit).ToArray());

            return int.TryParse(digits, out var order)
                ? order
                : 0;
        }

        // Saves the tier for the selected trade AND every other trade with the same name.
        public async Task<TradeCreditMappingResponseDto> SaveMainTradeMappingAsync(
            Guid tradeCategoryId,
            Guid tierId,
            CancellationToken cancellationToken = default)
        {
            if (tradeCategoryId == Guid.Empty)
                throw new ArgumentException("Please select a valid trade.");

            if (tierId == Guid.Empty)
                throw new ArgumentException("Please select a valid tier.");

            var tier = await _context.TradeTiers
                .FirstOrDefaultAsync(
                    x => x.TierId == tierId && x.IsActive,
                    cancellationToken)
                ?? throw new ArgumentException(
                    "Selected tier was not found or is inactive.");

            var all = await _context.HomepageTradeCategories
                .ToListAsync(cancellationToken);

            var category = all.FirstOrDefault(x => x.TradeCategoryId == tradeCategoryId)
                ?? throw new ArgumentException("Selected trade was not found.");

            var key = NormalizeName(category.Name);

            var sameTrade = all
                .Where(x => string.Equals(
                    NormalizeName(x.Name),
                    key,
                    StringComparison.OrdinalIgnoreCase))
                .ToList();

            var ids = sameTrade.Select(x => x.TradeCategoryId).ToList();

            var existing = await _context.TradeCreditMappings
                .Where(x => ids.Contains(x.TradeCategoryId) && x.SubTradeId == null)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            TradeCreditMapping? requested = null;

            foreach (var c in sameTrade)
            {
                var m = existing.FirstOrDefault(x => x.TradeCategoryId == c.TradeCategoryId);

                if (m == null)
                {
                    m = new TradeCreditMapping
                    {
                        MappingId = Guid.NewGuid(),
                        TradeCategoryId = c.TradeCategoryId,
                        SubTradeId = null,
                        CreatedAt = now
                    };

                    _context.TradeCreditMappings.Add(m);
                }

                m.TierId = tierId;
                m.IsActive = true;
                m.UpdatedAt = now;

                if (c.TradeCategoryId == tradeCategoryId)
                    requested = m;
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new TradeCreditMappingResponseDto
            {
                MappingId = requested!.MappingId,
                TradeCategoryId = category.TradeCategoryId,
                TradeName = category.Name,
                SubTradeId = null,
                SubTradeName = null,
                TierId = tier.TierId,
                TierName = tier.TierName,
                UnlockCredits = tier.UnlockCredits,
                IsActive = true
            };
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

                    // Credits come from the tier.
                    UnlockCredits = x.Tier.UnlockCredits,

                    IsActive = x.IsActive
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<List<TradeTier>> GetTiersAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.TradeTiers
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.TierName)
                .ToListAsync(cancellationToken);
        }

        public async Task<TradeTier> CreateTierAsync(
            string tierName,
            int displayOrder,
            int unlockCredits,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tierName))
                throw new ArgumentException("Tier name is required.");

            if (displayOrder < 0)
                throw new ArgumentException(
                    "Display order cannot be negative.");

            if (unlockCredits <= 0)
                throw new ArgumentException(
                    "Unlock credits must be greater than zero.");

            tierName = tierName.Trim();

            var exists = await _context.TradeTiers
                .AnyAsync(
                    x => x.TierName.ToLower() == tierName.ToLower(),
                    cancellationToken);

            if (exists)
                throw new ArgumentException(
                    $"Tier '{tierName}' already exists.");

            var now = DateTime.UtcNow;

            var tier = new TradeTier
            {
                TierId = Guid.NewGuid(),
                TierName = tierName,
                DisplayOrder = displayOrder,
                UnlockCredits = unlockCredits,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _context.TradeTiers.Add(tier);

            await _context.SaveChangesAsync(cancellationToken);

            return tier;
        }

        public async Task<TradeTier> UpdateTierAsync(
            Guid tierId,
            string tierName,
            int displayOrder,
            int unlockCredits,
            CancellationToken cancellationToken = default)
        {
            if (tierId == Guid.Empty)
                throw new ArgumentException("Invalid tier ID.");

            if (string.IsNullOrWhiteSpace(tierName))
                throw new ArgumentException("Tier name is required.");

            if (displayOrder < 0)
                throw new ArgumentException(
                    "Display order cannot be negative.");

            if (unlockCredits <= 0)
                throw new ArgumentException(
                    "Unlock credits must be greater than zero.");

            tierName = tierName.Trim();

            var tier = await _context.TradeTiers
                .FirstOrDefaultAsync(
                    x => x.TierId == tierId,
                    cancellationToken);

            if (tier == null)
                throw new ArgumentException("Tier was not found.");

            var duplicateName = await _context.TradeTiers
                .AnyAsync(
                    x => x.TierId != tierId &&
                         x.TierName.ToLower() == tierName.ToLower(),
                    cancellationToken);

            if (duplicateName)
                throw new ArgumentException(
                    $"Tier '{tierName}' already exists.");

            tier.TierName = tierName;
            tier.DisplayOrder = displayOrder;
            tier.UnlockCredits = unlockCredits;
            tier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return tier;
        }

        public async Task<TradeTier> ToggleTierAsync(
            Guid tierId,
            CancellationToken cancellationToken = default)
        {
            if (tierId == Guid.Empty)
                throw new ArgumentException("Invalid tier ID.");

            var tier = await _context.TradeTiers
                .FirstOrDefaultAsync(
                    x => x.TierId == tierId,
                    cancellationToken);

            if (tier == null)
                throw new ArgumentException("Tier was not found.");

            tier.IsActive = !tier.IsActive;
            tier.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return tier;
        }
    }
}