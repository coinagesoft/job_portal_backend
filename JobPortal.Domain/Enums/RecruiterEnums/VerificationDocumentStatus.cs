using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Enums.RecruiterEnums
{
    public enum VerificationDocumentStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3,
        Expired = 4,
        Resubmission = 5
    }
}
