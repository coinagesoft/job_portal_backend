using JobPortal.Domain.Entities;
using JobPortal.Domain.Entities.AI;
using JobPortal.Domain.Common;
using JobPortal.Domain.Enums;
using JobPortal.Domain.Enums.common;
using JobPortal.Domain.Enums.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection.Emit;
using System.Text.Json;
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
    public DbSet<CreditPlan> CreditPlans { get; set; }
    public DbSet<MembershipPlan> MembershipPlans { get; set; }

    public DbSet<CreditUsageTransaction> CreditUsageTransactions { get; set; }

    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<EmployerPreference> EmployerPreferences { get; set; }

    public DbSet<CandidateCvDownload> CandidateCvDownloads { get; set; }

    public DbSet<CreditConfiguration> CreditConfigurations { get; set; }

    public DbSet<CreditAllocationHistory> CreditAllocationHistory { get; set; }
    public DbSet<EmployerCandidateAccess> EmployerCandidateAccesses { get; set; }
    public DbSet<SubUserCreditAllocation> SubUserCreditAllocation { get; set; }
    public DbSet<AdminSession> AdminSessions => Set<AdminSession>();
    public DbSet<EmployerPlanPurchase> EmployerPlanPurchase { get; set; }

    // Section 2 — Candidate
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<CandidateEducation> CandidateEducations => Set<CandidateEducation>();
    public DbSet<CandidateWorkHistory> CandidateWorkHistories => Set<CandidateWorkHistory>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<CandidateCv> CandidateCvs => Set<CandidateCv>();

    // Section 3 — KYC
    public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
    public DbSet<PassportVerification> PassportVerifications { get; set; }
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
    public DbSet<SupportTicketReply> SupportTicketReplies => Set<SupportTicketReply>();
    // Section 8 — Admin Config
    public DbSet<PlatformConfig> PlatformConfigs => Set<PlatformConfig>();
    public DbSet<LegalDocument> LegalDocuments => Set<LegalDocument>();
    public DbSet<CountryVerificationConfig> CountryVerificationConfigs => Set<CountryVerificationConfig>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ConsentLog> ConsentLogs => Set<ConsentLog>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<RegistrationSession> RegistrationSessions => Set<RegistrationSession>();

    public DbSet<RecruiterNote> RecruiterNotes { get; set; }
    public DbSet<CandidateNotificationSetting> CandidateNotificationSettings => Set<CandidateNotificationSetting>();

    public DbSet<CandidatePreferenceSetting> CandidatePreferenceSettings => Set<CandidatePreferenceSetting>();

    public DbSet<CandidateLogoutSession> CandidateLogoutSessions => Set<CandidateLogoutSession>();
    public DbSet<CandidateEmbedding> CandidateEmbeddings { get; set; }
    public DbSet<CandidateDocument> CandidateDocuments => Set<CandidateDocument>();

    public DbSet<EmployerVerificationDocument> EmployerVerificationDocuments { get; set; }

    public DbSet<VerificationDocumentMaster> VerificationDocumentMasters { get; set; }

    public DbSet<RegistrationSessionDocument> RegistrationSessionDocuments => Set<RegistrationSessionDocument>();

    public DbSet<AdminRole> AdminRoles => Set<AdminRole>();


    public DbSet<AdminEmailOtp> AdminEmailOtps => Set<AdminEmailOtp>();

    public DbSet<AdminLoginLog> AdminLoginLogs => Set<AdminLoginLog>();

    //public DbSet<JobEmbedding> JobEmbeddings { get; set; }
    public DbSet<JobEmbedding> JobEmbeddings =>
    Set<JobEmbedding>();
    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        // Handle RegistrationSession (has ExpiresAt too)
        foreach (var entry in ChangeTracker.Entries<RegistrationSession>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.ExpiresAt = now.AddHours(24);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }

        // Handle User
        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }

        // Handle EmployerProfile
        foreach (var entry in ChangeTracker.Entries<EmployerProfile>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Entity.UpdatedAt = now;
            }
        }

        // Handle CreditWallet (UpdatedAt only, no CreatedAt)
        foreach (var entry in ChangeTracker.Entries<CreditWallet>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }

    protected override void OnModelCreating(ModelBuilder m)
    {

        // ── Candidate Documents (unified, OCR-parsed) ─────────
        m.Entity<CandidateDocument>(e =>
        {
            e.ToTable("candidate_documents");

            e.HasKey(x => x.DocumentId);

            e.Property(x => x.DocumentId).HasColumnName("document_id");
            e.Property(x => x.CandidateId).HasColumnName("candidate_id");
            e.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(100);
            e.Property(x => x.FileUrl).HasColumnName("file_url");
            e.Property(x => x.FilePublicId).HasColumnName("file_public_id");
            e.Property(x => x.ParsedName).HasColumnName("parsed_name");
            e.Property(x => x.ParsedDataJson).HasColumnName("parsed_data_json").HasColumnType("text");
            e.Property(x => x.VerificationStatus).HasColumnName("verification_status").HasMaxLength(20);
            e.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");

            e.HasIndex(x => new { x.CandidateId, x.DocumentType });

            e.HasOne(x => x.Candidate)
                .WithMany()
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // ── AI Embeddings ─────────────────────────────

        m.Entity<CandidateEmbedding>(e =>
        {
            e.ToTable("candidate_embeddings");

            e.HasKey(x => x.CandidateId);

            e.Property(x => x.CandidateId)
                .HasColumnName("candidate_id");

            e.Property(x => x.EmbeddingJson)
                .HasColumnName("embedding_json")
                .HasColumnType("text");

            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            e.HasOne(x => x.Candidate)
                .WithMany()
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<JobEmbedding>(e =>
        {
            e.ToTable("job_embeddings");

            e.HasKey(x => x.JobId);

            e.Property(x => x.JobId)
                .HasColumnName("job_id");

            e.Property(x => x.EmbeddingJson)
                .HasColumnName("embedding_json")
                .HasColumnType("text");

            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            e.HasOne(x => x.JobPosting)
                .WithMany()
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        // ── users ──────────────────────────────────────────────

        var userTypeConverter =
    new ValueConverter<UserType, string>(
        v => UserTypeConverterHelper.ToProviderString(v),
        v => UserTypeConverterHelper.FromProviderString(v));

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

        m.Entity<SupportTicketReply>(e =>
        {
            e.ToTable("support_ticket_replies");

            e.HasKey(x => x.ReplyId);

            e.Property(x => x.ReplyId)
                .HasColumnName("reply_id");

            e.Property(x => x.TicketId)
                .HasColumnName("ticket_id");

            e.Property(x => x.SenderId)
                .HasColumnName("sender_id");

            e.Property(x => x.SenderType)
                .HasColumnName("sender_type")
                .HasConversion<string>();

            e.Property(x => x.Message)
                .HasColumnName("message");

            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            e.HasOne(x => x.Ticket)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<RecruiterNote>(entity =>
        {
            entity.HasKey(n => n.RecruiterNoteId);

            entity.HasOne(n => n.JobApplication)
                  .WithMany(a => a.RecruiterNotes)
                  .HasForeignKey(n => n.ApplicationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.EmployerProfile)
                  .WithMany()
                  .HasForeignKey(n => n.EmployerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.Property(n => n.NoteText)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(n => n.IsAcknowledged)
                  .HasDefaultValue(false);
        });

        m.Entity<CandidateLogoutSession>(e =>
        {
            e.ToTable("candidate_logout_sessions");
            e.HasKey(x => x.LogoutSessionId);
            e.HasIndex(x => x.CandidateId);
            e.HasIndex(x => x.JwtJti);
            e.HasOne(x => x.CandidateProfile)
                .WithMany()
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<CandidateNotificationSetting>(e =>
        {
            e.ToTable("candidate_notification_settings");

            e.HasKey(x => x.NotifPrefId);

            e.HasIndex(x => x.CandidateId)
                .IsUnique();

            e.HasOne(x => x.CandidateProfile)
                .WithOne()
                .HasForeignKey<CandidateNotificationSetting>(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<CandidatePreferenceSetting>(e =>
        {
            e.ToTable("candidate_preference_settings");

            e.HasKey(x => x.PrefId);

            e.HasIndex(x => x.CandidateId)
                .IsUnique();

            e.HasOne(x => x.CandidateProfile)
                .WithOne()
                .HasForeignKey<CandidatePreferenceSetting>(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── otp_verifications ──────────────────────────────────
        m.Entity<OtpVerification>(e => {
            e.ToTable("otp_verifications");
            e.HasKey(x => x.OtpId);
            e.Property(x => x.OtpId).HasColumnName("otp_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.MobileNumber).HasColumnName("mobile_number");
            e.Property(x => x.CountryCode).HasColumnName("country_code");
            e.Property(x => x.OtpCode).HasColumnName("otp_code").HasColumnType("varchar(255)");
            e.Property(x => x.OtpSentAt).HasColumnName("otp_sent_at");
            e.Property(x => x.OtpExpiresAt).HasColumnName("otp_expires_at");
            e.Property(x => x.ResendCooldownSec).HasColumnName("resend_cooldown_sec");
            e.Property(x => x.OtpAttempts).HasColumnName("otp_attempts");
            e.Property(x => x.IsVerified).HasColumnName("is_verified");
            e.Property(x => x.LockedUntil).HasColumnName("locked_until");
        });

        // ── admin_users ────────────────────────────────────────
        m.Entity<AdminUser>(entity =>
        {
            entity.HasKey(x => x.AdminId);

            entity.Property(x => x.AdminIdentifier)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.AdminIdentifier)
                .IsUnique();

            entity.Property(x => x.FullName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.AdminType)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.PermissionOverrides)
                .HasColumnType("jsonb");

            // User (1:1)
            entity.HasOne(x => x.User)
                .WithOne()
                .HasForeignKey<AdminUser>(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Role (Many -> One)
            entity.HasOne(x => x.Role)
                .WithMany(x => x.AdminUsers)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // CreatedBy (Self Reference)
            entity.HasOne(x => x.CreatedByAdmin)
                .WithMany(x => x.CreatedAdmins)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<AdminRole>(entity =>
        {
            entity.HasKey(x => x.RoleId);

            entity.Property(x => x.RoleName)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(x => x.RoleName)
                .IsUnique();

            entity.Property(x => x.Permissions)
                .HasColumnType("jsonb");

            entity.Property(x => x.Description)
                .HasMaxLength(500);
        });
        // ── admin_sessions ─────────────────────────────────────
        m.Entity<AdminSession>(entity =>
        {
            entity.HasKey(x => x.SessionId);

            entity.Property(x => x.JwtId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.RefreshToken)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.IpAddress)
                .HasMaxLength(100);

            entity.Property(x => x.UserAgent)
                .HasMaxLength(500);

            entity.HasOne(x => x.AdminUser)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<AdminEmailOtp>(entity =>
        {
            entity.HasKey(x => x.OtpId);

            entity.Property(x => x.Email)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.OtpCode)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(x => x.Purpose)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasOne(x => x.AdminUser)
                .WithMany()
                .HasForeignKey(x => x.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<AdminLoginLog>(entity =>
        {
            entity.HasKey(x => x.LoginLogId);

            entity.Property(x => x.Email)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.IpAddress)
                .HasMaxLength(100);

            entity.Property(x => x.UserAgent)
                .HasMaxLength(500);

            entity.Property(x => x.FailureReason)
                .HasMaxLength(500);

            entity.HasOne(x => x.AdminUser)
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
            e.Property(x => x.WelcomeEmailSent).HasColumnName("welcome_email_sent");
            e.Property(x => x.NewsletterOptIn).HasColumnName("newsletter_opt_in");
            e.Property(x => x.TempPasswordFlag).HasColumnName("temp_password_flag");
            e.Property(x => x.Pincode).HasColumnName("Pincode");
            e.Property(x => x.ProfessionalSummary).HasColumnName("ProfessionalSummary");
            e.Property(x => x.About).HasColumnName("About");
            e.Property(x => x.NoticePeriod).HasColumnName("NoticePeriod");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.User).WithOne()
             .HasForeignKey<CandidateProfile>(x => x.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── All remaining tables follow same pattern ────────────
        // EF will auto-map remaining properties by convention
        // since column names match C# property names after snake_case mapping

        m.Entity<CandidateEducation>(e =>
        {
            e.ToTable("candidate_education");

            e.HasKey(x => x.EducationId);

            e.HasOne(x => x.CandidateProfile)
                .WithMany(x => x.Educations)
                .HasForeignKey(x => x.CandidateId);

            e.Property(x => x.YearDetails)
                .HasColumnName("year_details")
                .HasMaxLength(500);

            e.Property(x => x.IsAiVerified)
                .HasColumnName("is_ai_verified")
                .HasDefaultValue(false);

            e.Property(x => x.CertificateNumber)
                .HasColumnName("certificate_number")
                .HasMaxLength(100);
        });

        m.Entity<CandidateWorkHistory>(e => {
            e.ToTable("candidate_work_history");
            e.HasKey(x => x.WorkId);
            e.HasOne(x => x.CandidateProfile)
             .WithMany(x => x.WorkHistories)
             .HasForeignKey(x => x.CandidateId);
        });

        m.Entity<CandidateSkill>(e =>
        {
            e.ToTable("candidate_skills");

            e.HasKey(x => x.SkillId);

            e.HasOne(x => x.CandidateProfile)
                .WithMany(x => x.Skills)
                .HasForeignKey(x => x.CandidateId);

            e.Property(x => x.CanRead)
                .HasColumnName("can_read");

            e.Property(x => x.CanWrite)
                .HasColumnName("can_write");

            e.Property(x => x.CanSpeak)
                .HasColumnName("can_speak");
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

        m.Entity<PassportVerification>(e =>
        {
            e.ToTable("passport_verifications");

            e.HasKey(x => x.VerificationId);

            e.Property(x => x.VerificationId)
                .HasColumnName("verification_id");

            e.Property(x => x.CandidateId)
                .HasColumnName("candidate_id");

            e.Property(x => x.FrontImageUrl)
                .HasColumnName("front_image_url");

            e.Property(x => x.BackImageUrl)
                .HasColumnName("back_image_url");

            e.Property(x => x.AiExtractedName)
                .HasColumnName("ai_extracted_name");

            e.Property(x => x.AiExtractedDob)
                .HasColumnName("ai_extracted_dob");

            e.Property(x => x.AiConfidenceScore)
                .HasColumnName("ai_confidence_score");

            e.Property(x => x.AdminDecision)
                .HasColumnName("admin_decision");

            e.Property(x => x.RejectionReason)
                .HasColumnName("rejection_reason");

            e.Property(x => x.ReviewedBy)
                .HasColumnName("reviewed_by");

            e.Property(x => x.ReviewedAt)
                .HasColumnName("reviewed_at");

            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

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

        m.Entity<EmployerProfile>(e =>
        {
            e.ToTable("employer_profiles");

            e.HasKey(x => x.EmployerId);

            // PRIMARY KEY
            e.Property(x => x.EmployerId)
                .HasColumnName("employer_id");

            // FOREIGN KEY
            e.Property(x => x.UserId)
                .HasColumnName("user_id");

            // BASIC FIELDS
            e.Property(x => x.LegalName)
                .HasColumnName("legal_name");

            e.Property(x => x.TradeName)
                .HasColumnName("trade_name");

            e.Property(x => x.CompanyDisplayName)
                .HasColumnName("company_display_name");

            e.Property(x => x.CompanyDescription)
                .HasColumnName("company_description");

            e.Property(x => x.CompanyLogoUrl)
                .HasColumnName("company_logo_url");

            e.Property(x => x.CompanySize)
        .HasConversion(
            v =>
                v == CompanySize.Size_1_10 ? "1-10" :
                v == CompanySize.Size_11_50 ? "11-50" :
                v == CompanySize.Size_51_200 ? "51-200" :
                v == CompanySize.Size_201_500 ? "201-500" :
                v == CompanySize.Size_500_Plus ? "500+" :
                "1-10",

            v =>
                v == "1-10" ? CompanySize.Size_1_10 :
                v == "11-50" ? CompanySize.Size_11_50 :
                v == "51-200" ? CompanySize.Size_51_200 :
                v == "201-500" ? CompanySize.Size_201_500 :
                CompanySize.Size_500_Plus
        )
        .HasColumnName("company_size");

            e.Property(x => x.YearEstablished)
                .HasColumnName("year_established");

            e.Property(x => x.WebsiteUrl)
                .HasColumnName("website_url");

            e.Property(x => x.BusinessType)
                 .HasConversion<string>()
                 .HasColumnName("business_type");

            e.Property(x => x.IndustryType)
                .HasConversion<string>()
                .HasColumnName("industry_type");

            e.Property(x => x.GstRegistered)
                .HasColumnName("gst_registered");

            e.Property(x => x.Gstin)
                .HasColumnName("gstin");

            e.Property(x => x.Pan)
                .HasColumnName("pan");

            e.Property(x => x.Cin)
                .HasColumnName("cin");

            e.Property(x => x.AddressLine1)
                .HasColumnName("address_line1");

            e.Property(x => x.AddressLine2)
                .HasColumnName("address_line2");

            e.Property(x => x.City)
                .HasColumnName("city");

            e.Property(x => x.State)
                .HasColumnName("state");

            e.Property(x => x.Pincode)
                .HasColumnName("pincode");

            e.Property(x => x.Country)
                .HasColumnName("country");

            e.Property(x => x.ContactPhone)
                .HasColumnName("contact_phone");

            e.Property(x => x.ContactEmailPublic)
                .HasColumnName("contact_email_public");

            e.Property(x => x.ContactPersonName)
                .HasColumnName("contact_person_name");

            e.Property(x => x.Designation)
                .HasColumnName("designation");

            e.Property(x => x.AccountStatus)
                .HasColumnName("account_status");

            e.Property(x => x.CreatedAt)
                .HasColumnName("created_at");

            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            e.Property(x => x.GstinRegistrationDate)
                .HasColumnName("gstn_registration_date");

            e.Property(x => x.KarzaRequestId)
                .HasColumnName("karza_request_id");

            e.Property(x => x.OfficeAddress)
                .HasColumnName("office_address");

            e.Property(x => x.OperatingHours)
                .HasColumnName("operating_hours");

            e.Property(x => x.TrialExpiresAt)
                .HasColumnName("trial_expires_at");

            e.Property(x => x.SecurityDepositPaid)
                .HasColumnName("security_deposit_paid");

            e.Property(x => x.SecurityDepositStatus)
                .HasColumnName("security_deposit_status");

            e.Property(x => x.ProfileCompletionScore)
                .HasColumnName("profile_completion_score");


            e.Property(x => x.ConsentTimestamp)
                .HasColumnName("consent_timestamp");

            e.Property(x => x.CompanyHighlights)
     .HasColumnName("company_highlights")
     .HasColumnType("jsonb");
            // INDEX
            e.HasIndex(x => x.Gstin)
                .IsUnique();



            // RELATION
            e.HasOne(x => x.User)
                .WithOne()
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
            e.Property(x => x.BadgeType)
             .HasConversion<string>();
            e.Property(x => x.BadgeStatus)
            .HasConversion<string>();
        });

        m.Entity<EmployerSubUser>(e =>
        {
            e.ToTable("employer_sub_users");
            e.HasKey(x => x.SubUserId);

            e.Property(x => x.SubUserId).HasColumnName("SubUserId");
            e.Property(x => x.EmployerId).HasColumnName("EmployerId");
            e.Property(x => x.UserId).HasColumnName("UserId");
            e.Property(x => x.SubUserName).HasColumnName("SubUserName");
            e.Property(x => x.SubUserEmail).HasColumnName("SubUserEmail");
            e.Property(x => x.SubUserMobile).HasColumnName("SubUserMobile");
            e.Property(x => x.SubUserCountryCode).HasColumnName("SubUserCountryCode");
            e.Property(x => x.SubUserRole).HasColumnName("SubUserRole");
            e.Property(x => x.InviteToken).HasColumnName("InviteToken");
            e.Property(x => x.InviteExpiresAt).HasColumnName("InviteExpiresAt");
            e.Property(x => x.InviteAccepted).HasColumnName("InviteAccepted");
            e.Property(x => x.CanSearchCandidates).HasColumnName("CanSearchCandidates");
            e.Property(x => x.CanUnlockProfiles).HasColumnName("CanUnlockProfiles");
            e.Property(x => x.CanPostJobs).HasColumnName("CanPostJobs");
            e.Property(x => x.CanManageApplications).HasColumnName("CanManageApplications");
            e.Property(x => x.SubUserStatus).HasColumnName("SubUserStatus");
            e.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            e.Property(x => x.DeactivatedAt).HasColumnName("DeactivatedAt");

            e.HasOne(x => x.EmployerProfile)
                .WithMany(x => x.SubUsers)
                .HasForeignKey(x => x.EmployerId);

            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId);
        });

        m.Entity<EmployerNotificationSetting>(e =>
        {
            e.ToTable("employer_notification_settings");

            e.HasKey(x => x.NotifPrefId);

            e.Property(x => x.NotifPrefId)
                .HasColumnName("notif_pref_id");

            e.Property(x => x.EmployerId)
                .HasColumnName("employer_id");

            e.Property(x => x.PrefEmailEnabled)
                .HasColumnName("pref_email_enabled");

            e.Property(x => x.PrefPushEnabled)
                .HasColumnName("pref_push_enabled");

            e.Property(x => x.PrefApplicantNotify)
                .HasColumnName("pref_applicant_notify");

            e.Property(x => x.PrefCreditExpiryEmail)
                .HasColumnName("pref_credit_expiry_email");

            e.Property(x => x.PrefJobStatusUpdates)
                .HasColumnName("pref_job_status_updates");

            e.Property(x => x.PrefSystemMessages)
                .HasColumnName("pref_system_messages");

            e.Property(x => x.FcmToken)
                .HasColumnName("fcm_token");

            e.Property(x => x.SessionTimeoutMinutes)
                .HasColumnName("session_timeout_minutes");

            e.HasIndex(x => x.EmployerId)
                .IsUnique();

            e.HasOne(x => x.EmployerProfile)
                .WithOne(x => x.NotificationSetting)
                .HasForeignKey<EmployerNotificationSetting>(x => x.EmployerId);
        });

        m.Entity<RegistrationSessionDocument>()
           .HasOne(x => x.Session)
           .WithMany(x => x.Documents)
           .HasForeignKey(x => x.SessionId)
           .OnDelete(DeleteBehavior.Cascade);

        m.Entity<RegistrationSessionDocument>()
            .HasOne(x => x.DocumentType)
            .WithMany()
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);



        var stringListConverter = new ValueConverter<List<string>?, string?>(
         v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
          v => string.IsNullOrEmpty(v)
        ? new List<string>()
        : JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
);

        m.Entity<MembershipPlan>(e =>
        {
            e.ToTable("MembershipPlans");
            e.HasKey(x => x.PlanId);

            e.Property(x => x.PlanType)
                .HasConversion<string>();

            e.Property(x => x.Features)
                .HasConversion(stringListConverter);
        });

        m.Entity<JobPosting>(e =>
        {
            e.ToTable("job_postings");
            e.HasKey(x => x.JobId);

            e.Property(x => x.JobId)
             .HasColumnName("job_id");

            e.Property(x => x.EmployerId)
             .HasColumnName("employer_id");

            e.Property(x => x.PostedBySubUserId)
             .HasColumnName("posted_by_sub_user_id");

            e.Property(x => x.JobTitle)
             .HasColumnName("job_title");

            e.Property(x => x.JobDescription)
             .HasColumnName("job_description");

            e.Property(x => x.Role)
             .HasColumnName("role");

            e.Property(x => x.TradeCategory)
             .HasColumnName("trade_category");

            e.Property(x => x.SalaryMin)
             .HasColumnName("salary_min");

            e.Property(x => x.SalaryMax)
             .HasColumnName("salary_max");

            e.Property(x => x.JobStatus)
             .HasColumnName("job_status")
             .HasConversion<string>();

            e.Property(x => x.JobType)
                .HasColumnName("job_type")
                .HasConversion<string>();
            // ✅ string properties — no HasDefaultValue needed
            // defaults are set on the entity itself



            e.Property(x => x.Vacancies)
             .HasColumnName("vacancies");



            e.Property(x => x.AgeMin)
             .HasColumnName("age_min");

            e.Property(x => x.AgeMax)
             .HasColumnName("age_max");



            e.Property(x => x.EducationRequired)
             .HasColumnName("education_required");

            e.Property(x => x.LicenceDocsRequired)
             .HasColumnName("licence_docs_required");

            e.Property(x => x.LanguageRequired)
             .HasColumnName("language_required");

            e.Property(x => x.KeySkills)
  .HasColumnName("key_skills")
  .HasConversion(stringListConverter)
  .HasColumnType("jsonb");

            e.Property(x => x.DisabilityEligible)
             .HasColumnName("disability_eligible");


            e.Property(x => x.OnshoreCity)
             .HasColumnName("onshore_city");

            e.Property(x => x.OnshoreState)
             .HasColumnName("onshore_state");

            e.Property(x => x.OffshoreVesselName)
             .HasColumnName("offshore_vessel_name");

            e.Property(x => x.OffshoreRegion)
             .HasColumnName("offshore_region");

            e.Property(x => x.IsInternational)
             .HasColumnName("is_international");

            e.Property(x => x.PassportRequired)
             .HasColumnName("passport_required");

            e.Property(x => x.PassportValidityMonths)
             .HasColumnName("passport_validity_months");



            e.Property(x => x.ApplicationDeadline)
             .HasColumnName("application_deadline");

            e.Property(x => x.AppliedCount)
             .HasColumnName("applied_count");

            e.Property(x => x.PublishedAt)
             .HasColumnName("published_at");

            e.Property(x => x.CreatedAt)
             .HasColumnName("created_at");

            e.Property(x => x.UpdatedAt)
             .HasColumnName("updated_at");

            e.Property(x => x.CurrentStep)
             .HasColumnName("current_step");

            e.Property(x => x.LastCompletedStep)
             .HasColumnName("last_completed_step");

            e.Property(x => x.ScreeningQuestions)
   .HasColumnName("screening_questions")
   .HasConversion(stringListConverter)
   .HasColumnType("jsonb");

            e.Property(x => x.PublishingTags)
           .HasColumnName("publishing_tags")
           .HasConversion(stringListConverter)
           .HasColumnType("jsonb");

            // Relationships
            e.HasOne(x => x.EmployerProfile)
             .WithMany()
             .HasForeignKey(x => x.EmployerId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.PostedBySubUser)
             .WithMany()
             .HasForeignKey(x => x.PostedBySubUserId)
             .OnDelete(DeleteBehavior.SetNull);

            // Experience
            e.Property(x => x.ExperienceMinYears)
                .HasColumnName("experience_min_years");

            e.Property(x => x.ExperienceMaxYears)
                .HasColumnName("experience_max_years");

            // Address
            e.Property(x => x.WorkAddressLine)
                .HasColumnName("work_address_line");

            e.Property(x => x.OnshoreCountry)
                .HasColumnName("onshore_country");

            e.Property(x => x.OnshorePincode)
                .HasColumnName("onshore_pincode");

            e.Property(x => x.OffshoreCountry)
                .HasColumnName("offshore_country");

            // Enums
            e.Property(x => x.SalaryCurrency)
                .HasColumnName("salary_currency")
                .HasConversion<string>();

            e.Property(x => x.SalaryDisplayOption)
                .HasColumnName("salary_display_option")
                .HasConversion<string>();

            e.Property(x => x.GenderPreferred)
                .HasColumnName("gender_preferred")
                .HasConversion<string>();

            e.Property(x => x.LocationType)
                .HasColumnName("location_type")
                .HasConversion<string>();

            e.Property(x => x.CompanyVisibility)
                .HasColumnName("company_visibility")
                .HasConversion<string>();

            e.Property(x => x.EmploymentType)
                .HasColumnName("employment_type")
                .HasConversion<string>();

            e.Property(x => x.EmploymentMode)
                .HasColumnName("employment_mode")
                .HasConversion<string>();

            // Employment Details
            e.Property(x => x.DutyHoursPerDay)
                .HasColumnName("duty_hours_per_day");

            e.Property(x => x.PaidOvertime)
                .HasColumnName("paid_overtime");

            e.Property(x => x.Department)
                .HasColumnName("department");

            e.Property(x => x.KeyResponsibilities)
 .HasColumnName("key_responsibilities")
 .HasConversion(stringListConverter)
 .HasColumnType("jsonb");

            // Analytics
            e.Property(x => x.ViewCount)
                .HasColumnName("view_count");

            // Benefits
            e.Property(x => x.Benefits)
 .HasColumnName("benefits")
 .HasConversion(stringListConverter)
 .HasColumnType("jsonb");

            // Home Page
            e.Property(x => x.IsFeatured)
                .HasColumnName("is_featured");

            e.Property(x => x.IsUrgentHiring)
                .HasColumnName("is_urgent_hiring");

            e.Property(x => x.IsDeleted)
                .HasColumnName("is_deleted");

            e.Property(x => x.IsActive)
                .HasColumnName("is_active");

            // Search
            e.Property(x => x.Tags)
 .HasColumnName("tags")
 .HasConversion(stringListConverter)
 .HasColumnType("jsonb");

            e.Property(x => x.SearchKeywords)
                .HasColumnName("search_keywords");
        });

        m.Entity<JobApplication>(e =>
        {
            e.ToTable("job_applications");

            e.HasKey(x => x.ApplicationId);

            e.HasIndex(x => new { x.JobId, x.CandidateId })
                .IsUnique();

            e.Property(x => x.ApplicationStatus)
                .HasConversion<string>();

            e.Property(x => x.IsShortlisted)
                .HasColumnName("is_shortlisted");

            e.Property(x => x.ShortlistedAt)
                .HasColumnName("shortlisted_at");

            e.Property(x => x.InterviewScheduledAt)
                .HasColumnName("interview_scheduled_at");

            e.Property(x => x.RejectedAt)
                .HasColumnName("rejected_at");

            e.Property(x => x.ScreeningAnswers)
                .HasColumnName("screening_answers")
                .HasColumnType("jsonb");

            e.Property(x => x.MotivationMessage)
                .HasColumnName("motivation_message");

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

        m.Entity<SavedJob>(e =>
        {
            e.ToTable("saved_jobs");

            e.HasKey(x => x.SavedJobId);

            e.Property(x => x.SavedJobId)
                .HasColumnName("saved_job_id");

            e.Property(x => x.CandidateId)
                .HasColumnName("candidate_id");

            e.Property(x => x.JobId)
                .HasColumnName("job_id");

            e.Property(x => x.SavedAt)
                .HasColumnName("saved_at");

            e.HasIndex(x => new { x.CandidateId, x.JobId })
                .IsUnique();

            e.HasOne(x => x.CandidateProfile)
                .WithMany()
                .HasForeignKey(x => x.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.JobPosting)
                .WithMany()
                .HasForeignKey(x => x.JobId)
                .OnDelete(DeleteBehavior.Cascade);
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

        m.Entity<CreditWallet>(e =>
        {
            e.ToTable("credit_wallets");

            e.HasKey(x => x.Wallet_Id);

            e.Property(x => x.Wallet_Id)
                .HasColumnName("wallet_id");

            e.Property(x => x.EmployerId)
                .HasColumnName("employer_id");

            e.Property(x => x.CreditBalance)
                .HasColumnName("credit_balance");

            e.Property(x => x.PackageName)
                .HasColumnName("package_name");

            e.Property(x => x.PackExpiresAt)
                .HasColumnName("pack_expires_at");

            e.Property(x => x.SharedWallet)
                .HasColumnName("shared_wallet");

            e.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            e.HasOne(x => x.EmployerProfile)
                .WithOne(x => x.CreditWallet)
                .HasForeignKey<CreditWallet>(x => x.EmployerId);
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
            e.Property(x => x.TicketType)
                .HasConversion<string>();
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

        m.Entity<LegalDocument>(e => {
            e.ToTable("legal_documents");
            e.HasKey(x => x.DocumentId);
            e.HasIndex(x => x.Type).IsUnique();
            e.Property(x => x.Type).HasMaxLength(30).IsRequired();
            e.Property(x => x.Title).HasMaxLength(150).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasOne(x => x.UpdatedByAdmin)
             .WithMany()
             .HasForeignKey(x => x.UpdatedBy)
             .OnDelete(DeleteBehavior.SetNull);

            // Seed the two documents the Legal Pages admin screen edits so
            // the API has something to serve out of the box.
            e.HasData(
                new
                {
                    DocumentId = Guid.Parse("6f2a6c2e-6a3b-4a0a-8a2e-0a6f6a2c2a01"),
                    Type = "privacy",
                    Title = "Privacy Policy",
                    DraftContent = "<h2>Privacy Policy</h2><p>Write or paste your privacy policy here.</p>",
                    Status = "Draft",
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    DocumentId = Guid.Parse("6f2a6c2e-6a3b-4a0a-8a2e-0a6f6a2c2a02"),
                    Type = "terms",
                    Title = "Terms & Conditions",
                    DraftContent = "<h2>Terms &amp; Conditions</h2><p>Write or paste your terms of service here.</p>",
                    Status = "Draft",
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        });

        m.Entity<CountryVerificationConfig>(e => {
            e.ToTable("country_verification_config");
            e.HasKey(x => x.ConfigId);
            e.HasIndex(x => x.CountryCode).IsUnique();
            e.HasOne(x => x.UpdatedByAdmin)
             .WithMany()
             .HasForeignKey(x => x.ConfigUpdatedBy);
        });

        m.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.LogId);

            entity.Property(x => x.PerformedByName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.PerformedByRole)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Module)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.TargetEntityType)
                .HasMaxLength(100);

            entity.Property(x => x.TargetEntityName)
                .HasMaxLength(200);

            entity.Property(x => x.Severity)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(AuditSeverity.Info)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(1000);

            entity.Property(x => x.IpAddress)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.UserAgent)
                .HasMaxLength(500);

            entity.Property(x => x.OldValues)
                .HasColumnType("jsonb");

            entity.Property(x => x.NewValues)
                .HasColumnType("jsonb");

            entity.HasOne(x => x.PerformedByAdmin)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.PerformedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);
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

        m.Entity<EmployerPlanPurchase>(e =>
        {
            e.ToTable("EmployerPlanPurchase");

            e.HasKey(x => x.EmployerCreditPlanId);

            e.Property(x => x.EmployerCreditPlanId)
                .HasColumnName("EmployerCreditPlanId");

            e.Property(x => x.EmployerId)
                .HasColumnName("EmployerId");

            e.Property(x => x.PlanId)
                .HasColumnName("PlanId");

            e.Property(x => x.PlanName)
                .HasColumnName("PlanName");

            e.Property(x => x.Credits)
                .HasColumnName("Credits");

            e.Property(x => x.Price)
                .HasColumnName("Price");

            e.Property(x => x.AssignedAt)
                .HasColumnName("AssignedAt");

            e.Property(x => x.ExpiresAt)
                .HasColumnName("ExpiresAt");

            e.Property(x => x.IsActive)
                .HasColumnName("IsActive");

            e.Property(x => x.AssignedBy)
                .HasColumnName("AssignedBy");

            e.HasOne(x => x.Employer)
                .WithMany()
                .HasForeignKey(x => x.EmployerId)
                .HasPrincipalKey(x => x.EmployerId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.CreditPlan)
                .WithMany()
                .HasForeignKey(x => x.PlanId)
                .HasPrincipalKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<RegistrationSession>(e =>
        {
            e.ToTable("registration_sessions");
            e.HasKey(x => x.SessionId);

            e.Property(x => x.SessionId)
             .ValueGeneratedOnAdd();

            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.UpdatedAt).IsRequired();
            e.Property(x => x.ExpiresAt).IsRequired();
        });
    }
}