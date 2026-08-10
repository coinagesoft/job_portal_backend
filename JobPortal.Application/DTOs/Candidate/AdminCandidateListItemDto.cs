using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate
{
    public class AdminCandidateListItemDto
    {
        public string Id { get; set; } = default!;
        public string? Img { get; set; }
        public string Name { get; set; } = default!;
        public string? Email { get; set; }
        public string? Trade { get; set; }
        public string Status { get; set; } = default!;
        public string Joined { get; set; } = default!; // formatted "MMM d, yyyy" to match the page
    }
}
