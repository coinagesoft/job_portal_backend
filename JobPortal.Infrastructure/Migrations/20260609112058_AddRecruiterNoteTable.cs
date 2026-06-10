using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruiterNoteTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employer_badges_admin_users_IssuedBy",
                table: "employer_badges");

            migrationBuilder.DropColumn(
                name: "BadgeBlueTick",
                table: "employer_badges");

            migrationBuilder.DropColumn(
                name: "BadgeGstVerified",
                table: "employer_badges");

            migrationBuilder.DropColumn(
                name: "BadgePanVerified",
                table: "employer_badges");

            migrationBuilder.DropColumn(
                name: "BadgePoeLicensed",
                table: "employer_badges");

            migrationBuilder.DropColumn(
                name: "BadgeRpslLicensed",
                table: "employer_badges");

            migrationBuilder.DropColumn(
                name: "BlueTickEligible",
                table: "employer_badges");

            migrationBuilder.RenameColumn(
                name: "BadgeRevokedAt",
                table: "employer_badges",
                newName: "RevokedAt");

            migrationBuilder.RenameColumn(
                name: "BadgeRevocationReason",
                table: "employer_badges",
                newName: "RevocationReason");

            migrationBuilder.RenameColumn(
                name: "BadgeIssuedAt",
                table: "employer_badges",
                newName: "IssuedAt");

            migrationBuilder.RenameColumn(
                name: "MarksPercentage",
                table: "candidate_education",
                newName: "tage");

            migrationBuilder.AlterColumn<Guid>(
                name: "IssuedBy",
                table: "employer_badges",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "BadgeType",
                table: "employer_badges",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "BadgeStatus",
                table: "employer_badges",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "can_read",
                table: "candidate_skills",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "can_speak",
                table: "candidate_skills",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "can_write",
                table: "candidate_skills",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_ai_verified",
                table: "candidate_education",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "year_details",
                table: "candidate_education",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecruiterNotes",
                columns: table => new
                {
                    RecruiterNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployerId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruiterNotes", x => x.RecruiterNoteId);
                    table.ForeignKey(
                        name: "FK_RecruiterNotes_employer_profiles_EmployerId",
                        column: x => x.EmployerId,
                        principalTable: "employer_profiles",
                        principalColumn: "employer_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecruiterNotes_job_applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "job_applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterNotes_ApplicationId",
                table: "RecruiterNotes",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruiterNotes_EmployerId",
                table: "RecruiterNotes",
                column: "EmployerId");

            migrationBuilder.AddForeignKey(
                name: "FK_employer_badges_admin_users_IssuedBy",
                table: "employer_badges",
                column: "IssuedBy",
                principalTable: "admin_users",
                principalColumn: "admin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_employer_badges_admin_users_IssuedBy",
                table: "employer_badges");

            migrationBuilder.DropTable(
                name: "RecruiterNotes");

            migrationBuilder.DropColumn(
                name: "can_read",
                table: "candidate_skills");

            migrationBuilder.DropColumn(
                name: "can_speak",
                table: "candidate_skills");

            migrationBuilder.DropColumn(
                name: "can_write",
                table: "candidate_skills");

            migrationBuilder.DropColumn(
                name: "is_ai_verified",
                table: "candidate_education");

            migrationBuilder.DropColumn(
                name: "year_details",
                table: "candidate_education");

            migrationBuilder.RenameColumn(
                name: "RevokedAt",
                table: "employer_badges",
                newName: "BadgeRevokedAt");

            migrationBuilder.RenameColumn(
                name: "RevocationReason",
                table: "employer_badges",
                newName: "BadgeRevocationReason");

            migrationBuilder.RenameColumn(
                name: "IssuedAt",
                table: "employer_badges",
                newName: "BadgeIssuedAt");

            migrationBuilder.RenameColumn(
                name: "tage",
                table: "candidate_education",
                newName: "MarksPercentage");

            migrationBuilder.AlterColumn<Guid>(
                name: "IssuedBy",
                table: "employer_badges",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BadgeType",
                table: "employer_badges",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "BadgeStatus",
                table: "employer_badges",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "BadgeBlueTick",
                table: "employer_badges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BadgeGstVerified",
                table: "employer_badges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BadgePanVerified",
                table: "employer_badges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BadgePoeLicensed",
                table: "employer_badges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BadgeRpslLicensed",
                table: "employer_badges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BlueTickEligible",
                table: "employer_badges",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_employer_badges_admin_users_IssuedBy",
                table: "employer_badges",
                column: "IssuedBy",
                principalTable: "admin_users",
                principalColumn: "admin_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
