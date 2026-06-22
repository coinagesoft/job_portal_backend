using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate.Auth
{
   public class CreateCandidateOrderResponseDto
{
        public bool Success { get; set; }

        public string OrderId { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "INR";

        public string Message { get; set; } = string.Empty;
    }
}
