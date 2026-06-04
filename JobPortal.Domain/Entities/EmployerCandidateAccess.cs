using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class EmployerCandidateAccess
    {
        [Key] 
        public Guid AccessId { get; set; }

        public Guid EmployerId { get; set; }

        public Guid CandidateId { get; set; }

        public Guid UnlockId { get; set; }

        public DateTime GrantedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsActive { get; set; }
    }
}
