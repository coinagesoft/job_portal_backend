using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Recruiter.Applicants
{
    public class AddRecruiterNoteRequestDto
    {
        [Required]
        public string NoteText { get; set; } = string.Empty;
    }
}
