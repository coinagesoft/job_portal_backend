using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.CreditWallet
{
    public class AssignPlanRequestDto
    {
        public Guid EmployerId { get; set; }

        public Guid PlanId { get; set; }
    }
}
