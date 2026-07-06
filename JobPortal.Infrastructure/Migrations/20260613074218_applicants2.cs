using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class applicants2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_shortlisted",
                table: "job_applications",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "shortlisted_at",
                table: "job_applications",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "interview_scheduled_at",
                table: "job_applications",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rejected_at",
                table: "job_applications",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApplicationStatus",
                table: "job_applications",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_shortlisted",
                table: "job_applications");

            migrationBuilder.DropColumn(
                name: "shortlisted_at",
                table: "job_applications");

            migrationBuilder.DropColumn(
                name: "interview_scheduled_at",
                table: "job_applications");

            migrationBuilder.DropColumn(
                name: "rejected_at",
                table: "job_applications");

            migrationBuilder.AlterColumn<int>(
                name: "ApplicationStatus",
                table: "job_applications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
