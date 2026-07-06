using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class CvDownloadHistoryDto
    {
        public Guid DownloadId { get; set; }

        public Guid CandidateId { get; set; }

        public Guid CvId { get; set; }

        public Guid EmployerId { get; set; }

        public Guid? SubUserId { get; set; }

        public int CreditsUsed { get; set; }

        public DateTime DownloadedAt { get; set; }
    }
}
