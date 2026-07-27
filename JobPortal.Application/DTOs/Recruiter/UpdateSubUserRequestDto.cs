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
        // ── Permissions — the single source of truth now that the Role
        // dropdown is gone. Each flag is set directly from the checkbox
        // state on the Sub-Users page. ─────────────────────────────────
        public bool CanSearchCandidates { get; set; }
        public bool CanUnlockProfiles { get; set; }
        public bool CanPostJobs { get; set; }
        public bool CanManageApplications { get; set; }
    }
}