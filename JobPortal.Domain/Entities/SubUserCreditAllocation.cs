using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class SubUserCreditAllocation
    {
        [Key]
        public Guid AllocationId { get; set; }

        public Guid EmployerId { get; set; }

        public Guid SubUserId { get; set; }


        public int AllocatedCredits { get; set; }

        public int UsedCredits { get; set; }

        public int RemainingCredits { get; set; }

        public DateTime AllocatedAt { get; set; }

        public Guid AllocatedBy { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
