using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class SecurityDeposit
{
    public Guid DepositId { get; set; }
    public Guid EmployerId { get; set; }
    public Guid TransactionId { get; set; }
    public int AmountPaise { get; set; } = 200000;
    public string DepositStatus { get; set; } = "Held";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public EmployerProfile EmployerProfile { get; set; } = default!;
    public PaymentTransaction PaymentTransaction { get; set; } = default!;
}
