using JobPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobPortal.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.UserId);

        builder.Property(x => x.UserId)
            .HasColumnName("user_id");

        builder.Property(x => x.UserType)
            .HasColumnName("user_type")
            .HasConversion<string>();

        builder.Property(x => x.MobileNumber)
            .HasColumnName("mobile_number")
            .HasMaxLength(15);

        builder.Property(x => x.CountryCode)
            .HasColumnName("country_code")
            .HasMaxLength(6);

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(255);

        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash");

        builder.Property(x => x.AccountStatus)
            .HasColumnName("account_status")
            .HasConversion<string>();

        builder.Property(x => x.KycStatus)
            .HasColumnName("kyc_status")
            .HasConversion<string>();

        builder.Property(x => x.PaymentStatus)
            .HasColumnName("payment_status")
            .HasConversion<string>();

        builder.Property(x => x.LastLoginAt)
            .HasColumnName("last_login_at");

        builder.Property(x => x.SuspensionReason)
            .HasColumnName("suspension_reason");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");
    }
}