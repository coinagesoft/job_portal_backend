using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobPortal.Application.DTOs.Admin.Coupons
{
    public class CouponValidationResponseDto
    {
        public bool IsValid { get; set; }

        public string Message { get; set; } = string.Empty;

        public Guid? CouponId { get; set; }

        public string? CouponCode { get; set; }

        public Guid PlanId { get; set; }

        public decimal OriginalAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal FinalAmount { get; set; }

        public string? Currency { get; set; }

        public decimal? DiscountPercentage { get; set; }
    }
}
