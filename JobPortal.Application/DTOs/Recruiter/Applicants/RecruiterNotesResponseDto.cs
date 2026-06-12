using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{

    public class RecruiterNotesResponseDto
    {
        public List<RecruiterNoteItemDto> Notes { get; set; }
            = new();
    }
}
