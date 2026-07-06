using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{

    public class RecruiterNoteItemDto
    {
        public Guid RecruiterNoteId { get; set; }

        public string NoteText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
