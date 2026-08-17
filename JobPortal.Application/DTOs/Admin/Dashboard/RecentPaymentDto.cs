using System;

namespace JobPortal.Application.DTOs.Admin.Dashboard
{
    // Powers the "Recent Payments" table on Admin ▸ Dashboard.
    // GET /api/admin/dashboard/recent-payments?limit=5
    public class RecentPaymentDto
    {
        public Guid TransactionId { get; set; }

        public string EntityName { get; set; } = default!;

        public decimal Amount { get; set; }

        // "Completed" | "Pending" | "Failed" | ... (raw PaymentStatus)
        public string PaymentStatus { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
    }
}