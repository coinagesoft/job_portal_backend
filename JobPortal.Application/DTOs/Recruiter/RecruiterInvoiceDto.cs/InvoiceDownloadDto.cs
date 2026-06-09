using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.RecruiterInvoiceDto.cs
{
    public class InvoiceDownloadDto
    {
        public Guid InvoiceId { get; set; }

        public string InvoiceNumber { get; set; } = string.Empty;

        public string? InvoiceUrl { get; set; }
    }
}
