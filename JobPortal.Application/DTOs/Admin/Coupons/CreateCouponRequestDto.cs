using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Coupons
{
 
        public class CreateCouponRequestDto
        {
            [Required]
            [StringLength(50)]
            public string Code { get; set; } = string.Empty;

            [Required]
            public PlanType PlanType { get; set; }

            [Required]
            [StringLength(20)]
            public string Region { get; set; } = "in";

            // Null = applies to all plans of this PlanType + Region
            public Guid? PlanId { get; set; }

            [Required]
            public CouponDiscountType DiscountType { get; set; }

            [Range(0.01, double.MaxValue)]
            public decimal DiscountValue { get; set; }

            // Minimum membership plan price required to use coupon.
            public decimal? MinimumAmount { get; set; }

            // Maximum discount allowed for percentage coupons.
            public decimal? MaximumDiscount { get; set; }

            // Null = unlimited usage.
            [Range(1, int.MaxValue)]
            public int? UsageLimit { get; set; }

            // Null = unlimited usage per user.
            [Range(1, int.MaxValue)]
            public int? PerUserLimit { get; set; }

            public DateTime? StartAt { get; set; }

            public DateTime? ExpiresAt { get; set; }

            public bool IsActive { get; set; } = true;
        }
}
