using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JobPortal.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // Section 1 — Auth
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();

    // Section 2 — Candidate
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<CandidateEducation> CandidateEducations => Set<CandidateEducation>();
    public DbSet<CandidateWorkHistory> CandidateWorkHistories => Set<CandidateWorkHistory>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<CandidateCv> CandidateCvs => Set<CandidateCv>();

    // Section 3 — KYC
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<PassportVerification> PassportVerifications => Set<PassportVerification>();
    public DbSet<ItiCertificateReview> ItiCertificateReviews => Set<ItiCertificateReview>();

    // Section 4 — Employer
    public DbSet<EmployerProfile> EmployerProfiles => Set<EmployerProfile>();
    public DbSet<EmployerBadge> EmployerBadges => Set<EmployerBadge>();
    public DbSet<EmployerSubUser> EmployerSubUsers => Set<EmployerSubUser>();
    public DbSet<EmployerNotificationSetting> EmployerNotificationSettings => Set<EmployerNotificationSetting>();

    // Section 5 — Jobs
    public DbSet<JobPosting> JobPostings => Set<JobPosting>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<SavedJob> SavedJobs => Set<SavedJob>();
    public DbSet<SavedSearch> SavedSearches => Set<SavedSearch>();
    public DbSet<CandidateUnlock> CandidateUnlocks => Set<CandidateUnlock>();

    // Section 6 — Payments
    public DbSet<CreditWallet> CreditWallets => Set<CreditWallet>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<SecurityDeposit> SecurityDeposits => Set<SecurityDeposit>();

    // Section 7 — Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    // Section 8 — Admin Config
    public DbSet<PlatformConfig> PlatformConfigs => Set<PlatformConfig>();
    public DbSet<CountryVerificationConfig> CountryVerificationConfigs => Set<CountryVerificationConfig>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ConsentLog> ConsentLogs => Set<ConsentLog>();
    public DbSet<Dispute> Disputes => Set<Dispute>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        // ── users ──────────────────────────────────────────────

        var userTypeConverter =
    new EnumToStringConverter<UserType>();

        var accountStatusConverter =
            new EnumToStringConverter<AccountStatus>();

        var kycStatusConverter =
            new EnumToStringConverter<KycStatus>();

        var paymentStatusConverter =
            new EnumToStringConverter<PaymentStatus>();
        m.Entity<User>(e =>
        {
            e.ToTable("users");

            e.HasKey(x => x.UserId);

            e.Property(x => x.UserId)
                .HasColumnName("user_id");

            e.Property(x => x.UserType)
                .HasColumnName("user_type")
                .HasConversion(userTypeConverter);

            e.Property(x => x.MobileNumber)
                .HasColumnName("mobile_number");

            e.Property(x => x.CountryCode)
                .HasColumnName("country_code")
                .HasDefaultValue("+91");

            e.Property(x => x.Email)
                .HasColumnName("email");

            e.Property(x => x.PasswordHash)
                .HasColumnName("password_hash");

            e.Property(x => x.AccountStatus)
                .HasColumnName("account_status")
                .HasConversion(accountStatusConverter)
                .HasDefaultValue(AccountStatus.Pending);

            e.Property(x => x.KycStatus)
                .HasColumnName("kyc_status")
                .HasConversion(kycStatusConverter)
                .HasDefaultValue(KycStatus.Pending);

            e.Property(x => x.PaymentStatus)
                .HasColumnName("payment_status")
                .HasConversion(paymentStatusConverter)
                .HasDefaultValue(PaymentStatus.Unpaid);

            e.Property(x => x.LastLoginAt)
                .HasColumnName("last_login_at");

            e.Property(x => x.SuspensionReason)
                .HasColumnName("suspension_reason");

            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            e.HasIndex(x => x.MobileNumber)
                .IsUnique()
                .HasDatabaseName("uq_users_mobile");

            e.HasIndex(x => x.Email)
                .IsUnique()
                .HasDatabaseName("uq_users_email");
        });

        // ── otp_verifications ──────────────────────────────────
        m.Entity<OtpVerification>(e => {
            e.ToTable("otp_verifications");
            e.HasKey(x => x.OtpId);
            e.Property(x => x.OtpId).HasColumnName("otp_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.MobileNumber).HasColumnName("mobile_number");
            e.Property(x => x.CountryCode).HasColumnName("country_code");
            e.Property(x => x.OtpCode).HasColumnName("otp_code");
            e.Property(x => x.OtpSentAt).HasColumnName("otp_sent_at");
            e.Property(x => x.OtpExpiresAt).HasColumnName("otp_expires_at");
            e.Property(x => x.ResendCooldownSec).HasColumnName("resend_cooldown_sec");
            e.Property(x => x.OtpAttempts).HasColumnName("otp_attempts");
            e.Property(x => x.IsVerified).HasColumnName("is_verified");
            e.Property(x => x.LockedUntil).HasColumnName("locked_until");
        });

        // ── admin_users ────────────────────────────────────────
        m.Entity<AdminUser>(e => {
            e.ToTable("admin_users");
            e.HasKey(x => x.AdminId);
            e.Property(x => x.AdminId).HasColumnName("admin_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.AdminIdentifier).HasColumnName("admin_identifier");
            e.Property(x => x.AdminRole).HasColumnName("admin_role");
            e.Property(x => x.Permissions).HasColumnName("permissions");
            e.Property(x => x.FailedAttempts).HasColumnName("failed_attempts");
            e.Property(x => x.LockedUntil).HasColumnName("locked_until");
            e.Property(x => x.IsActive).HasColumnName("is_active");
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.AdminIdentifier).IsUnique();
            e.HasOne(x => x.User)
             .WithOne()
             .HasForeignKey<AdminUser>(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── admin_sessions ─────────────────────────────────────
        m.Entity<AdminSession>(e => {
            e.ToTable("admin_sessions");
            e.HasKey(x => x.SessionId);
            e.Property(x => x.SessionId).HasColumnName("session_id");
            e.Property(x => x.AdminId).HasColumnName("admin_id");
            e.Property(x => x.SessionToken).HasColumnName("session_token");
            e.Property(x => x.IpAddress).HasColumnName("ip_address");
            e.Property(x => x.TrustedDevice).HasColumnName("trusted_device");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.HasOne(x => x.AdminUser)
             .WithMany()
             .HasForeignKey(x => x.AdminId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── candidate_profiles ─────────────────────────────────
        m.Entity<CandidateProfile>(e => {
            e.ToTable("candidate_profiles");
            e.HasKey(x => x.CandidateId);
            e.Property(x => x.CandidateId).HasColumnName("candidate_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.FullName).HasColumnName("full_name");
            e.Property(x => x.ProfilePhotoUrl).HasColumnName("profile_photo_url");
            e.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
            e.Property(x => x.Gender).HasColumnName("gender");
            e.Property(x => x.Nationality).HasColumnName("nationality");
            e.Property(x => x.CurrentCity).HasColumnName("current_city");
            e.Property(x => x.CurrentState).HasColumnName("current_state");
            e.Property(x => x.PreferredWorkLocation).HasColumnName("preferred_work_location");
            e.Property(x => x.PreferredSalary).HasColumnName("preferred_salary");
            e.Property(x => x.AvailabilityStatus).HasColumnName("availability_status");
            e.Property(x => x.AvailabilityUpdatedAt).HasColumnName("availability_updated_at");
            e.Property(x => x.DisabilityStatus).HasColumnName("disability_status");
            e.Property(x => x.DisabilityNote).HasColumnName("disability_note");
            e.Property(x => x.PrimaryTrade).HasColumnName("primary_trade");
            e.Property(x => x.TotalExperienceYears).HasColumnName("total_experience_years");
            e.Property(x => x.ItiCertified).HasColumnName("iti_certified");
            e.Property(x => x.ItiTrade).HasColumnName("iti_trade");
            e.Property(x => x.ItiMarks).HasColumnName("iti_marks");
            e.Property(x => x.ItiCollege).HasColumnName("iti_college");
            e.Property(x => x.Band).HasColumnName("band");
            e.Property(x => x.AiMatchScore).HasColumnName("ai_match_score");
            e.Property(x => x.ProfileStatus).HasColumnName("profile_status");
            e.Property(x => x.ProfileCompletionPct).HasColumnName("profile_completion_pct");
            e.Property(x => x.ReengagementResponse).HasColumnName("reengagement_response");
            e.Property(x => x.LastAppliedAt).HasColumnName("last_applied_at");
            e.Property(x => x.FcmToken).HasColumnName("fcm_token");
            e.Property(x => x.AdminNotes).HasColumnName("admin_notes");
            e.Property(x => x.CreditBalance).HasColumnName("credit_balance");
            e.Property(x => x.WelcomeEmailSent).HasColumnName("welcome_email_sent");
            e.Property(x => x.NewsletterOptIn).HasColumnName("newsletter_opt_in");
            e.Property(x => x.TempPasswordFlag).HasColumnName("temp_password_flag");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.User).WithOne()
             .HasForeignKey<CandidateProfile>(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── All remaining tables follow same pattern ────────────
        // EF will auto-map remaining properties by convention
        // since column names match C# property names after snake_case mapping

        m.Entity<CandidateEducation>(e => {
            e.ToTable("candidate_education");
            e.HasKey(x => x.EducationId);
            e.HasOne(x => x.CandidateProfile)
             .WithMany(x => x.Educations)
             .HasForeignKey(x => x.CandidateId);
        });

        m.Entity<CandidateWorkHistory>(e => {
            e.ToTable("candidate_work_history");
            e.HasKey(x => x.WorkId);
            e.HasOne(x => x.CandidateProfile)
             .WithMany(x => x.WorkHistories)
             .HasForeignKey(x => x.CandidateId);
        });

        m.Entity<CandidateSkill>(e => {
            e.ToTable("candidate_skills");
            e.HasKey(x => x.SkillId);
            e.HasOne(x => x.CandidateProfile)
             .WithMany(x => x.Skills)
             .HasForeignKey(x => x.CandidateId);
        });

        m.Entity<CandidateCv>(e => {
            e.ToTable("candidate_cv");
            e.HasKey(x => x.CvId);
            e.HasOne(x => x.CandidateProfile)
             .WithMany(x => x.Cvs)
             .HasForeignKey(x => x.CandidateId);
        });

        m.Entity<KycVerification>(e => {
            e.ToTable("kyc_verifications");
            e.HasKey(x => x.VerificationId);
            e.HasOne(x => x.CandidateProfile)
             .WithMany()
             .HasForeignKey(x => x.CandidateId);
            e.HasOne(x => x.Reviewer)
             .WithMany()
             .HasForeignKey(x => x.ReviewedBy)
             .OnDelete(DeleteBehavior.SetNull);
        });

        m.Entity<PassportVerification>(e => {
            e.ToTable("passport_verifications");
            e.HasKey(x => x.PassportVerId);
            e.HasOne(x => x.CandidateProfile)
             .WithMany()
             .HasForeignKey(x => x.CandidateId);
            e.HasOne(x => x.Reviewer)
             .WithMany()
             .HasForeignKey(x => x.ReviewedBy)
             .OnDelete(DeleteBehavior.SetNull);
        });

        m.Entity<ItiCertificateReview>(e => {
            e.ToTable("iti_certificate_reviews");
            e.HasKey(x => x.ItiReviewId);
            e.HasOne(x => x.CandidateProfile)
             .WithMany()
             .HasForeignKey(x => x.CandidateId);
        });

        m.Entity<EmployerProfile>(e => {
            e.ToTable("employer_profiles");
            e.HasKey(x => x.EmployerId);
            e.HasIndex(x => x.Gstn).IsUnique();
            e.HasOne(x => x.User).WithOne()
             .HasForeignKey<EmployerProfile>(x => x.UserId);
        });

        m.Entity<EmployerBadge>(e => {
            e.ToTable("employer_badges");
            e.HasKey(x => x.BadgeId);
            e.HasOne(x => x.EmployerProfile)
             .WithMany(x => x.Badges)
             .HasForeignKey(x => x.EmployerId);
            e.HasOne(x => x.IssuedByAdmin)
             .WithMany()
             .HasForeignKey(x => x.IssuedBy);
        });

        m.Entity<EmployerSubUser>(e => {
            e.ToTable("employer_sub_users");
            e.HasKey(x => x.SubUserId);
            e.HasOne(x => x.EmployerProfile)
             .WithMany(x => x.SubUsers)
             .HasForeignKey(x => x.EmployerId);
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId);
        });

        m.Entity<EmployerNotificationSetting>(e => {
            e.ToTable("employer_notification_settings");
            e.HasKey(x => x.NotifPrefId);
            e.HasIndex(x => x.EmployerId).IsUnique();
            e.HasOne(x => x.EmployerProfile)
             .WithOne(x => x.NotificationSetting)
             .HasForeignKey<EmployerNotificationSetting>(x => x.EmployerId);
        });

        m.Entity<JobPosting>(e => {
            e.ToTable("job_postings");
            e.HasKey(x => x.JobId);
            e.HasOne(x => x.EmployerProfile)
             .WithMany()
             .HasForeignKey(x => x.EmployerId);
            e.HasOne(x => x.PostedBySubUser)
             .WithMany()
             .HasForeignKey(x => x.PostedBySubUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        m.Entity<JobApplication>(e => {
            e.ToTable("job_applications");
            e.HasKey(x => x.ApplicationId);
            e.HasIndex(x => new { x.JobId, x.CandidateId }).IsUnique();
            e.HasOne(x => x.JobPosting)
             .WithMany(x => x.Applications)
             .HasForeignKey(x => x.JobId);
            e.HasOne(x => x.CandidateProfile)
             .WithMany()
             .HasForeignKey(x => x.CandidateId);
            e.HasOne(x => x.EmployerProfile)
             .WithMany()
             .HasForeignKey(x => x.EmployerId);
        });

        m.Entity<SavedJob>(e => {
            e.ToTable("saved_jobs");
            e.HasKey(x => x.SavedJobId);
            e.HasIndex(x => new { x.CandidateId, x.JobId }).IsUnique();
        });

        m.Entity<SavedSearch>(e => {
            e.ToTable("saved_searches");
            e.HasKey(x => x.SavedSearchId);
        });

        m.Entity<CandidateUnlock>(e => {
            e.ToTable("candidate_unlocks");
            e.HasKey(x => x.UnlockId);
            e.HasIndex(x => new { x.EmployerId, x.CandidateId }).IsUnique();
        });

        m.Entity<CreditWallet>(e => {
            e.ToTable("credit_wallets");
            e.HasKey(x => x.WalletId);
            e.HasIndex(x => x.EmployerId).IsUnique();
        });

        m.Entity<PaymentTransaction>(e => {
            e.ToTable("payment_transactions");
            e.HasKey(x => x.TransactionId);
            e.HasOne(x => x.OriginalTransaction)
             .WithMany()
             .HasForeignKey(x => x.OriginalTxnId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RefundAdmin)
             .WithMany()
             .HasForeignKey(x => x.RefundProcessedBy)
             .OnDelete(DeleteBehavior.SetNull);
        });

        m.Entity<Invoice>(e => {
            e.ToTable("invoices");
            e.HasKey(x => x.InvoiceId);
            e.HasIndex(x => x.InvoiceNumber).IsUnique();
        });

        m.Entity<SecurityDeposit>(e => {
            e.ToTable("security_deposits");
            e.HasKey(x => x.DepositId);
            e.HasIndex(x => x.EmployerId).IsUnique();
        });

        m.Entity<Notification>(e => {
            e.ToTable("notifications");
            e.HasKey(x => x.NotificationId);
        });

        m.Entity<SupportTicket>(e => {
            e.ToTable("support_tickets");
            e.HasKey(x => x.TicketId);
            e.HasOne(x => x.RaisedByUser)
             .WithMany()
             .HasForeignKey(x => x.RaisedBy);
            e.HasOne(x => x.AssignedAdmin)
             .WithMany()
             .HasForeignKey(x => x.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);
        });

        m.Entity<PlatformConfig>(e => {
            e.ToTable("platform_config");
            e.HasKey(x => x.ConfigId);
            e.HasOne(x => x.UpdatedByAdmin)
             .WithMany()
             .HasForeignKey(x => x.UpdatedBy);
        });

        m.Entity<CountryVerificationConfig>(e => {
            e.ToTable("country_verification_config");
            e.HasKey(x => x.ConfigId);
            e.HasIndex(x => x.CountryCode).IsUnique();
            e.HasOne(x => x.UpdatedByAdmin)
             .WithMany()
             .HasForeignKey(x => x.ConfigUpdatedBy);
        });

        m.Entity<AuditLog>(e => {
            e.ToTable("audit_logs");
            e.HasKey(x => x.LogId);
            e.HasOne(x => x.PerformedByAdmin)
             .WithMany()
             .HasForeignKey(x => x.PerformedBy);
        });

        m.Entity<ConsentLog>(e => {
            e.ToTable("consent_logs");
            e.HasKey(x => x.ConsentLogId);
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId);
        });

        m.Entity<Dispute>(e => {
            e.ToTable("disputes");
            e.HasKey(x => x.DisputeId);
            e.HasOne(x => x.RaisedByUser)
             .WithMany()
             .HasForeignKey(x => x.RaisedBy);
            e.HasOne(x => x.AssignedAdmin)
             .WithMany()
             .HasForeignKey(x => x.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}