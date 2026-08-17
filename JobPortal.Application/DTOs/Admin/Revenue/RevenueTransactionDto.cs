using System;
using System.Collections.Generic;

namespace JobPortal.Application.DTOs.Admin.Revenue
{
    // One row in the "Plan transactions" table.
    public class RevenueTransactionDto
    {
        public Guid TransactionId { get; set; }

        public DateTime Date { get; set; }

        public string Customer { get; set; } = default!;

        public string Plan { get; set; } = default!;

        // "candidate" | "recruiter" | "credits" — drives the pill
        // color and the tab filter on the frontend.
        public string Type { get; set; } = default!;

        public string Country { get; set; } = default!;

        public string CountryCode { get; set; } = default!;

        public decimal Amount { get; set; }

        public string? PaymentMethod { get; set; }

        public string PaymentStatus { get; set; } = default!;

        public string? InvoiceNumber { get; set; }

        public DateOnly? InvoiceDate { get; set; }

        public string? InvoiceUrl { get; set; }
    }

    public class RevenueTransactionsResponseDto
    {
        public List<RevenueTransactionDto> Items { get; set; } = new();

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }
}