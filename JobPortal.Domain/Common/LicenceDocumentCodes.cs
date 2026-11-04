using System;
using System.Linq;

namespace JobPortal.Domain.Common
{
    /// <summary>
    /// Canonical codes for the licence-type verification documents, plus a
    /// tolerant resolver.
    ///
    /// Why this exists: documents created by hand in Admin ▸ Document Types
    /// used to get a random 8-character Code, so the candidate-facing
    /// company page (which matches on Code) never recognised them. The
    /// resolver accepts either the canonical Code OR a matching
    /// DocumentName, so the lookup keeps working even if a Code is wrong.
    /// </summary>
    public static class LicenceDocumentCodes
    {
        public const string Poe = "POE_LICENSE";
        public const string Rpsl = "RPSL_LICENSE";
        public const string Registration = "REGISTRATION_CERTIFICATE";

        public static readonly string[] All = { Poe, Rpsl, Registration };

        private static string Normalize(string? value) =>
            new string((value ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '_')
                .ToArray())
            .Trim('_');

        /// <summary>
        /// Canonical code for a document NAME, or null when the name is not
        /// one of the three licence documents.
        /// </summary>
        public static string? CodeForName(string? name)
        {
            var n = Normalize(name);
            if (n.Length == 0) return null;

            // "POE License" / "POE Licence" only. A document such as
            // "Proof of Establishment" is intentionally NOT matched.
            if (n == "POE_LICENSE" || n == "POE_LICENCE") return Poe;
            if (n == "RPSL_LICENSE" || n == "RPSL_LICENCE") return Rpsl;

            if (n == "REGISTRATION_CERTIFICATE" ||
                n == "COMPANY_REGISTRATION_CERTIFICATE")
                return Registration;

            return null;
        }

        /// <summary>
        /// Resolves a master document to a canonical licence code: the Code
        /// when it is already canonical, otherwise the DocumentName.
        /// Returns null for any other document type.
        /// </summary>
        public static string? Resolve(string? code, string? name)
        {
            var c = Normalize(code);
            if (All.Contains(c)) return c;

            return CodeForName(name);
        }

        /// <summary>
        /// GST and Registration Certificate are permanent: they never lapse,
        /// so any ExpiryDate read from the file (e.g. by Gemini) is ignored.
        /// </summary>
        public static bool NeverExpires(string? canonicalCode) =>
            canonicalCode == Registration;
    }
}