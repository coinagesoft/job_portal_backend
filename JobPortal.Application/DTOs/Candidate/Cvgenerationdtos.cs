using System;

namespace JobPortal.Application.DTOs.Candidate
{
    public class GenerateCvResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        /// <summary>Public URL of the freshly generated Portal CV PDF.</summary>
        public string? GeneratedCvUrl { get; set; }

        public DateTime? GeneratedAt { get; set; }
    }
}