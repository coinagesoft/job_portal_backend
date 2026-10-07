using System.ComponentModel.DataAnnotations;

namespace JobPortal.Domain.Entities
{
    public class CouponRedemption
    {
        [Key]
        public Guid RedemptionId { get; set; }

        /// <summary>
        /// Coupon that was successfully redeemed.
        /// </summary>
        public Guid CouponId { get; set; }

        /// <summary>
        /// User who successfully used the coupon.
        /// Used for per-user usage restrictions.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Membership plan purchased using the coupon.
        /// </summary>
        public Guid PlanId { get; set; }

        /// <summary>
        /// Payment transaction associated with this redemption.
        /// </summary>
        public Guid? PaymentTransactionId { get; set; }

        /// <summary>
        /// Store the actual coupon code used at the time.
        ///
        /// This is intentionally stored even though CouponId exists,
        /// so historical records remain readable if an admin later
        /// changes the coupon configuration.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string CouponCode { get; set; } = default!;

        /// <summary>
        /// Original membership amount before coupon.
        /// Stored in paise/minor currency unit.
        /// </summary>
        public int OriginalAmountPaise { get; set; }

        /// <summary>
        /// Discount actually granted.
        /// Stored in paise/minor currency unit.
        /// </summary>
        public int DiscountAmountPaise { get; set; }

        /// <summary>
        /// Final amount after coupon discount.
        /// Stored in paise/minor currency unit.
        /// </summary>
        public int FinalAmountPaise { get; set; }

        /// <summary>
        /// When the coupon was successfully redeemed.
        /// </summary>
        public DateTime RedeemedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties

        public Coupon Coupon { get; set; } = default!;

        public User User { get; set; } = default!;

        public MembershipPlan MembershipPlan { get; set; } = default!;

        public PaymentTransaction? PaymentTransaction { get; set; }
    }
}