using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class Invoice
{
    public Guid InvoiceId { get; set; }
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public string InvoiceNumber { get; set; } = default!;
    public DateOnly InvoiceDate { get; set; }
    public int InvoiceAmount { get; set; }
    public int InvoiceGst { get; set; } = 0;
    public int InvoiceTotal { get; set; }
    public string? InvoiceS3Url { get; set; }
    public DateTime CreatedAt { get; set; }

    public PaymentTransaction PaymentTransaction { get; set; } = default!;
    public User User { get; set; } = default!;
}
