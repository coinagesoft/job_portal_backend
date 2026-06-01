using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initialBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registration_sessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionType = table.Column<string>(type: "text", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    LastCompletedStep = table.Column<int>(type: "integer", nullable: false),
                    GstRegistered = table.Column<bool>(type: "boolean", nullable: true),
                    IndustryType = table.Column<string>(type: "text", nullable: true),
                    RequiresSecurityDeposit = table.Column<bool>(type: "boolean", nullable: false),
                    LegalName = table.Column<string>(type: "text", nullable: true),
                    TradeName = table.Column<string>(type: "text", nullable: true),
                    CompanyDisplayName = table.Column<string>(type: "text", nullable: true),
                    BusinessType = table.Column<string>(type: "text", nullable: true),
                    CompanySize = table.Column<string>(type: "text", nullable: true),
                    Cin = table.Column<string>(type: "text", nullable: true),
                    Gstn = table.Column<string>(type: "text", nullable: true),
                    Pan = table.Column<string>(type: "text", nullable: true),
                    GstnRegistrationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Pincode = table.Column<string>(type: "text", nullable: true),
                    AddressLine1 = table.Column<string>(type: "text", nullable: true),
                    AddressLine2 = table.Column<string>(type: "text", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true),
                    CompanyLogoUrl = table.Column<string>(type: "text", nullable: true),
                    ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    Designation = table.Column<string>(type: "text", nullable: true),
                    ContactPersonEmail = table.Column<string>(type: "text", nullable: true),
                    CompanyEmail = table.Column<string>(type: "text", nullable: true),
                    MobileNumber = table.Column<string>(type: "text", nullable: true),
                    CountryCode = table.Column<string>(type: "text", nullable: true),
                    CompanyDescription = table.Column<string>(type: "text", nullable: true),
                    MobileVerified = table.Column<bool>(type: "boolean", nullable: false),
                    PoeLicenceS3Url = table.Column<string>(type: "text", nullable: true),
                    RpslLicenceS3Url = table.Column<string>(type: "text", nullable: true),
                    LicencesSkipped = table.Column<bool>(type: "boolean", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_sessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_type = table.Column<string>(type: "text", nullable: false),
                    mobile_number = table.Column<string>(type: "text", nullable: false),
                    country_code = table.Column<string>(type: "text", nullable: false, defaultValue: "+91"),
                    email = table.Column<string>(type: "text", nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    account_status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    kyc_status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    payment_status = table.Column<string>(type: "text", nullable: false, defaultValue: "Unpaid"),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    suspension_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "admin_users",
                columns: table => new
                {
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_identifier = table.Column<string>(type: "text", nullable: false),
                    admin_role = table.Column<string>(type: "text", nullable: false),
                    permissions = table.Column<string>(type: "text", nullable: true),
                    failed_attempts = table.Column<short>(type: "smallint", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_users", x => x.admin_id);
                    table.ForeignKey(
                        name: "FK_admin_users_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "candidate_profiles",
                columns: table => new
                {
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    profile_photo_url = table.Column<string>(type: "text", nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    gender = table.Column<string>(type: "text", nullable: true),
                    nationality = table.Column<string>(type: "text", nullable: true),
                    current_city = table.Column<string>(type: "text", nullable: true),
                    current_state = table.Column<string>(type: "text", nullable: true),
                    preferred_work_location = table.Column<string>(type: "text", nullable: true),
                    preferred_salary = table.Column<int>(type: "integer", nullable: true),
                    availability_status = table.Column<string>(type: "text", nullable: false),
                    availability_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    disability_status = table.Column<bool>(type: "boolean", nullable: false),
                    disability_note = table.Column<string>(type: "text", nullable: true),
                    primary_trade = table.Column<string>(type: "text", nullable: true),
                    total_experience_years = table.Column<int>(type: "integer", nullable: false),
                    iti_certified = table.Column<bool>(type: "boolean", nullable: false),
                    iti_trade = table.Column<string>(type: "text", nullable: true),
                    iti_marks = table.Column<string>(type: "text", nullable: true),
                    iti_college = table.Column<string>(type: "text", nullable: true),
                    band = table.Column<string>(type: "text", nullable: true),
                    ai_match_score = table.Column<byte>(type: "smallint", nullable: true),
                    profile_status = table.Column<string>(type: "text", nullable: false),
                    profile_completion_pct = table.Column<byte>(type: "smallint", nullable: false),
                    reengagement_response = table.Column<string>(type: "text", nullable: true),
                    last_applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fcm_token = table.Column<string>(type: "text", nullable: true),
                    admin_notes = table.Column<string>(type: "text", nullable: true),
                    credit_balance = table.Column<int>(type: "integer", nullable: false),
                    welcome_email_sent = table.Column<bool>(type: "boolean", nullable: false),
                    newsletter_opt_in = table.Column<bool>(type: "boolean", nullable: false),
                    temp_password_flag = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_profiles", x => x.candidate_id);
                    table.ForeignKey(
                        name: "FK_candidate_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consent_logs",
                columns: table => new
                {
                    ConsentLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentType = table.Column<string>(type: "text", nullable: false),
                    ConsentGiven = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataResidency = table.Column<string>(type: "text", nullable: false),
                    NationalIdStorage = table.Column<string>(type: "text", nullable: false),
                    ConsentVersion = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_logs", x => x.ConsentLogId);
                    table.ForeignKey(
                        name: "FK_consent_logs_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employer_profiles",
                columns: table => new
                {
                    employer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "text", nullable: false),
                    trade_name = table.Column<string>(type: "text", nullable: true),
                    company_display_name = table.Column<string>(type: "text", nullable: false),
                    company_description = table.Column<string>(type: "text", nullable: true),
                    company_logo_url = table.Column<string>(type: "text", nullable: true),
                    company_size = table.Column<string>(type: "text", nullable: true),
                    year_established = table.Column<short>(type: "smallint", nullable: true),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    business_type = table.Column<string>(type: "text", nullable: false),
                    industry_type = table.Column<string>(type: "text", nullable: false),
                    gst_registered = table.Column<bool>(type: "boolean", nullable: false),
                    gstn = table.Column<string>(type: "text", nullable: true),
                    pan = table.Column<string>(type: "text", nullable: true),
                    cin = table.Column<string>(type: "text", nullable: true),
                    gstn_registration_date = table.Column<DateOnly>(type: "date", nullable: true),
                    karza_request_id = table.Column<string>(type: "text", nullable: true),
                    address_line1 = table.Column<string>(type: "text", nullable: false),
                    address_line2 = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: true),
                    pincode = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    office_address = table.Column<string>(type: "text", nullable: true),
                    contact_phone = table.Column<string>(type: "text", nullable: false),
                    contact_email_public = table.Column<string>(type: "text", nullable: true),
                    contact_person_name = table.Column<string>(type: "text", nullable: false),
                    designation = table.Column<string>(type: "text", nullable: false),
                    operating_hours = table.Column<string>(type: "text", nullable: true),
                    account_status = table.Column<int>(type: "integer", nullable: false),
                    trial_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    security_deposit_paid = table.Column<bool>(type: "boolean", nullable: false),
                    security_deposit_status = table.Column<string>(type: "text", nullable: true),
                    profile_completion_score = table.Column<byte>(type: "smallint", nullable: false),
                    poe_licence_s3_url = table.Column<string>(type: "text", nullable: true),
                    poe_licence_number = table.Column<string>(type: "text", nullable: true),
                    poe_company_name = table.Column<string>(type: "text", nullable: true),
                    poe_validity_date = table.Column<DateOnly>(type: "date", nullable: true),
                    poe_expired_flag = table.Column<bool>(type: "boolean", nullable: false),
                    rpsl_licence_s3_url = table.Column<string>(type: "text", nullable: true),
                    rpsl_licence_number = table.Column<string>(type: "text", nullable: true),
                    rpsl_company_name = table.Column<string>(type: "text", nullable: true),
                    rpsl_validity_date = table.Column<DateOnly>(type: "date", nullable: true),
                    rpsl_expired_flag = table.Column<bool>(type: "boolean", nullable: false),
                    business_reg_doc_url = table.Column<string>(type: "text", nullable: true),
                    consent_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tags = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employer_profiles", x => x.employer_id);
                    table.ForeignKey(
                        name: "FK_employer_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<string>(type: "text", nullable: false),
                    Channel = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceType = table.Column<string>(type: "text", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_notifications_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "otp_verifications",
                columns: table => new
                {
                    otp_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mobile_number = table.Column<string>(type: "text", nullable: false),
                    country_code = table.Column<string>(type: "text", nullable: false),
                    otp_code = table.Column<string>(type: "varchar(255)", nullable: false),
                    otp_sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    otp_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resend_cooldown_sec = table.Column<int>(type: "integer", nullable: false),
                    otp_attempts = table.Column<byte>(type: "smallint", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_verifications", x => x.otp_id);
                    table.ForeignKey(
                        name: "FK_otp_verifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "admin_sessions",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_token = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    trusted_device = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_sessions", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_admin_sessions_admin_users_admin_id",
                        column: x => x.admin_id,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    LogId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "text", nullable: false),
                    PerformedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedByName = table.Column<string>(type: "text", nullable: false),
                    TargetEntityType = table.Column<string>(type: "text", nullable: false),
                    TargetEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionDetail = table.Column<string>(type: "text", nullable: false),
                    ChangeReason = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_audit_logs_admin_users_PerformedBy",
                        column: x => x.PerformedBy,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "country_verification_config",
                columns: table => new
                {
                    ConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: false),
                    AcceptedCandidateIdTypes = table.Column<string>(type: "text", nullable: false),
                    AcceptedEmployerDocTypes = table.Column<string>(type: "text", nullable: false),
                    PrimaryBusinessVerify = table.Column<string>(type: "text", nullable: false),
                    RequireSecurityDeposit = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigUpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_verification_config", x => x.ConfigId);
                    table.ForeignKey(
                        name: "FK_country_verification_config_admin_users_ConfigUpdatedBy",
                        column: x => x.ConfigUpdatedBy,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "disputes",
                columns: table => new
                {
                    DisputeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaisedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DisputeType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignedTo = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_disputes", x => x.DisputeId);
                    table.ForeignKey(
                        name: "FK_disputes_admin_users_AssignedTo",
                        column: x => x.AssignedTo,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_disputes_users_RaisedBy",
                        column: x => x.RaisedBy,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "platform_config",
                columns: table => new
                {
                    ConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReengagementIntervalDays = table.Column<byte>(type: "smallint", nullable: false),
                    ReengagementChannel = table.Column<string>(type: "text", nullable: false),
                    WhatsappTemplateId = table.Column<string>(type: "text", nullable: false),
                    FcmFallbackEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CvUnlockValidityDays = table.Column<byte>(type: "smallint", nullable: false),
                    WatermarkTemplate = table.Column<string>(type: "text", nullable: false),
                    CreditExpiryAlertDays = table.Column<string>(type: "text", nullable: false),
                    AlertChannels = table.Column<string>(type: "text", nullable: false),
                    TrialDurationDays = table.Column<byte>(type: "smallint", nullable: false),
                    TrialFreeCredits = table.Column<byte>(type: "smallint", nullable: false),
                    TrialCvDownloadAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    OneTrialPerGstDomain = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_config", x => x.ConfigId);
                    table.ForeignKey(
                        name: "FK_platform_config_admin_users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "support_tickets",
                columns: table => new
                {
                    TicketId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaisedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketType = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    AssignedTo = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_tickets", x => x.TicketId);
                    table.ForeignKey(
                        name: "FK_support_tickets_admin_users_AssignedTo",
                        column: x => x.AssignedTo,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_support_tickets_users_RaisedBy",
                        column: x => x.RaisedBy,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_cv",
                columns: table => new
                {
                    CvId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CvFileUrl = table.Column<string>(type: "text", nullable: true),
                    CvPdfUrl = table.Column<string>(type: "text", nullable: true),
                    CvS3Url = table.Column<string>(type: "text", nullable: true),
                    AffindaJobId = table.Column<string>(type: "text", nullable: true),
                    ParsedName = table.Column<string>(type: "text", nullable: true),
                    ParsedPhone = table.Column<string>(type: "text", nullable: true),
                    ParsedEmail = table.Column<string>(type: "text", nullable: true),
                    ParsedTrade = table.Column<string>(type: "text", nullable: true),
                    ParsedExperienceYrs = table.Column<int>(type: "integer", nullable: true),
                    ParsedSkills = table.Column<string>(type: "text", nullable: true),
                    AiConfidenceScore = table.Column<decimal>(type: "numeric", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_cv", x => x.CvId);
                    table.ForeignKey(
                        name: "FK_candidate_cv_candidate_profiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_education",
                columns: table => new
                {
                    EducationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EducationLevel = table.Column<string>(type: "text", nullable: false),
                    InstituteName = table.Column<string>(type: "text", nullable: true),
                    MarksPercentage = table.Column<string>(type: "text", nullable: true),
                    PassoutYear = table.Column<short>(type: "smallint", nullable: true),
                    CertificateUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_education", x => x.EducationId);
                    table.ForeignKey(
                        name: "FK_candidate_education_candidate_profiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_skills",
                columns: table => new
                {
                    SkillId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "text", nullable: false),
                    SkillType = table.Column<string>(type: "text", nullable: false),
                    YearsOfExperience = table.Column<byte>(type: "smallint", nullable: true),
                    SkillRole = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_skills", x => x.SkillId);
                    table.ForeignKey(
                        name: "FK_candidate_skills_candidate_profiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_work_history",
                columns: table => new
                {
                    WorkId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    JobTitle = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    JobDescription = table.Column<string>(type: "text", nullable: true),
                    WorkLocation = table.Column<string>(type: "text", nullable: true),
                    IsOffshore = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_work_history", x => x.WorkId);
                    table.ForeignKey(
                        name: "FK_candidate_work_history_candidate_profiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "iti_certificate_reviews",
                columns: table => new
                {
                    ItiReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItiCertImageUrl = table.Column<string>(type: "text", nullable: false),
                    AiExtractedTrade = table.Column<string>(type: "text", nullable: true),
                    AiExtractedInstitute = table.Column<string>(type: "text", nullable: true),
                    AiExtractedYear = table.Column<short>(type: "smallint", nullable: true),
                    AiExtractedCertNo = table.Column<string>(type: "text", nullable: true),
                    AiConfidenceScore = table.Column<decimal>(type: "numeric", nullable: true),
                    AdminNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_iti_certificate_reviews", x => x.ItiReviewId);
                    table.ForeignKey(
                        name: "FK_iti_certificate_reviews_candidate_profiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "kyc_verifications",
                columns: table => new
                {
                    VerificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdType = table.Column<string>(type: "text", nullable: false),
                    IdFrontImageUrl = table.Column<string>(type: "text", nullable: false),
                    IdBackImageUrl = table.Column<string>(type: "text", nullable: true),
                    AiExtractedName = table.Column<string>(type: "text", nullable: true),
                    AiExtractedDob = table.Column<DateOnly>(type: "date", nullable: true),
                    AiExtractedAddress = table.Column<string>(type: "text", nullable: true),
                    AiConfidenceScore = table.Column<decimal>(type: "numeric", nullable: true),
                    IdHash = table.Column<string>(type: "text", nullable: false),
                    OcrConfidence = table.Column<decimal>(type: "numeric", nullable: true),
                    AdminDecision = table.Column<string>(type: "text", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kyc_verifications", x => x.VerificationId);
                    table.ForeignKey(
                        name: "FK_kyc_verifications_admin_users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_kyc_verifications_candidate_profiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "passport_verifications",
                columns: table => new
                {
                    PassportVerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassportImageUrl = table.Column<string>(type: "text", nullable: false),
                    AiExtractedPassportNo = table.Column<string>(type: "text", nullable: true),
                    AiExtractedNationality = table.Column<string>(type: "text", nullable: true),
                    AiExtractedExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AiExtractedFullName = table.Column<string>(type: "text", nullable: true),
                    ExpiryAutoFlagged = table.Column<bool>(type: "boolean", nullable: false),
                    AiConfidenceScore = table.Column<decimal>(type: "numeric", nullable: true),
                    AdminDecision = table.Column<string>(type: "text", nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passport_verifications", x => x.PassportVerId);
                    table.ForeignKey(
                        name: "FK_passport_verifications_admin_users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_passport_verifications_candidate_profiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_unlocks",
                columns: table => new
                {
                    UnlockId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnlockRequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditsDeducted = table.Column<byte>(type: "smallint", nullable: false),
                    UnlockTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UnlockExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WalletBalanceBefore = table.Column<int>(type: "integer", nullable: false),
                    WalletBalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    UnlockStatus = table.Column<string>(type: "text", nullable: false),
                    WatermarkedCvUrl = table.Column<string>(type: "text", nullable: true),
                    CvWatermarkEmployerId = table.Column<string>(type: "text", nullable: true),
                    EmployerProfileEmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateProfileCandidateId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_unlocks", x => x.UnlockId);
                    table.ForeignKey(
                        name: "FK_candidate_unlocks_candidate_profiles_CandidateProfileCandid~",
                        column: x => x.CandidateProfileCandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_candidate_unlocks_employer_profiles_EmployerProfileEmployer~",
                        column: x => x.EmployerProfileEmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credit_wallets",
                columns: table => new
                {
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credit_balance = table.Column<int>(type: "integer", nullable: false),
                    package_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pack_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    shared_wallet = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_wallets", x => x.wallet_id);
                    table.ForeignKey(
                        name: "FK_credit_wallets_employer_profiles_employer_id",
                        column: x => x.employer_id,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employer_badges",
                columns: table => new
                {
                    BadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeType = table.Column<string>(type: "text", nullable: false),
                    BadgeStatus = table.Column<string>(type: "text", nullable: false),
                    BadgeGstVerified = table.Column<bool>(type: "boolean", nullable: false),
                    BadgePanVerified = table.Column<bool>(type: "boolean", nullable: false),
                    BadgePoeLicensed = table.Column<bool>(type: "boolean", nullable: false),
                    BadgeRpslLicensed = table.Column<bool>(type: "boolean", nullable: false),
                    BadgeBlueTick = table.Column<bool>(type: "boolean", nullable: false),
                    BlueTickEligible = table.Column<bool>(type: "boolean", nullable: false),
                    BadgeRevocationReason = table.Column<string>(type: "text", nullable: true),
                    IssuedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    BadgeIssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BadgeRevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employer_badges", x => x.BadgeId);
                    table.ForeignKey(
                        name: "FK_employer_badges_admin_users_IssuedBy",
                        column: x => x.IssuedBy,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employer_badges_employer_profiles_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employer_notification_settings",
                columns: table => new
                {
                    notif_pref_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pref_email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pref_push_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    pref_applicant_notify = table.Column<bool>(type: "boolean", nullable: false),
                    pref_credit_expiry_email = table.Column<bool>(type: "boolean", nullable: false),
                    pref_availability_push = table.Column<bool>(type: "boolean", nullable: false),
                    fcm_token = table.Column<string>(type: "text", nullable: true),
                    session_timeout_minutes = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employer_notification_settings", x => x.notif_pref_id);
                    table.ForeignKey(
                        name: "FK_employer_notification_settings_employer_profiles_employer_id",
                        column: x => x.employer_id,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employer_sub_users",
                columns: table => new
                {
                    SubUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubUserName = table.Column<string>(type: "text", nullable: false),
                    SubUserEmail = table.Column<string>(type: "text", nullable: false),
                    SubUserRole = table.Column<string>(type: "text", nullable: false),
                    InviteToken = table.Column<Guid>(type: "uuid", nullable: true),
                    InviteExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InviteAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    CanSearchCandidates = table.Column<bool>(type: "boolean", nullable: false),
                    CanUnlockProfiles = table.Column<bool>(type: "boolean", nullable: false),
                    CanPostJobs = table.Column<bool>(type: "boolean", nullable: false),
                    CanManageApplications = table.Column<bool>(type: "boolean", nullable: false),
                    SubUserStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employer_sub_users", x => x.SubUserId);
                    table.ForeignKey(
                        name: "FK_employer_sub_users_employer_profiles_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employer_sub_users_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionType = table.Column<string>(type: "text", nullable: false),
                    PackType = table.Column<string>(type: "text", nullable: true),
                    CreditQuantity = table.Column<int>(type: "integer", nullable: true),
                    ValidityMonths = table.Column<byte>(type: "smallint", nullable: true),
                    AmountPaise = table.Column<int>(type: "integer", nullable: false),
                    GstAmountPaise = table.Column<int>(type: "integer", nullable: false),
                    TotalAmountPaise = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: true),
                    RazorpayOrderId = table.Column<string>(type: "text", nullable: true),
                    RazorpayPaymentId = table.Column<string>(type: "text", nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "text", nullable: true),
                    GatewayRefundId = table.Column<string>(type: "text", nullable: true),
                    PaymentStatus = table.Column<string>(type: "text", nullable: false),
                    OriginalTxnId = table.Column<Guid>(type: "uuid", nullable: true),
                    RefundReason = table.Column<string>(type: "text", nullable: true),
                    RefundProcessedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    InvoiceUrl = table.Column<string>(type: "text", nullable: true),
                    CreditsAddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmployerProfileEmployerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CandidateProfileCandidateId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_payment_transactions_admin_users_RefundProcessedBy",
                        column: x => x.RefundProcessedBy,
                        principalTable: "admin_users",
                        principalColumn: "admin_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_payment_transactions_candidate_profiles_CandidateProfileCan~",
                        column: x => x.CandidateProfileCandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id");
                    table.ForeignKey(
                        name: "FK_payment_transactions_employer_profiles_EmployerProfileEmplo~",
                        column: x => x.EmployerProfileEmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id");
                    table.ForeignKey(
                        name: "FK_payment_transactions_payment_transactions_OriginalTxnId",
                        column: x => x.OriginalTxnId,
                        principalTable: "payment_transactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_transactions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_searches",
                columns: table => new
                {
                    SavedSearchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedSearchName = table.Column<string>(type: "text", nullable: false),
                    SearchFilters = table.Column<string>(type: "text", nullable: false),
                    AlertEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmployerProfileEmployerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_searches", x => x.SavedSearchId);
                    table.ForeignKey(
                        name: "FK_saved_searches_employer_profiles_EmployerProfileEmployerId",
                        column: x => x.EmployerProfileEmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_postings",
                columns: table => new
                {
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    employer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    posted_by_sub_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    job_title = table.Column<string>(type: "text", nullable: false),
                    job_description = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: true),
                    trade_category = table.Column<string>(type: "text", nullable: false),
                    salary_min = table.Column<int>(type: "integer", nullable: false),
                    salary_max = table.Column<int>(type: "integer", nullable: false),
                    salary_currency = table.Column<string>(type: "text", nullable: false),
                    salary_display_option = table.Column<string>(type: "text", nullable: false),
                    vacancies = table.Column<short>(type: "smallint", nullable: false),
                    experience_required_years = table.Column<byte>(type: "smallint", nullable: false),
                    age_min = table.Column<byte>(type: "smallint", nullable: true),
                    age_max = table.Column<byte>(type: "smallint", nullable: true),
                    gender_preferred = table.Column<string>(type: "text", nullable: false),
                    education_required = table.Column<string>(type: "text", nullable: true),
                    licence_docs_required = table.Column<string>(type: "text", nullable: true),
                    language_required = table.Column<string>(type: "text", nullable: true),
                    key_skills = table.Column<string>(type: "json", nullable: true),
                    disability_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    location_type = table.Column<string>(type: "text", nullable: false),
                    onshore_city = table.Column<string>(type: "text", nullable: true),
                    onshore_state = table.Column<string>(type: "text", nullable: true),
                    offshore_vessel_name = table.Column<string>(type: "text", nullable: true),
                    offshore_region = table.Column<string>(type: "text", nullable: true),
                    is_international = table.Column<bool>(type: "boolean", nullable: false),
                    passport_required = table.Column<bool>(type: "boolean", nullable: false),
                    passport_validity_months = table.Column<byte>(type: "smallint", nullable: true),
                    company_visibility = table.Column<string>(type: "text", nullable: false),
                    application_deadline = table.Column<DateOnly>(type: "date", nullable: false),
                    applied_count = table.Column<int>(type: "integer", nullable: false),
                    job_status = table.Column<string>(type: "text", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current_step = table.Column<int>(type: "integer", nullable: false),
                    last_completed_step = table.Column<int>(type: "integer", nullable: false),
                    screening_questions = table.Column<string>(type: "json", nullable: true),
                    publishing_tags = table.Column<string>(type: "json", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_postings", x => x.job_id);
                    table.ForeignKey(
                        name: "FK_job_postings_employer_profiles_employer_id",
                        column: x => x.employer_id,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_postings_employer_sub_users_posted_by_sub_user_id",
                        column: x => x.posted_by_sub_user_id,
                        principalTable: "employer_sub_users",
                        principalColumn: "SubUserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceAmount = table.Column<int>(type: "integer", nullable: false),
                    InvoiceGst = table.Column<int>(type: "integer", nullable: false),
                    InvoiceTotal = table.Column<int>(type: "integer", nullable: false),
                    InvoiceS3Url = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentTransactionTransactionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.InvoiceId);
                    table.ForeignKey(
                        name: "FK_invoices_payment_transactions_PaymentTransactionTransaction~",
                        column: x => x.PaymentTransactionTransactionId,
                        principalTable: "payment_transactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoices_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "security_deposits",
                columns: table => new
                {
                    DepositId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountPaise = table.Column<int>(type: "integer", nullable: false),
                    DepositStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EmployerProfileEmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentTransactionTransactionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_security_deposits", x => x.DepositId);
                    table.ForeignKey(
                        name: "FK_security_deposits_employer_profiles_EmployerProfileEmployer~",
                        column: x => x.EmployerProfileEmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_security_deposits_payment_transactions_PaymentTransactionTr~",
                        column: x => x.PaymentTransactionTransactionId,
                        principalTable: "payment_transactions",
                        principalColumn: "TransactionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "job_applications",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApplicationStatus = table.Column<string>(type: "text", nullable: false),
                    StatusUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StatusChangedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmployerInternalNote = table.Column<string>(type: "text", nullable: true),
                    RejectionAutoNotify = table.Column<bool>(type: "boolean", nullable: false),
                    WithdrawalAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    PassportGatePassed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_applications", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK_job_applications_candidate_profiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_applications_employer_profiles_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_job_applications_job_postings_JobId",
                        column: x => x.JobId,
                        principalTable: "job_postings",
                        principalColumn: "job_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_jobs",
                columns: table => new
                {
                    SavedJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CandidateProfileCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobPostingJobId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_jobs", x => x.SavedJobId);
                    table.ForeignKey(
                        name: "FK_saved_jobs_candidate_profiles_CandidateProfileCandidateId",
                        column: x => x.CandidateProfileCandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_saved_jobs_job_postings_JobPostingJobId",
                        column: x => x.JobPostingJobId,
                        principalTable: "job_postings",
                        principalColumn: "job_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_sessions_admin_id",
                table: "admin_sessions",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_admin_identifier",
                table: "admin_users",
                column: "admin_identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_admin_users_user_id",
                table: "admin_users",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_PerformedBy",
                table: "audit_logs",
                column: "PerformedBy");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_cv_CandidateId",
                table: "candidate_cv",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_education_CandidateId",
                table: "candidate_education",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profiles_user_id",
                table: "candidate_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidate_skills_CandidateId",
                table: "candidate_skills",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_unlocks_CandidateProfileCandidateId",
                table: "candidate_unlocks",
                column: "CandidateProfileCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_unlocks_EmployerId_CandidateId",
                table: "candidate_unlocks",
                columns: new[] { "EmployerId", "CandidateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidate_unlocks_EmployerProfileEmployerId",
                table: "candidate_unlocks",
                column: "EmployerProfileEmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_work_history_CandidateId",
                table: "candidate_work_history",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_consent_logs_UserId",
                table: "consent_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_country_verification_config_ConfigUpdatedBy",
                table: "country_verification_config",
                column: "ConfigUpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_country_verification_config_CountryCode",
                table: "country_verification_config",
                column: "CountryCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_wallets_employer_id",
                table: "credit_wallets",
                column: "employer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_disputes_AssignedTo",
                table: "disputes",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_disputes_RaisedBy",
                table: "disputes",
                column: "RaisedBy");

            migrationBuilder.CreateIndex(
                name: "IX_employer_badges_EmployerId",
                table: "employer_badges",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_employer_badges_IssuedBy",
                table: "employer_badges",
                column: "IssuedBy");

            migrationBuilder.CreateIndex(
                name: "IX_employer_notification_settings_employer_id",
                table: "employer_notification_settings",
                column: "employer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employer_profiles_gstn",
                table: "employer_profiles",
                column: "gstn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employer_profiles_user_id",
                table: "employer_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employer_sub_users_EmployerId",
                table: "employer_sub_users",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_employer_sub_users_UserId",
                table: "employer_sub_users",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_InvoiceNumber",
                table: "invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_PaymentTransactionTransactionId",
                table: "invoices",
                column: "PaymentTransactionTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_UserId",
                table: "invoices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_iti_certificate_reviews_CandidateId",
                table: "iti_certificate_reviews",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_job_applications_CandidateId",
                table: "job_applications",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_job_applications_EmployerId",
                table: "job_applications",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_job_applications_JobId_CandidateId",
                table: "job_applications",
                columns: new[] { "JobId", "CandidateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_employer_id",
                table: "job_postings",
                column: "employer_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_postings_posted_by_sub_user_id",
                table: "job_postings",
                column: "posted_by_sub_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_kyc_verifications_CandidateId",
                table: "kyc_verifications",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_kyc_verifications_ReviewedBy",
                table: "kyc_verifications",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_UserId",
                table: "notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_otp_verifications_user_id",
                table: "otp_verifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_passport_verifications_CandidateId",
                table: "passport_verifications",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_passport_verifications_ReviewedBy",
                table: "passport_verifications",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_CandidateProfileCandidateId",
                table: "payment_transactions",
                column: "CandidateProfileCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_EmployerProfileEmployerId",
                table: "payment_transactions",
                column: "EmployerProfileEmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_OriginalTxnId",
                table: "payment_transactions",
                column: "OriginalTxnId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_RefundProcessedBy",
                table: "payment_transactions",
                column: "RefundProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_UserId",
                table: "payment_transactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_platform_config_UpdatedBy",
                table: "platform_config",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_saved_jobs_CandidateId_JobId",
                table: "saved_jobs",
                columns: new[] { "CandidateId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_jobs_CandidateProfileCandidateId",
                table: "saved_jobs",
                column: "CandidateProfileCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_jobs_JobPostingJobId",
                table: "saved_jobs",
                column: "JobPostingJobId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_searches_EmployerProfileEmployerId",
                table: "saved_searches",
                column: "EmployerProfileEmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_security_deposits_EmployerId",
                table: "security_deposits",
                column: "EmployerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_security_deposits_EmployerProfileEmployerId",
                table: "security_deposits",
                column: "EmployerProfileEmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_security_deposits_PaymentTransactionTransactionId",
                table: "security_deposits",
                column: "PaymentTransactionTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_AssignedTo",
                table: "support_tickets",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_support_tickets_RaisedBy",
                table: "support_tickets",
                column: "RaisedBy");

            migrationBuilder.CreateIndex(
                name: "uq_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_users_mobile",
                table: "users",
                column: "mobile_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_sessions");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "candidate_cv");

            migrationBuilder.DropTable(
                name: "candidate_education");

            migrationBuilder.DropTable(
                name: "candidate_skills");

            migrationBuilder.DropTable(
                name: "candidate_unlocks");

            migrationBuilder.DropTable(
                name: "candidate_work_history");

            migrationBuilder.DropTable(
                name: "consent_logs");

            migrationBuilder.DropTable(
                name: "country_verification_config");

            migrationBuilder.DropTable(
                name: "credit_wallets");

            migrationBuilder.DropTable(
                name: "disputes");

            migrationBuilder.DropTable(
                name: "employer_badges");

            migrationBuilder.DropTable(
                name: "employer_notification_settings");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "iti_certificate_reviews");

            migrationBuilder.DropTable(
                name: "job_applications");

            migrationBuilder.DropTable(
                name: "kyc_verifications");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "otp_verifications");

            migrationBuilder.DropTable(
                name: "passport_verifications");

            migrationBuilder.DropTable(
                name: "platform_config");

            migrationBuilder.DropTable(
                name: "registration_sessions");

            migrationBuilder.DropTable(
                name: "saved_jobs");

            migrationBuilder.DropTable(
                name: "saved_searches");

            migrationBuilder.DropTable(
                name: "security_deposits");

            migrationBuilder.DropTable(
                name: "support_tickets");

            migrationBuilder.DropTable(
                name: "job_postings");

            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.DropTable(
                name: "employer_sub_users");

            migrationBuilder.DropTable(
                name: "admin_users");

            migrationBuilder.DropTable(
                name: "candidate_profiles");

            migrationBuilder.DropTable(
                name: "employer_profiles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
