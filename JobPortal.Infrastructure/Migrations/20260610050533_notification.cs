using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class notification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "pref_availability_push",
                table: "employer_notification_settings",
                newName: "pref_system_messages");

            migrationBuilder.AddColumn<bool>(
                name: "pref_job_status_updates",
                table: "employer_notification_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pref_job_status_updates",
                table: "employer_notification_settings");

            migrationBuilder.RenameColumn(
                name: "pref_system_messages",
                table: "employer_notification_settings",
                newName: "pref_availability_push");
        }
    }
}
