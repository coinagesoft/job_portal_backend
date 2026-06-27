using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Candidate
{
    public class FileUploadResult
    {
        public string Url { get; set; } = default!;

        public string PublicId { get; set; } = default!;
    }
}
