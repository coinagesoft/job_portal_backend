using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Settings
{
    /// <summary>
    /// Shared response shape for the Settings ▸ Delete Account danger-zone
    /// actions: Deactivate Account, Delete All Jobs, Delete Account.
    /// </summary>
    public class DangerZoneActionResponseDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = default!;

        /// <summary>
        /// Populated by DeleteAllJobsAsync/DeleteAccountAsync — how many
        /// job postings were archived by the action.
        /// </summary>
        public int? JobsAffected { get; set; }
    }
}