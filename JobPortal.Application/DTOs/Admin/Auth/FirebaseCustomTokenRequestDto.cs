using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Auth
{
    public class FirebaseCustomTokenRequestDto
    {
        public string MobileNumber { get; set; } = string.Empty;  // "9075309705"
        public string CountryCode { get; set; }      // "+91"
    }
}
