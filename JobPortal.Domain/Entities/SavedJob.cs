using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities;

public class SavedJob
{
    public Guid SavedJobId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public DateTime SavedAt { get; set; }

    public CandidateProfile CandidateProfile { get; set; } = default!;
    public JobPosting JobPosting { get; set; } = default!;
}
