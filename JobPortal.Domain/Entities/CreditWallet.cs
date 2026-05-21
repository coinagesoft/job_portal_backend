using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class CreditWallet
{
    public Guid WalletId { get; set; }
    public Guid EmployerId { get; set; }
    public int CreditBalance { get; set; } = 0;
    public string? PackageName { get; set; }
    public DateTime? PackExpiresAt { get; set; }
    public bool SharedWallet { get; set; } = true;
    public DateTime UpdatedAt { get; set; }

    public EmployerProfile EmployerProfile { get; set; } = default!;
}
