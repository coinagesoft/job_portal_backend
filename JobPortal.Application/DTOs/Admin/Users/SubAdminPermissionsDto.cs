using System.Collections.Generic;
using System.Text.Json.Serialization;
using JobPortal.Domain.Constants;

namespace JobPortal.Application.DTOs.Admin.Users
{
    // Sidebar tab access as individual true/false toggles instead of a
    // free-form string array — this is what renders as a set of boolean
    // fields in Swagger, one per tab, matching the "Sidebar Tab Access"
    // toggle list on the Add/Edit Sub Admin drawer 1:1.
    //
    // The [JsonPropertyName] on each property must stay in sync with the
    // `key` used in TABS in job_portal_admin's
    // src/app/admin/users/page.js — see JobPortal.Domain.Constants
    // .AdminSidebarTabs, which is the source of truth for these keys.
    public class SubAdminPermissionsDto
    {
        [JsonPropertyName("dashboard")]
        public bool Dashboard { get; set; }

        [JsonPropertyName("candidates")]
        public bool Candidates { get; set; }

        [JsonPropertyName("recruiters")]
        public bool Recruiters { get; set; }

        [JsonPropertyName("revenue")]
        public bool Revenue { get; set; }

        [JsonPropertyName("plans")]
        public bool Plans { get; set; }

        [JsonPropertyName("home_management")]
        public bool HomeManagement { get; set; }

        [JsonPropertyName("users")]
        public bool Users { get; set; }

        [JsonPropertyName("help_support")]
        public bool HelpSupport { get; set; }

        [JsonPropertyName("audit_logs")]
        public bool AuditLogs { get; set; }

        [JsonPropertyName("legal_pages")]
        public bool LegalPages { get; set; }

        [JsonPropertyName("settings")]
        public bool Settings { get; set; }

        // Flattens the toggles down to the tab-key list format the service
        // layer (and DB storage on AdminRole.Permissions) already uses.
        // Runs through AdminSidebarTabs.Normalize so the result is always
        // a clean, deduped, known-key list.
        public List<string> ToKeyList()
        {
            var keys = new List<string>();

            if (Dashboard) keys.Add("dashboard");
            if (Candidates) keys.Add("candidates");
            if (Recruiters) keys.Add("recruiters");
            if (Revenue) keys.Add("revenue");
            if (Plans) keys.Add("plans");
            if (HomeManagement) keys.Add("home_management");
            if (Users) keys.Add("users");
            if (HelpSupport) keys.Add("help_support");
            if (AuditLogs) keys.Add("audit_logs");
            if (LegalPages) keys.Add("legal_pages");
            if (Settings) keys.Add("settings");

            return AdminSidebarTabs.Normalize(keys);
        }

        // Reverse of ToKeyList — builds the toggle set from a stored
        // tab-key list (e.g. AdminRole.Permissions once deserialized).
        // Used to prefill the Edit drawer / echo permissions back on a
        // response DTO in the same true/false shape as the request.
        public static SubAdminPermissionsDto FromKeyList(IEnumerable<string>? keys)
        {
            var normalized = new HashSet<string>(AdminSidebarTabs.Normalize(keys));

            return new SubAdminPermissionsDto
            {
                Dashboard = normalized.Contains("dashboard"),
                Candidates = normalized.Contains("candidates"),
                Recruiters = normalized.Contains("recruiters"),
                Revenue = normalized.Contains("revenue"),
                Plans = normalized.Contains("plans"),
                HomeManagement = normalized.Contains("home_management"),
                Users = normalized.Contains("users"),
                HelpSupport = normalized.Contains("help_support"),
                AuditLogs = normalized.Contains("audit_logs"),
                LegalPages = normalized.Contains("legal_pages"),
                Settings = normalized.Contains("settings")
            };
        }
    }
}