using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JobPortal.Services.Implement.Recruiter
{
    /// <summary>
    /// Decides how many credits unlocking a candidate costs.
    ///
    /// A candidate's PrimaryTrade is often a free-text job title such as
    /// "Senior Welder &amp; Fabrication Lead", so an exact lookup against the
    /// admin-configured trade categories is not enough. Resolution order:
    ///   1. Exact match with a configured main trade (ignoring case,
    ///      punctuation and "&amp;" / "and").
    ///   2. The configured trade appears inside the candidate's title
    ///      ("Welder" inside "Senior Welder &amp; Fabrication Lead").
    ///      If several match, the most specific (most words, then longest
    ///      name, then alphabetical) wins.
    ///   3. Nothing matches: the trade is new, so charge
    ///      <see cref="DefaultUnlockCredits"/>.
    /// </summary>
    public static class TradeCreditResolver
    {
        /// <summary>Cost for a trade that has no configured band.</summary>
        public const int DefaultUnlockCredits = 6;

        public const string DefaultTierName = "Default (unmapped trade)";

        public sealed record TradeCreditRow(
            string TradeName,
            int UnlockCredits,
            string? TierName);

        public sealed record TradeCreditResult(
            int Credits,
            string TierName,
            string? MatchedTrade,
            bool IsDefault);

        private static readonly HashSet<string> StopWords =
            new(StringComparer.Ordinal) { "and", "the", "of", "for", "a", "an" };

        public static TradeCreditResult Resolve(
            IEnumerable<TradeCreditRow> rows,
            string? candidateTrade)
        {
            var titleTokens = Tokenize(candidateTrade);

            if (titleTokens.Count > 0)
            {
                var titleSet = new HashSet<string>(titleTokens);
                var titleJoined = " " + string.Join(" ", titleTokens) + " ";

                TradeCreditRow? best = null;
                var bestTokens = 0;

                foreach (var row in rows
                    .Where(r => r.UnlockCredits > 0)
                    .OrderBy(r => r.TradeName, StringComparer.OrdinalIgnoreCase))
                {
                    var tradeTokens = Tokenize(row.TradeName);
                    if (tradeTokens.Count == 0) continue;

                    var tradeJoined = " " + string.Join(" ", tradeTokens) + " ";

                    var exact = tradeTokens.Count == titleTokens.Count &&
                                titleJoined == tradeJoined;

                    var contained =
                        titleJoined.Contains(tradeJoined) ||
                        tradeTokens.All(titleSet.Contains);

                    if (!exact && !contained) continue;

                    if (exact)
                    {
                        // Exact beats everything.
                        return new TradeCreditResult(
                            row.UnlockCredits,
                            row.TierName ?? string.Empty,
                            row.TradeName,
                            false);
                    }

                    var better =
                        best == null ||
                        tradeTokens.Count > bestTokens ||
                        (tradeTokens.Count == bestTokens &&
                         row.TradeName.Length > best.TradeName.Length);

                    if (better)
                    {
                        best = row;
                        bestTokens = tradeTokens.Count;
                    }
                }

                if (best != null)
                {
                    return new TradeCreditResult(
                        best.UnlockCredits,
                        best.TierName ?? string.Empty,
                        best.TradeName,
                        false);
                }
            }

            return new TradeCreditResult(
                DefaultUnlockCredits,
                DefaultTierName,
                null,
                true);
        }

        // Lower-case words with "&" read as "and", punctuation removed,
        // stop words dropped and a trailing plural "s" trimmed.
        private static List<string> Tokenize(string? text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return result;

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text.Trim().ToLowerInvariant())
            {
                if (ch == '&') sb.Append(" and ");
                else if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                else sb.Append(' ');
            }

            foreach (var raw in sb.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (StopWords.Contains(raw)) continue;

                var word = raw.Length > 3 && raw.EndsWith("s") && !raw.EndsWith("ss")
                    ? raw[..^1]
                    : raw;

                result.Add(word);
            }

            return result;
        }
    }
}