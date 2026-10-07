using JobPortal.Domain.Enums.common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Coupons
{
    public class CouponResponseDto
    {
        public Guid CouponId { get; set; }

        public string Code { get; set; } = string.Empty;

        public PlanType PlanType { get; set; }

        public string Region { get; set; } = string.Empty;

        public Guid? PlanId { get; set; }

        public string? PlanName { get; set; }

        public CouponDiscountType DiscountType { get; set; }

        public decimal DiscountValue { get; set; }

        public decimal? MinimumAmount { get; set; }

        public decimal? MaximumDiscount { get; set; }

        public int? UsageLimit { get; set; }

        public int? PerUserLimit { get; set; }

        public int UsedCount { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
