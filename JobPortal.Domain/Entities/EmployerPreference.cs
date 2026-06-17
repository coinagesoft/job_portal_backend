using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Entities
{
    public class EmployerPreference
    {
        [Key]
        public Guid PreferenceId { get; set; }

        public Guid EmployerId { get; set; }

        public string PrimaryLanguage { get; set; } = "English";

        public string? SecondaryLanguage { get; set; }

        public int ItemsPerPage { get; set; } = 10;

        public string DateFormat { get; set; } = "dd MMM yyyy";

        public bool MarketingEmailsEnabled { get; set; }

        public bool PlatformUpdatesEnabled { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
