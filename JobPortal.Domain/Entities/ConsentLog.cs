using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class ConsentLog
{
    public Guid ConsentLogId { get; set; }
    public Guid UserId { get; set; }
    public string ConsentType { get; set; } = default!;
    public bool ConsentGiven { get; set; }
    public DateTime ConsentTimestamp { get; set; }
    public string DataResidency { get; set; } = "AWS Mumbai (ap-south-1)";
    public string NationalIdStorage { get; set; } = "Hash_Only";
    public string ConsentVersion { get; set; } = default!;

    public User User { get; set; } = default!;
}
