using JobPortal.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter
{
    public class UpdateSubUserRequestDto
    {
        [Required]
        public SubUserRole Role { get; set; }

        // Optional — override role defaults
        public bool? CanSearchCandidates { get; set; }
        public bool? CanUnlockProfiles { get; set; }
        public bool? CanPostJobs { get; set; }
        public bool? CanManageApplications { get; set; }
    }
}
