using JobPortal.Domain.Enums.common;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Domain.Entities
{
    public class Coupon
    {
        [Key]
        public Guid CouponId { get; set; }

        /// <summary>
        /// Coupon code entered by the customer.
        /// Example: INDIA50
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = default!;

        /// <summary>
        /// Recruiter or Candidate.
        /// </summary>
        public PlanType PlanType { get; set; }

        /// <summary>
        /// Pricing region.
        /// Example: in, us, ae
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Region { get; set; } = "in";

        /// <summary>
        /// Optional specific membership plan.
        /// Null means the coupon applies to all plans
        /// matching PlanType + Region.
        /// </summary>
        public Guid? PlanId { get; set; }

        /// <summary>
        /// Percentage or Fixed.
        /// </summary>
        public CouponDiscountType DiscountType { get; set; }

        /// <summary>
        /// Percentage value or fixed discount value
        /// in the plan's major currency unit.
        ///
        /// Percentage example:
        /// 20 = 20%
        ///
        /// Fixed example:
        /// 500 = ₹500
        /// </summary>
        public decimal DiscountValue { get; set; }

        /// <summary>
        /// Minimum membership price required to use the coupon.
        /// Null means there is no minimum amount.
        /// </summary>
        public decimal? MinimumAmount { get; set; }

        /// <summary>
        /// Maximum discount allowed for percentage coupons.
        /// Null means no maximum discount.
        /// </summary>
        public decimal? MaximumDiscount { get; set; }

        /// <summary>
        /// Maximum number of successful redemptions allowed.
        /// Null means unlimited usage.
        /// </summary>
        public int? UsageLimit { get; set; }

        /// <summary>
        /// Maximum number of times one user can successfully
        /// redeem this coupon.
        /// Null means unlimited per-user usage.
        /// </summary>
        public int? PerUserLimit { get; set; }

        /// <summary>
        /// Coupon becomes valid from this time.
        /// </summary>
        public DateTime? StartAt { get; set; }

        /// <summary>
        /// Coupon expires after this time.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Allows admin to manually enable/disable the coupon.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Cached successful redemption count.
        ///
        /// The actual usage history is stored in CouponRedemption.
        /// This value is maintained by the service for fast admin display.
        /// </summary>
        public int UsedCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid CreatedBy { get; set; }

        public Guid? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties

        /// <summary>
        /// Optional specific membership plan to which this coupon applies.
        /// </summary>
        public MembershipPlan? MembershipPlan { get; set; }

        /// <summary>
        /// All successful redemptions of this coupon.
        /// </summary>
        public ICollection<CouponRedemption> Redemptions { get; set; }
            = new List<CouponRedemption>();
    }
}