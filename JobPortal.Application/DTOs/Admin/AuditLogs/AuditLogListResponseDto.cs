using System;
using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin.AuditLogs
{
    public class AuditLogItemDto
    {
        public Guid LogId { get; set; }

        public DateTime Timestamp { get; set; }

        // Email shown in the "Admin" column (falls back to the admin
        // identifier / name if the user record can't be resolved).
        public string Admin { get; set; } = default!;

        // "Admin" | "Sub-Admin"
        public string ActorType { get; set; } = default!;

        public string Action { get; set; } = default!;

        public string Module { get; set; } = default!;

        public string? TargetEntity { get; set; }

        public string IpAddress { get; set; } = default!;

        // Info | Warning | Critical
        public string Severity { get; set; } = default!;

        public bool Success { get; set; }

        // Extra detail shown when the row is expanded (chevron).
        public string? Description { get; set; }

        public string? UserAgent { get; set; }

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public Guid? SessionId { get; set; }
    }

    public class AuditLogListResponseDto
    {
        public bool Success { get; set; } = true;

        public string? Message { get; set; }

        public List<AuditLogItemDto> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }
    }
}