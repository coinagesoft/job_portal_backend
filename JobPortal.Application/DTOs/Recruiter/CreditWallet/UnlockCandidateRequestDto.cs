using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class UnlockCandidateRequestDto
    {
        [Required]
        public Guid CandidateId { get; set; }
    }
}
