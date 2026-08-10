using System.Collections.Generic;
using System.Linq;

namespace JobPortal.Domain.Constants
{
    // Single source of truth for the sub-admin "Sidebar Tab Access" keys.
    // Mirrors the TABS array in job_portal_admin's
    // src/app/admin/users/page.js 1:1 (same keys, same order). If a tab is
    // added/removed/renamed on the sidebar, update it here too so the
    // Add/Edit Sub Admin drawer's permission list and the backend's
    // validation never drift apart.
    public static class AdminSidebarTabs
    {
        public record TabDefinition(string Key, string Label);

        public static readonly IReadOnlyList<TabDefinition> All = new List<TabDefinition>
        {
            new("dashboard", "Dashboard"),
            new("candidates", "Candidates"),
            new("recruiters", "Recruiters"),
            new("revenue", "Revenue"),
            new("plans", "Plans"),
            new("home_management", "Home Management"),
            new("users", "Users"),
            new("help_support", "Help & Support"),
            new("audit_logs", "Audit Logs"),
            new("legal_pages", "Legal Pages"),
            new("settings", "Settings"),
        };

        public static readonly IReadOnlySet<string> ValidKeys =
            All.Select(t => t.Key).ToHashSet();

        // Legacy, pre-tab-based granular permission keys (e.g.
        // "candidates.view", "subadmin.create") map onto the tab that now
        // covers them. Lets old rows created before this change keep
        // working instead of failing validation — same mapping as the
        // frontend's LEGACY_TO_TAB table.
        public static readonly IReadOnlyDictionary<string, string> LegacyKeyToTab =
            new Dictionary<string, string>
            {
                ["candidates"] = "candidates",
                ["candidates.view"] = "candidates",
                ["candidates.approve"] = "candidates",
                ["candidates.suspend"] = "candidates",
                ["employers"] = "recruiters",
                ["recruiters.view"] = "recruiters",
                ["employers.approve"] = "recruiters",
                ["employers.badges"] = "recruiters",
                ["revenue.view"] = "revenue",
                ["finance.view"] = "revenue",
                ["finance.invoice"] = "revenue",
                ["finance.refund"] = "revenue",
                ["finance.deposit"] = "revenue",
                ["plans.view"] = "plans",
                ["plans.edit"] = "plans",
                ["verify.kyc"] = "candidates",
                ["verify.passport"] = "candidates",
                ["verify.iti"] = "candidates",
                ["verify.gst"] = "candidates",
                ["verify.poe"] = "candidates",
                ["verify.rpsl"] = "candidates",
                ["verify.ai"] = "candidates",
                ["support.view"] = "help_support",
                ["support.reply"] = "help_support",
                ["legal.view"] = "legal_pages",
                ["legal.edit"] = "legal_pages",
                ["settings.view"] = "settings",
                ["settings.edit"] = "settings",
                ["settings.credits"] = "settings",
                ["audit.view"] = "audit_logs",
                ["audit.export"] = "audit_logs",
                ["subadmin.view"] = "users",
                ["subadmin.create"] = "users",
                ["subadmin.edit"] = "users",
                ["subadmin.delete"] = "users",
            };

        // Maps a raw permissions list (which may contain legacy keys, dupes,
        // or unknown junk) onto the canonical tab-key set. Anything that
        // doesn't resolve to a known tab is dropped.
        public static List<string> Normalize(IEnumerable<string>? rawKeys)
        {
            if (rawKeys == null)
                return new List<string>();

            return rawKeys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim())
                .Select(k => LegacyKeyToTab.TryGetValue(k, out var tab) ? tab : k)
                .Where(k => ValidKeys.Contains(k))
                .Distinct()
                .ToList();
        }
    }
}