using JobPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobPortal.Infrastructure.Persistence.Configurations;

public class OtpVerificationConfiguration
    : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("otp_verifications");

        builder.HasKey(x => x.OtpId);

        builder.Property(x => x.OtpId)
            .HasColumnName("otp_id");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.MobileNumber)
            .HasColumnName("mobile_number")
            .HasMaxLength(15);

        builder.Property(x => x.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(6);

        builder.Property(x => x.OtpCode)
            .HasColumnName("otp_code")
            .HasMaxLength(6);

        builder.Property(x => x.OtpSentAt)
            .HasColumnName("otp_sent_at");

        builder.Property(x => x.OtpExpiresAt)
            .HasColumnName("otp_expires_at");

        builder.Property(x => x.ResendCooldownSec)
            .HasColumnName("resend_cooldown_sec");

        builder.Property(x => x.OtpAttempts)
            .HasColumnName("otp_attempts");

        builder.Property(x => x.IsVerified)
            .HasColumnName("is_verified");

        builder.Property(x => x.LockedUntil)
            .HasColumnName("locked_until");
    }
}