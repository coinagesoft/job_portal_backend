using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Coupons
{
    public class ValidateCouponRequestDto
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        public PlanType PlanType { get; set; }

        [Required]
        public Guid PlanId { get; set; }

        [Required]
        [StringLength(20)]
        public string Region { get; set; } = "in";
    }
}
