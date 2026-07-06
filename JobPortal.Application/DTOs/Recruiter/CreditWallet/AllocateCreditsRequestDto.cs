using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.CreditWallet
{
    public class AllocateCreditsRequestDto
    {
        [Required]
        public Guid SubUserId { get; set; }

        [Range(1, int.MaxValue)]
        public int Credits { get; set; }
    }
}
