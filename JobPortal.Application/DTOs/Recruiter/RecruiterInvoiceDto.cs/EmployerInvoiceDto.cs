using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.RecruiterInvoiceDto.cs
{
    public class EmployerInvoiceDto
    {
        public Guid InvoiceId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateOnly InvoiceDate { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public int Amount { get; set; }

        public int Gst { get; set; }

        public int Total { get; set; }

        public string? InvoiceUrl { get; set; }
    }
}
