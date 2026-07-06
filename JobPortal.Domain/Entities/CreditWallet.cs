using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Domain.Entities;

[Table("credit_wallets")]
public class CreditWallet
{
    [Key]
    [Column("wallet_id")]
    public Guid Wallet_Id { get; set; }

    [Column("employer_id")]
    public Guid EmployerId { get; set; }

    [Column("credit_balance")]
    public int CreditBalance { get; set; } = 0;

    [Column("package_name")]
    [MaxLength(100)]
    public string? PackageName { get; set; }

    [Column("pack_expires_at")]
    public DateTime? PackExpiresAt { get; set; }

    [Column("shared_wallet")]
    public bool SharedWallet { get; set; } = true;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey(nameof(EmployerId))]
    public EmployerProfile EmployerProfile { get; set; } = default!;
}
