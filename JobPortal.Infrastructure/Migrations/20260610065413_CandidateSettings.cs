using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CandidateSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "NotificationSettingNotifPrefId",
                table: "candidate_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreferenceSettingPrefId",
                table: "candidate_profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "candidate_notification_settings",
                columns: table => new
                {
                    NotifPrefId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobMatches = table.Column<bool>(type: "boolean", nullable: false),
                    ApplicationUpdates = table.Column<bool>(type: "boolean", nullable: false),
                    RecruiterMessages = table.Column<bool>(type: "boolean", nullable: false),
                    DocumentReminders = table.Column<bool>(type: "boolean", nullable: false),
                    OffersAnnouncements = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_notification_settings", x => x.NotifPrefId);
                    table.ForeignKey(
                        name: "FK_candidate_notification_settings_candidate_profiles_Candidat~",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "candidate_preference_settings",
                columns: table => new
                {
                    PrefId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "text", nullable: false),
                    TimeZone = table.Column<string>(type: "text", nullable: false),
                    ResumeVisibility = table.Column<string>(type: "text", nullable: false),
                    CommunicationPreference = table.Column<string>(type: "text", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastPasswordUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_candidate_preference_settings", x => x.PrefId);
                    table.ForeignKey(
                        name: "FK_candidate_preference_settings_candidate_profiles_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "candidate_profiles",
                        principalColumn: "candidate_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profiles_NotificationSettingNotifPrefId",
                table: "candidate_profiles",
                column: "NotificationSettingNotifPrefId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_profiles_PreferenceSettingPrefId",
                table: "candidate_profiles",
                column: "PreferenceSettingPrefId");

            migrationBuilder.CreateIndex(
                name: "IX_candidate_notification_settings_CandidateId",
                table: "candidate_notification_settings",
                column: "CandidateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_candidate_preference_settings_CandidateId",
                table: "candidate_preference_settings",
                column: "CandidateId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_candidate_profiles_candidate_notification_settings_Notifica~",
                table: "candidate_profiles",
                column: "NotificationSettingNotifPrefId",
                principalTable: "candidate_notification_settings",
                principalColumn: "NotifPrefId");

            migrationBuilder.AddForeignKey(
                name: "FK_candidate_profiles_candidate_preference_settings_Preference~",
                table: "candidate_profiles",
                column: "PreferenceSettingPrefId",
                principalTable: "candidate_preference_settings",
                principalColumn: "PrefId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_candidate_profiles_candidate_notification_settings_Notifica~",
                table: "candidate_profiles");

            migrationBuilder.DropForeignKey(
                name: "FK_candidate_profiles_candidate_preference_settings_Preference~",
                table: "candidate_profiles");

            migrationBuilder.DropTable(
                name: "candidate_notification_settings");

            migrationBuilder.DropTable(
                name: "candidate_preference_settings");

            migrationBuilder.DropIndex(
                name: "IX_candidate_profiles_NotificationSettingNotifPrefId",
                table: "candidate_profiles");

            migrationBuilder.DropIndex(
                name: "IX_candidate_profiles_PreferenceSettingPrefId",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "NotificationSettingNotifPrefId",
                table: "candidate_profiles");

            migrationBuilder.DropColumn(
                name: "PreferenceSettingPrefId",
                table: "candidate_profiles");
        }
    }
}
