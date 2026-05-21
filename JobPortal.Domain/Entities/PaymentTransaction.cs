using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class PaymentTransaction
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public Guid? EmployerId { get; set; }
    public Guid? CandidateId { get; set; }
    public string TransactionType { get; set; } = default!;
    public string? PackType { get; set; }
    public int? CreditQuantity { get; set; }
    public byte? ValidityMonths { get; set; }
    public int AmountPaise { get; set; }
    public int GstAmountPaise { get; set; } = 0;
    public int TotalAmountPaise { get; set; }
    public string? PaymentMethod { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string? GatewayRefundId { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public Guid? OriginalTxnId { get; set; }
    public string? RefundReason { get; set; }
    public Guid? RefundProcessedBy { get; set; }
    public string? InvoiceUrl { get; set; }
    public DateTime? CreditsAddedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = default!;
    public EmployerProfile? EmployerProfile { get; set; }
    public CandidateProfile? CandidateProfile { get; set; }
    public PaymentTransaction? OriginalTransaction { get; set; }
    public AdminUser? RefundAdmin { get; set; }
}
