using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Coupons
{
   
        public class UpdateCouponRequestDto
        {
            [Required]
            [StringLength(50)]
            public string Code { get; set; } = string.Empty;

            [Required]
            public PlanType PlanType { get; set; }

            [Required]
            [StringLength(20)]
            public string Region { get; set; } = "in";

            // Null = all plans for this PlanType + Region
            public Guid? PlanId { get; set; }

            [Required]
            public CouponDiscountType DiscountType { get; set; }

            [Range(0.01, double.MaxValue)]
            public decimal DiscountValue { get; set; }

            public decimal? MinimumAmount { get; set; }

            public decimal? MaximumDiscount { get; set; }

            [Range(1, int.MaxValue)]
            public int? UsageLimit { get; set; }

            [Range(1, int.MaxValue)]
            public int? PerUserLimit { get; set; }

            public DateTime? StartAt { get; set; }

            public DateTime? ExpiresAt { get; set; }

            public bool IsActive { get; set; }
        }
    }

