using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class adminEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_admin_sessions_admin_users_admin_id",
                table: "admin_sessions");

            migrationBuilder.DropForeignKey(
                name: "FK_admin_users_users_user_id",
                table: "admin_users");

            migrationBuilder.DropForeignKey(
                name: "FK_audit_logs_admin_users_PerformedBy",
                table: "audit_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_country_verification_config_admin_users_ConfigUpdatedBy",
                table: "country_verification_config");

            migrationBuilder.DropForeignKey(
                name: "FK_disputes_admin_users_AssignedTo",
                table: "disputes");

            migrationBuilder.DropForeignKey(
                name: "FK_employer_badges_admin_users_IssuedBy",
                table: "employer_badges");

            migrationBuilder.DropForeignKey(
                name: "FK_kyc_verifications_admin_users_ReviewedBy",
                table: "kyc_verifications");

       

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_admin_users_RefundProcessedBy",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_platform_config_admin_users_UpdatedBy",
                table: "platform_config");

            migrationBuilder.DropForeignKey(
                name: "FK_support_tickets_admin_users_AssignedTo",
                table: "support_tickets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_admin_users",
                table: "admin_users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_admin_sessions",
                table: "admin_sessions");

            migrationBuilder.DropColumn(
                name: "ActionDetail",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "ChangeReason",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "admin_role",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "permissions",
                table: "admin_users");

            migrationBuilder.DropColumn(
                name: "session_token",
                table: "admin_sessions");

            migrationBuilder.RenameTable(
                name: "audit_logs",
                newName: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "admin_users",
                newName: "AdminUsers");

            migrationBuilder.RenameTable(
                name: "admin_sessions",
                newName: "AdminSessions");

            migrationBuilder.RenameColumn(
                name: "PerformedBy",
                table: "AuditLogs",
                newName: "PerformedByAdminId");

            migrationBuilder.RenameIndex(
                name: "IX_audit_logs_PerformedBy",
                table: "AuditLogs",
                newName: "IX_AuditLogs_PerformedByAdminId");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "AdminUsers",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "locked_until",
                table: "AdminUsers",
                newName: "LockedUntil");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "AdminUsers",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "failed_attempts",
                table: "AdminUsers",
                newName: "FailedAttempts");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "AdminUsers",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "AdminUsers",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "admin_identifier",
                table: "AdminUsers",
                newName: "AdminIdentifier");

            migrationBuilder.RenameColumn(
                name: "admin_id",
                table: "AdminUsers",
                newName: "AdminId");

            migrationBuilder.RenameIndex(
                name: "IX_admin_users_user_id",
                table: "AdminUsers",
                newName: "IX_AdminUsers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_admin_users_admin_identifier",
                table: "AdminUsers",
                newName: "IX_AdminUsers_AdminIdentifier");

            migrationBuilder.RenameColumn(
                name: "trusted_device",
                table: "AdminSessions",
                newName: "TrustedDevice");

            migrationBuilder.RenameColumn(
                name: "ip_address",
                table: "AdminSessions",
                newName: "IpAddress");

            migrationBuilder.RenameColumn(
                name: "expires_at",
                table: "AdminSessions",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "admin_id",
                table: "AdminSessions",
                newName: "AdminId");

            migrationBuilder.RenameColumn(
                name: "session_id",
                table: "AdminSessions",
                newName: "SessionId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "AdminSessions",
                newName: "LoginAt");

            migrationBuilder.RenameIndex(
                name: "IX_admin_sessions_admin_id",
                table: "AdminSessions",
                newName: "IX_AdminSessions_AdminId");

            migrationBuilder.AlterColumn<string>(
                name: "TargetEntityType",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "TargetEntityId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "PerformedByName",
                table: "AuditLogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AuditLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Module",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NewValues",
                table: "AuditLogs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldValues",
                table: "AuditLogs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedByRole",
                table: "AuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Success",
                table: "AuditLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdminIdentifier",
                table: "AdminUsers",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "AdminType",
                table: "AdminUsers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PermissionOverrides",
                table: "AdminUsers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "AdminUsers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "AdminUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "AdminSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "AdminSessions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JwtId",
                table: "AdminSessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LogoutAt",
                table: "AdminSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "AdminSessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "AdminSessions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "LogId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdminUsers",
                table: "AdminUsers",
                column: "AdminId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdminSessions",
                table: "AdminSessions",
                column: "SessionId");

            migrationBuilder.CreateTable(
                name: "AdminEmailOtps",
                columns: table => new
                {
                    OtpId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OtpCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Attempts = table.Column<short>(type: "smallint", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminEmailOtps", x => x.OtpId);
                    table.ForeignKey(
                        name: "FK_AdminEmailOtps_AdminUsers_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminLoginLogs",
                columns: table => new
                {
                    LoginLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LogoutAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminLoginLogs", x => x.LoginLogId);
                    table.ForeignKey(
                        name: "FK_AdminLoginLogs_AdminUsers_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AdminUsers",
                        principalColumn: "AdminId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminRoles",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Permissions = table.Column<string>(type: "jsonb", nullable: false),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRoles", x => x.RoleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_CreatedBy",
                table: "AdminUsers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_RoleId",
                table: "AdminUsers",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminEmailOtps_AdminId",
                table: "AdminEmailOtps",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginLogs_AdminId",
                table: "AdminLoginLogs",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRoles_RoleName",
                table: "AdminRoles",
                column: "RoleName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminSessions_AdminUsers_AdminId",
                table: "AdminSessions",
                column: "AdminId",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminUsers_AdminRoles_RoleId",
                table: "AdminUsers",
                column: "RoleId",
                principalTable: "AdminRoles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminUsers_AdminUsers_CreatedBy",
                table: "AdminUsers",
                column: "CreatedBy",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AdminUsers_users_UserId",
                table: "AdminUsers",
                column: "UserId",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AdminUsers_PerformedByAdminId",
                table: "AuditLogs",
                column: "PerformedByAdminId",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_country_verification_config_AdminUsers_ConfigUpdatedBy",
                table: "country_verification_config",
                column: "ConfigUpdatedBy",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_disputes_AdminUsers_AssignedTo",
                table: "disputes",
                column: "AssignedTo",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_employer_badges_AdminUsers_IssuedBy",
                table: "employer_badges",
                column: "IssuedBy",
                principalTable: "AdminUsers",
                principalColumn: "AdminId");

            migrationBuilder.AddForeignKey(
                name: "FK_kyc_verifications_AdminUsers_ReviewedBy",
                table: "kyc_verifications",
                column: "ReviewedBy",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.SetNull);

           

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_AdminUsers_RefundProcessedBy",
                table: "payment_transactions",
                column: "RefundProcessedBy",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_platform_config_AdminUsers_UpdatedBy",
                table: "platform_config",
                column: "UpdatedBy",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_support_tickets_AdminUsers_AssignedTo",
                table: "support_tickets",
                column: "AssignedTo",
                principalTable: "AdminUsers",
                principalColumn: "AdminId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminSessions_AdminUsers_AdminId",
                table: "AdminSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_AdminUsers_AdminRoles_RoleId",
                table: "AdminUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AdminUsers_AdminUsers_CreatedBy",
                table: "AdminUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AdminUsers_users_UserId",
                table: "AdminUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AdminUsers_PerformedByAdminId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_country_verification_config_AdminUsers_ConfigUpdatedBy",
                table: "country_verification_config");

            migrationBuilder.DropForeignKey(
                name: "FK_disputes_AdminUsers_AssignedTo",
                table: "disputes");

            migrationBuilder.DropForeignKey(
                name: "FK_employer_badges_AdminUsers_IssuedBy",
                table: "employer_badges");

            migrationBuilder.DropForeignKey(
                name: "FK_kyc_verifications_AdminUsers_ReviewedBy",
                table: "kyc_verifications");

          

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_AdminUsers_RefundProcessedBy",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_platform_config_AdminUsers_UpdatedBy",
                table: "platform_config");

            migrationBuilder.DropForeignKey(
                name: "FK_support_tickets_AdminUsers_AssignedTo",
                table: "support_tickets");

            migrationBuilder.DropTable(
                name: "AdminEmailOtps");

            migrationBuilder.DropTable(
                name: "AdminLoginLogs");

            migrationBuilder.DropTable(
                name: "AdminRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdminUsers",
                table: "AdminUsers");

            migrationBuilder.DropIndex(
                name: "IX_AdminUsers_CreatedBy",
                table: "AdminUsers");

            migrationBuilder.DropIndex(
                name: "IX_AdminUsers_RoleId",
                table: "AdminUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AdminSessions",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Module",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "NewValues",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OldValues",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PerformedByRole",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Success",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AdminType",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "PermissionOverrides",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "AdminUsers");

            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "JwtId",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "LogoutAt",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "AdminSessions");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "AdminSessions");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "audit_logs");

            migrationBuilder.RenameTable(
                name: "AdminUsers",
                newName: "admin_users");

            migrationBuilder.RenameTable(
                name: "AdminSessions",
                newName: "admin_sessions");

            migrationBuilder.RenameColumn(
                name: "PerformedByAdminId",
                table: "audit_logs",
                newName: "PerformedBy");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_PerformedByAdminId",
                table: "audit_logs",
                newName: "IX_audit_logs_PerformedBy");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "admin_users",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "LockedUntil",
                table: "admin_users",
                newName: "locked_until");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "admin_users",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "FailedAttempts",
                table: "admin_users",
                newName: "failed_attempts");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "admin_users",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "admin_users",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "AdminIdentifier",
                table: "admin_users",
                newName: "admin_identifier");

            migrationBuilder.RenameColumn(
                name: "AdminId",
                table: "admin_users",
                newName: "admin_id");

            migrationBuilder.RenameIndex(
                name: "IX_AdminUsers_UserId",
                table: "admin_users",
                newName: "IX_admin_users_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_AdminUsers_AdminIdentifier",
                table: "admin_users",
                newName: "IX_admin_users_admin_identifier");

            migrationBuilder.RenameColumn(
                name: "TrustedDevice",
                table: "admin_sessions",
                newName: "trusted_device");

            migrationBuilder.RenameColumn(
                name: "IpAddress",
                table: "admin_sessions",
                newName: "ip_address");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "admin_sessions",
                newName: "expires_at");

            migrationBuilder.RenameColumn(
                name: "AdminId",
                table: "admin_sessions",
                newName: "admin_id");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "admin_sessions",
                newName: "session_id");

            migrationBuilder.RenameColumn(
                name: "LoginAt",
                table: "admin_sessions",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_AdminSessions_AdminId",
                table: "admin_sessions",
                newName: "IX_admin_sessions_admin_id");

            migrationBuilder.AlterColumn<string>(
                name: "TargetEntityType",
                table: "audit_logs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "TargetEntityId",
                table: "audit_logs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PerformedByName",
                table: "audit_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "audit_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "ActionDetail",
                table: "audit_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "audit_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChangeReason",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "admin_identifier",
                table: "admin_users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "admin_role",
                table: "admin_users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "permissions",
                table: "admin_users",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ip_address",
                table: "admin_sessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "session_token",
                table: "admin_sessions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_audit_logs",
                table: "audit_logs",
                column: "LogId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_admin_users",
                table: "admin_users",
                column: "admin_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_admin_sessions",
                table: "admin_sessions",
                column: "session_id");

            migrationBuilder.AddForeignKey(
                name: "FK_admin_sessions_admin_users_admin_id",
                table: "admin_sessions",
                column: "admin_id",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_admin_users_users_user_id",
                table: "admin_users",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_audit_logs_admin_users_PerformedBy",
                table: "audit_logs",
                column: "PerformedBy",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_country_verification_config_admin_users_ConfigUpdatedBy",
                table: "country_verification_config",
                column: "ConfigUpdatedBy",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_disputes_admin_users_AssignedTo",
                table: "disputes",
                column: "AssignedTo",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_employer_badges_admin_users_IssuedBy",
                table: "employer_badges",
                column: "IssuedBy",
                principalTable: "admin_users",
                principalColumn: "admin_id");

            migrationBuilder.AddForeignKey(
                name: "FK_kyc_verifications_admin_users_ReviewedBy",
                table: "kyc_verifications",
                column: "ReviewedBy",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.SetNull);

           

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_admin_users_RefundProcessedBy",
                table: "payment_transactions",
                column: "RefundProcessedBy",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_platform_config_admin_users_UpdatedBy",
                table: "platform_config",
                column: "UpdatedBy",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_support_tickets_admin_users_AssignedTo",
                table: "support_tickets",
                column: "AssignedTo",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
