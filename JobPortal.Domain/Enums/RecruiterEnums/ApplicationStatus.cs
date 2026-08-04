using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Domain.Enums.RecruiterEnums
{
    public enum ApplicationStatus
    {
        Applied = 1,
        InReview = 2,
        Shortlisted = 3,
        Interview = 4,
        Rejected = 5,
        Hired = 6,
        Withdrawn = 7,
        TableInterview = 8,
        CvSelection = 9,
        LocationInterview = 10
    }
}