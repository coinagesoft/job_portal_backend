using System;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Domain.Entities
{
    public class EmployerCreditPlan
    {
        [Key]
        public Guid EmployerCreditPlanId { get; set; }

        public Guid EmployerId { get; set; }

        public Guid PlanId { get; set; }

        [Required]
        [MaxLength(200)]
        public string PlanName { get; set; } = string.Empty;

        public int Credits { get; set; }

        public decimal Price { get; set; }

        public DateTime AssignedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsActive { get; set; }

        public Guid AssignedBy { get; set; }

        // Optional Navigation Properties
        public EmployerProfile? Employer { get; set; }

        public CreditPlan? CreditPlan { get; set; }
    }
}