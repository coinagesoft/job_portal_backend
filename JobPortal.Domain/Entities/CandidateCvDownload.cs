using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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

        // Navigation — explicitly tied to CvId above. Without this attribute,
        // EF Core doesn't recognize CvId as the foreign key for this
        // navigation and instead creates a second, hidden shadow property
        // (historically "CandidateCvCvId") to back the relationship. That
        // shadow column is never set anywhere in code, so it always inserts
        // as an empty GUID — which can never match a real CV row, causing
        // every download to fail with a foreign key violation.
        [ForeignKey(nameof(CvId))]
        public CandidateCv CandidateCv { get; set; } = default!;
    }
}