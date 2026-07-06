using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class CandidateCvDownload
    {
        public Guid Id { get; set; }

        public Guid CandidateId { get; set; }

        public Guid CvId { get; set; }

        public Guid EmployerId { get; set; }

        public Guid? SubUserId { get; set; }

        public int CreditsUsed { get; set; }

        public DateTime DownloadedAt { get; set; }

        // Navigation
        public CandidateCv CandidateCv { get; set; } = default!;
    }
}
