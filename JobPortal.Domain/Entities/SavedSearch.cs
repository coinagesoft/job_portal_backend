using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class SavedSearch
{
    public Guid SavedSearchId { get; set; }
    public Guid EmployerId { get; set; }
    public string SavedSearchName { get; set; } = default!;
    public string SearchFilters { get; set; } = default!;  // JSON
    public bool AlertEnabled { get; set; } = false;
    public DateTime CreatedAt { get; set; }

    public EmployerProfile EmployerProfile { get; set; } = default!;
}
