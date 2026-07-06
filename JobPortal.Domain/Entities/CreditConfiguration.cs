using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class CreditConfiguration
    {
        [Key]
        public Guid ConfigurationId { get; set; }

        public int ProfileUnlockCredits { get; set; }

        public int CvDownloadCredits { get; set; }

        public int CandidateAccessDays { get; set; }

        public bool IsActive { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Guid UpdatedBy { get; set; }
    }
}
