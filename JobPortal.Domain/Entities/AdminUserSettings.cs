using System;

namespace JobPortal.Domain.Entities;

/// <summary>
/// One row per admin — backs the "/admin/settings" screen (Default Language +
/// Session Timeout). Created on first save (or lazily on first read) rather
/// than seeded, so every AdminUser doesn't need a matching row up front.
/// </summary>
public class AdminUserSettings
{
    public Guid SettingsId { get; set; }

    // FK -> AdminUser, one row per admin.
    public Guid AdminId { get; set; }

    // e.g. "English (US)", "Hindi", "French"...
    public string Language { get; set; } = "English (US)";

    // e.g. "15 Minutes", "30 Minutes", "1 Hour", "2 Hours", "Never"
    public string SessionTimeout { get; set; } = "30 Minutes";

    public DateTime UpdatedAt { get; set; }

    // Navigation
    public AdminUser AdminUser { get; set; } = default!;
}